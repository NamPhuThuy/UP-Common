using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditorInternal;
#endif

namespace NamPhuThuy
{
#if UNITY_EDITOR
    public class ConstGenerator : EditorWindow
    {
        #region Private Fields
        // Preferences (stored in EditorPrefs)
        private const string PrefNamespace = "ConstGenerator.Namespace";
        private const string PrefOutFolder = "ConstGenerator.OutFolder";

        private string _namespace;
        private string _outFolder;
        
        private string _newLayerNames = "";
        private string _newTagNames = "";
        private string _newSortingLayerNames = "";
        private string _newScriptingDefines = "";

        private Vector2 _scrollPos;
        private GUIStyle _centeredButtonStyle;
        private GUIStyle _centeredLabelStyle;
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Const Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConstGenerator>("Const Generator");
            window.minSize = new Vector2(520, 480);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            _namespace = EditorPrefs.GetString(PrefNamespace, "NamPhuThuy.Common");
            _outFolder = EditorPrefs.GetString(PrefOutFolder, "Assets/_Project");
        }

        private void OnDisable()
        {
            // Cleanup when window closes
        }

        private void OnGUI()
        {
            InitializeStyles();

            float padding = 20f;
            Rect areaRect = new Rect(padding, padding, position.width - 2 * padding, position.height - 2 * padding);

            GUILayout.BeginArea(areaRect);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            GUILayout.Space(10);
            DrawContent();
            GUILayout.Space(10);
            DrawButtons();

            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }
        #endregion

        #region Initialization
        private void InitializeStyles()
        {
            if (_centeredButtonStyle == null)
            {
                _centeredButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
            }

            if (_centeredLabelStyle == null)
            {
                _centeredLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16
                };
            }
        }

        private void DrawHeader()
        {
            GUILayout.Label("Constants Generator", _centeredLabelStyle);
            EditorGUILayout.HelpBox(
                "Tip: Use these constants to avoid string-typos and magic numbers. " +
                "Edit namespace/output to fit your project. Re-run after you change layers/tags/scenes/defines.",
                MessageType.Info);
        }

        private void DrawContent()
        {
            // Settings Section
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUI.BeginChangeCheck();
                _namespace = EditorGUILayout.TextField(new GUIContent("Namespace"), _namespace);
                
                EditorGUILayout.BeginHorizontal();
                var folderObj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_outFolder);
                var newFolderObj = (DefaultAsset)EditorGUILayout.ObjectField(
                    new GUIContent("Output Folder"), 
                    folderObj, 
                    typeof(DefaultAsset), 
                    false);

                if (newFolderObj != folderObj && newFolderObj != null)
                {
                    string path = AssetDatabase.GetAssetPath(newFolderObj);
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        _outFolder = path; // store as "Assets/..." relative path
                    }
                }

                if (GUILayout.Button("Reset", GUILayout.Width(60)))
                {
                    _outFolder = "Assets/Scripts/Generated";
                }
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetString(PrefNamespace, _namespace);
                    EditorPrefs.SetString(PrefOutFolder, _outFolder);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Output Folder"))
                {
                    var abs = MakeFolder(_outFolder);
                    EditorUtility.RevealInFinder(abs);
                }
                if (GUILayout.Button("Go to Script"))
                {
                    var script = MonoScript.FromScriptableObject(this);
                    if (script != null)
                    {
                        EditorGUIUtility.PingObject(script);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(12);

            // Add new items feature
            GUILayout.Label("Add to Project (comma-separated)", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.BeginHorizontal();
                _newLayerNames = EditorGUILayout.TextField("Layers", _newLayerNames);
                if (GUILayout.Button("Add", GUILayout.Width(80))) AddNewLayers(_newLayerNames);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _newTagNames = EditorGUILayout.TextField("Tags", _newTagNames);
                if (GUILayout.Button("Add", GUILayout.Width(80))) AddNewTags(_newTagNames);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _newSortingLayerNames = EditorGUILayout.TextField("Sorting Layers", _newSortingLayerNames);
                if (GUILayout.Button("Add", GUILayout.Width(80))) AddNewSortingLayers(_newSortingLayerNames);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _newScriptingDefines = EditorGUILayout.TextField("Scripting Defines", _newScriptingDefines);
                if (GUILayout.Button("Add", GUILayout.Width(80))) AddNewScriptingDefines(_newScriptingDefines);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(12);

            // Individual Generators
            GUILayout.Label("Generators", EditorStyles.boldLabel);
            DrawSection("Layers", GenerateLayers);
            DrawSection("Tags", GenerateTags);
            DrawSection("Sorting Layers", GenerateSortingLayers);
            DrawSection("Scenes (Build Settings)", GenerateScenes);
            DrawSection("Scripting Define Symbols", GenerateScriptingDefines);
        }

        private void DrawButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate All", _centeredButtonStyle, GUILayout.Height(30)))
            {
                GenerateLayers();
                GenerateTags();
                GenerateSortingLayers();
                GenerateScenes();
                GenerateScriptingDefines();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Const Generator", "Generated all constant files.", "OK");
            }

            if (GUILayout.Button("Refresh Assets", _centeredButtonStyle, GUILayout.Height(30)))
            {
                AssetDatabase.Refresh();
            }
            GUILayout.EndHorizontal();
        }
        #endregion

        #region Private Methods
        private void DrawSection(string currentTitle, System.Action gen)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(currentTitle);
                if (GUILayout.Button($"Generate", GUILayout.Width(100)))
                {
                    gen?.Invoke();
                    AssetDatabase.Refresh();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AddNewLayers(string layerNames)
        {
            if (string.IsNullOrEmpty(layerNames)) return;

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            
            string[] names = layerNames.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;

            foreach (var n in names)
            {
                string layerName = n.Trim();
                if (string.IsNullOrEmpty(layerName)) continue;

                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++)
                {
                    if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;

                bool added = false;
                for (int i = 8; i < layers.arraySize; i++)
                {
                    SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layerProp.stringValue))
                    {
                        layerProp.stringValue = layerName;
                        added = true;
                        addedCount++;
                        break;
                    }
                }
                if (!added)
                {
                    Debug.LogWarning($"Could not add layer '{layerName}'. All user layer slots are full!");
                }
            }

            if (addedCount > 0)
            {
                tagManager.ApplyModifiedProperties();
                EditorUtility.DisplayDialog("Add Layers", $"Successfully added {addedCount} layers.", "OK");
                _newLayerNames = "";
                GUI.FocusControl(null);
            }
            else
            {
                EditorUtility.DisplayDialog("Add Layers", "No new layers were added (they might already exist or slots are full).", "OK");
            }
        }

        private void AddNewTags(string tagNames)
        {
            if (string.IsNullOrEmpty(tagNames)) return;

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");
            
            string[] names = tagNames.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;

            foreach (var n in names)
            {
                string tagName = n.Trim();
                if (string.IsNullOrEmpty(tagName)) continue;

                bool exists = false;
                for (int i = 0; i < tags.arraySize; i++)
                {
                    if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;

                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
                addedCount++;
            }

            if (addedCount > 0)
            {
                tagManager.ApplyModifiedProperties();
                EditorUtility.DisplayDialog("Add Tags", $"Successfully added {addedCount} tags.", "OK");
                _newTagNames = "";
                GUI.FocusControl(null);
            }
            else
            {
                EditorUtility.DisplayDialog("Add Tags", "No new tags were added (they might already exist).", "OK");
            }
        }

        private void AddNewSortingLayers(string sortingLayerNames)
        {
            if (string.IsNullOrEmpty(sortingLayerNames)) return;

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
            
            string[] names = sortingLayerNames.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;

            foreach (var n in names)
            {
                string sortingLayerName = n.Trim();
                if (string.IsNullOrEmpty(sortingLayerName)) continue;

                bool exists = false;
                for (int i = 0; i < sortingLayers.arraySize; i++)
                {
                    if (sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == sortingLayerName)
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;

                int maxId = 0;
                for(int i = 0; i < sortingLayers.arraySize; i++) 
                {
                    int id = sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").intValue;
                    if(id > maxId) maxId = id;
                }

                sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
                SerializedProperty newLayer = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
                newLayer.FindPropertyRelative("name").stringValue = sortingLayerName;
                newLayer.FindPropertyRelative("uniqueID").intValue = maxId + 1;
                addedCount++;
            }

            if (addedCount > 0)
            {
                tagManager.ApplyModifiedProperties();
                EditorUtility.DisplayDialog("Add Sorting Layers", $"Successfully added {addedCount} sorting layers.", "OK");
                _newSortingLayerNames = "";
                GUI.FocusControl(null);
            }
            else
            {
                EditorUtility.DisplayDialog("Add Sorting Layers", "No new sorting layers were added (they might already exist).", "OK");
            }
        }

        private void AddNewScriptingDefines(string definesString)
        {
            if (string.IsNullOrEmpty(definesString)) return;

            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
#if UNITY_2021_1_OR_NEWER
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            string currentDefines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
#else
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif

            List<string> currentList = new List<string>(currentDefines.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
            string[] newDefines = definesString.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;

            foreach (var n in newDefines)
            {
                string define = n.Trim();
                if (string.IsNullOrEmpty(define)) continue;

                if (!currentList.Contains(define))
                {
                    currentList.Add(define);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                string newDefinesStr = string.Join(";", currentList.ToArray());
#if UNITY_2021_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefinesStr);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, newDefinesStr);
#endif
                EditorUtility.DisplayDialog("Add Scripting Defines", $"Successfully added {addedCount} defines.", "OK");
                _newScriptingDefines = "";
                GUI.FocusControl(null);
            }
            else
            {
                EditorUtility.DisplayDialog("Add Scripting Defines", "No new defines were added (they might already exist).", "OK");
            }
        }

        private void GenerateLayers()
        {
            var sb = new StringBuilder();
            BeginFile(sb, "LayerConst");

            // Strings
            sb.AppendLine("        // Layer names");
            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(name)) continue;
                string constName = ToConstName(name);
                sb.AppendLine($"        public const string {constName} = \"{name}\";");
            }
            sb.AppendLine();

            // Indices
            sb.AppendLine("        // Layer indices");
            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(name)) continue;
                string constName = ToConstName(name) + "_INDEX";
                sb.AppendLine($"        public const int {constName} = {i};");
            }

            EndFile(sb);
            WriteFile("LayerConst.cs", sb.ToString());
        }

        private void GenerateTags()
        {
            var tags = InternalEditorUtility.tags;

            var sb = new StringBuilder();
            BeginFile(sb, "TagConst");

            sb.AppendLine("        // Unity Tags");
            foreach (var t in tags)
            {
                if (string.IsNullOrEmpty(t)) continue;
                sb.AppendLine($"        public const string {ToConstName(t)} = \"{t}\";");
            }

            EndFile(sb);
            WriteFile("TagConst.cs", sb.ToString());
        }

        private void GenerateSortingLayers()
        {
            var layers = SortingLayer.layers; // struct[] with .id and .name

            var sb = new StringBuilder();
            BeginFile(sb, "SortingLayerConst");

            sb.AppendLine("        // Sorting Layer names");
            foreach (var l in layers)
            {
                if (string.IsNullOrEmpty(l.name))
                    continue;
                sb.AppendLine($"        public const string {ToConstName(l.name)} = \"{l.name}\";");
            }
            sb.AppendLine();
            sb.AppendLine("        // Sorting Layer IDs");
            foreach (var l in layers)
            {
                if (string.IsNullOrEmpty(l.name))
                    continue;
                sb.AppendLine($"        public const int {ToConstName(l.name)}_ID = {l.id};");
            }

            EndFile(sb);
            WriteFile("SortingLayerConst.cs", sb.ToString());
        }

        private void GenerateScenes()
        {
            var scenes = EditorBuildSettings.scenes;

            var sb = new StringBuilder();
            BeginFile(sb, "SceneConst");

            sb.AppendLine("        // Scenes included in Build Settings");
            for (int i = 0; i < scenes.Length; i++)
            {
                var s = scenes[i];
                if (!s.enabled) continue;

                string name = Path.GetFileNameWithoutExtension(s.path);
                string safe = ToConstName(name);
                sb.AppendLine($"        public const string {safe} = \"{name}\";");
            }

            sb.AppendLine();
            sb.AppendLine("        // Build indices");
            int buildIndex = 0;
            for (int i = 0; i < scenes.Length; i++)
            {
                var s = scenes[i];
                if (!s.enabled) continue;
                string name = Path.GetFileNameWithoutExtension(s.path);
                string safe = ToConstName(name);
                sb.AppendLine($"        public const int {safe}_INDEX = {buildIndex};");
                buildIndex++;
            }

            EndFile(sb);
            WriteFile("SceneConst.cs", sb.ToString());
        }

        private void GenerateScriptingDefines()
        {
            // By platform group of current active target
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
#if UNITY_2021_1_OR_NEWER
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
#else
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif
            var defineArray = defines.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder();
            BeginFile(sb, "ScriptingDefineConst");

            sb.AppendLine("        // PlayerSettings Scripting Define Symbols for current target group");
            foreach (var d in defineArray)
            {
                var trimmed = d.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                sb.AppendLine($"        public const string {ToConstName(trimmed)} = \"{trimmed}\";");
            }

            EndFile(sb);
            WriteFile("ScriptingDefineConst.cs", sb.ToString());
        }
        #endregion

        #region Helper Methods
        private void BeginFile(StringBuilder sb, string className)
        {
            sb.AppendLine("/* This file is auto-generated. Do not edit by hand. */");
            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {className}");
            sb.AppendLine("    {");
        }

        private void EndFile(StringBuilder sb)
        {
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        private void WriteFile(string fileName, string content)
        {
            string absFolder = MakeFolder(_outFolder);
            string path = Path.Combine(absFolder, fileName);
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        private static string MakeFolder(string projectRelative)
        {
            // Ensure under Assets
            if (string.IsNullOrEmpty(projectRelative) || !projectRelative.StartsWith("Assets"))
                projectRelative = "Assets/Scripts/Generated";

            if (!AssetDatabase.IsValidFolder(projectRelative))
            {
                // Create nested folders
                var parts = projectRelative.Split('/');
                string cur = "Assets";
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = $"{cur}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(cur, parts[i]);
                    cur = next;
                }
            }

            return Path.GetFullPath(projectRelative);
        }

        private static string ToConstName(string raw)
        {
            // Uppercase, replace invalid with underscore, collapse repeats, trim edges
            string upper = raw.ToUpperInvariant();
            upper = Regex.Replace(upper, @"[^A-Z0-9_]", "_");
            upper = Regex.Replace(upper, @"_+", "_");
            upper = upper.Trim('_');

            // Ensure first char is a letter or underscore
            if (!Regex.IsMatch(upper, @"^[A-Z_]")) upper = "_" + upper;

            return upper;
        }
        #endregion
    }
#endif
}