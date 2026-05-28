// ───────────────────────────────────────────────────────────────────────
// RULES:
// 1. PROCESS: Use Debug.Log for trace steps.
// 2. SAFETY: Use Debug.LogError in null/boundary checks.
// ───────────────────────────────────────────────────────────────────────

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Build;
using UnityEditorInternal;
#endif

namespace NamPhuThuy.Common
{
#if UNITY_EDITOR
    public class Window_ConstGenerator : EditorWindow
    {
        #region Private Fields
        // Preferences (stored in EditorPrefs)
        private const string PrefNamespace = "ConstGenerator.Namespace";
        private const string PrefOutFolder = "ConstGenerator.OutFolder";

        private string _namespace;
        private string _outFolder;
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Const Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<Window_ConstGenerator>("Const Generator");
            window.minSize = new Vector2(520, 600);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            Debug.Log("[ConstGenerator] OnEnable");
            _namespace = EditorPrefs.GetString(PrefNamespace, "NamPhuThuy.Common");
            _outFolder = EditorPrefs.GetString(PrefOutFolder, "Assets/_Project");
        }

        private void OnDisable()
        {
            Debug.Log("[ConstGenerator] OnDisable");
            EditorPrefs.SetString(PrefNamespace, _namespace);
            EditorPrefs.SetString(PrefOutFolder, _outFolder);
        }

        public void CreateGUI()
        {
            Debug.Log("[ConstGenerator] CreateGUI");
            var root = rootVisualElement;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;
            root.style.paddingTop = 20;
            root.style.paddingBottom = 20;

            // ── Header Section ──
            var header = new Label("Constants Generator")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16, unityTextAlign = TextAnchor.MiddleCenter, marginBottom = 10 }
            };
            root.Add(header);

            var helpBox = new HelpBox(
                "Tip: Use these constants to avoid string-typos and magic numbers. " +
                "Edit namespace/output to fit your project. Re-run after you change layers/tags/scenes/defines.",
                HelpBoxMessageType.Info);
            root.Add(helpBox);

            var mainScroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1, marginTop = 10 } };
            root.Add(mainScroll);

            // ── Content Sections ──
            mainScroll.Add(BuildSettingsSection());
            mainScroll.Add(BuildAddToProjectSection());
            mainScroll.Add(BuildGeneratorsSection());

            // ── Footer Buttons ──
            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10 } };
            
            var generateAllBtn = new Button(() =>
            {
                Debug.Log("[ConstGenerator] Generating all constants...");
                GenerateLayers();
                GenerateTags();
                GenerateSortingLayers();
                GenerateScenes();
                GenerateScriptingDefines();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Const Generator", "Generated all constant files.", "OK");
            }) 
            { 
                text = "Generate All", 
                style = { flexGrow = 1, height = 30, unityFontStyleAndWeight = FontStyle.Bold } 
            };
            buttonRow.Add(generateAllBtn);

            var refreshBtn = new Button(() => 
            {
                Debug.Log("[ConstGenerator] Refreshing assets...");
                AssetDatabase.Refresh();
            }) 
            { 
                text = "Refresh Assets", 
                style = { flexGrow = 1, height = 30, unityFontStyleAndWeight = FontStyle.Bold } 
            };
            buttonRow.Add(refreshBtn);

            root.Add(buttonRow);
        }
        #endregion

        #region UI Builders
        /// <summary>
        /// Creates a reusable box style for visually grouping elements
        /// </summary>
        private VisualElement BuildBox()
        {
            var box = new VisualElement();
            box.style.borderTopWidth = 1; box.style.borderBottomWidth = 1; box.style.borderLeftWidth = 1; box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0.15f, 0.15f, 0.15f, 1f); box.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            box.style.borderLeftColor = new Color(0.15f, 0.15f, 0.15f, 1f); box.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            box.style.borderTopLeftRadius = 3; box.style.borderTopRightRadius = 3;
            box.style.borderBottomLeftRadius = 3; box.style.borderBottomRightRadius = 3;
            box.style.paddingLeft = 10; box.style.paddingRight = 10; box.style.paddingTop = 10; box.style.paddingBottom = 10;
            box.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 0.5f);
            box.style.marginBottom = 10;
            return box;
        }

        private VisualElement BuildSettingsSection()
        {
            var box = BuildBox();

            var title = new Label("Settings") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } };
            box.Add(title);

            // Namespace Field
            var namespaceField = new TextField("Namespace") { value = _namespace };
            namespaceField.RegisterValueChangedCallback(e =>
            {
                _namespace = e.newValue;
                EditorPrefs.SetString(PrefNamespace, _namespace);
            });
            box.Add(namespaceField);

            // Output Folder Row (ObjectField + Reset button)
            var folderRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };
            
            var folderField = new ObjectField("Output Folder")
            {
                objectType = typeof(DefaultAsset),
                value = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_outFolder),
                style = { flexGrow = 1 }
            };
            folderField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != null)
                {
                    string path = AssetDatabase.GetAssetPath(e.newValue);
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        _outFolder = path;
                        EditorPrefs.SetString(PrefOutFolder, _outFolder);
                    }
                    else
                    {
                        Debug.LogError($"[ConstGenerator] Invalid folder selected: {path}");
                    }
                }
            });
            folderRow.Add(folderField);

            var resetBtn = new Button(() =>
            {
                _outFolder = "Assets/Scripts/Generated";
                folderField.value = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_outFolder);
                EditorPrefs.SetString(PrefOutFolder, _outFolder);
            }) 
            { 
                text = "Reset", 
                style = { width = 60 } 
            };
            folderRow.Add(resetBtn);
            box.Add(folderRow);

            // Action Buttons Row
            var actionRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };
            
            var openFolderBtn = new Button(() =>
            {
                var abs = MakeFolder(_outFolder);
                EditorUtility.RevealInFinder(abs);
            }) 
            { 
                text = "Open Output Folder", 
                style = { flexGrow = 1 } 
            };
            actionRow.Add(openFolderBtn);

            var goToScriptBtn = new Button(() =>
            {
                var script = MonoScript.FromScriptableObject(this);
                if (script != null)
                {
                    EditorGUIUtility.PingObject(script);
                }
                else
                {
                    Debug.LogError("[ConstGenerator] Could not find script object.");
                }
            }) 
            { 
                text = "Go to Script", 
                style = { flexGrow = 1 } 
            };
            actionRow.Add(goToScriptBtn);
            
            box.Add(actionRow);

            return box;
        }

        private VisualElement BuildAddToProjectSection()
        {
            var box = BuildBox();

            var title = new Label("Add to Project (comma-separated)") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } };
            box.Add(title);

            // Layers
            var layersRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
            var layersField = new TextField("Layers") { style = { flexGrow = 1 } };
            var addLayersBtn = new Button(() =>
            {
                AddNewLayers(layersField.value);
                layersField.value = "";
            }) { text = "Add", style = { width = 80 } };
            layersRow.Add(layersField);
            layersRow.Add(addLayersBtn);
            box.Add(layersRow);

            // Tags
            var tagsRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
            var tagsField = new TextField("Tags") { style = { flexGrow = 1 } };
            var addTagsBtn = new Button(() =>
            {
                AddNewTags(tagsField.value);
                tagsField.value = "";
            }) { text = "Add", style = { width = 80 } };
            tagsRow.Add(tagsField);
            tagsRow.Add(addTagsBtn);
            box.Add(tagsRow);

            // Sorting Layers
            var sortingRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
            var sortingField = new TextField("Sorting Layers") { style = { flexGrow = 1 } };
            var addSortingBtn = new Button(() =>
            {
                AddNewSortingLayers(sortingField.value);
                sortingField.value = "";
            }) { text = "Add", style = { width = 80 } };
            sortingRow.Add(sortingField);
            sortingRow.Add(addSortingBtn);
            box.Add(sortingRow);

            // Scripting Defines
            var definesRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
            var definesField = new TextField("Scripting Defines") { style = { flexGrow = 1 } };
            var addDefinesBtn = new Button(() =>
            {
                AddNewScriptingDefines(definesField.value);
                definesField.value = "";
            }) { text = "Add", style = { width = 80 } };
            definesRow.Add(definesField);
            definesRow.Add(addDefinesBtn);
            box.Add(definesRow);

            return box;
        }

        private VisualElement BuildGeneratorsSection()
        {
            var box = BuildBox();

            var title = new Label("Generators") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } };
            box.Add(title);

            box.Add(BuildGeneratorRow("Layers", GenerateLayers));
            box.Add(BuildGeneratorRow("Tags", GenerateTags));
            box.Add(BuildGeneratorRow("Sorting Layers", GenerateSortingLayers));
            box.Add(BuildGeneratorRow("Scenes (Build Settings)", GenerateScenes));
            box.Add(BuildGeneratorRow("Scripting Define Symbols", GenerateScriptingDefines));

            return box;
        }

        private VisualElement BuildGeneratorRow(string currentTitle, System.Action gen)
        {
            var rowBox = new VisualElement();
            rowBox.style.borderTopWidth = 1; rowBox.style.borderBottomWidth = 1; rowBox.style.borderLeftWidth = 1; rowBox.style.borderRightWidth = 1;
            rowBox.style.borderTopColor = new Color(0.15f, 0.15f, 0.15f, 1f); rowBox.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            rowBox.style.borderLeftColor = new Color(0.15f, 0.15f, 0.15f, 1f); rowBox.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            rowBox.style.borderTopLeftRadius = 3; rowBox.style.borderTopRightRadius = 3;
            rowBox.style.borderBottomLeftRadius = 3; rowBox.style.borderBottomRightRadius = 3;
            rowBox.style.paddingLeft = 5; rowBox.style.paddingRight = 5; rowBox.style.paddingTop = 5; rowBox.style.paddingBottom = 5;
            rowBox.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.3f);
            rowBox.style.marginBottom = 5;
            rowBox.style.flexDirection = FlexDirection.Row;
            rowBox.style.justifyContent = Justify.SpaceBetween;
            rowBox.style.alignItems = Align.Center;

            rowBox.Add(new Label(currentTitle));
            
            var btn = new Button(() =>
            {
                Debug.Log($"[ConstGenerator] Generating {currentTitle}...");
                gen?.Invoke();
                AssetDatabase.Refresh();
            }) 
            { 
                text = "Generate", 
                style = { width = 100 } 
            };
            rowBox.Add(btn);

            return rowBox;
        }
        #endregion

        #region Private Methods
        private void AddNewLayers(string layerNames)
        {
            if (string.IsNullOrEmpty(layerNames))
            {
                Debug.LogWarning("[ConstGenerator] Layer names string is empty.");
                return;
            }

            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogError("[ConstGenerator] TagManager.asset not found!");
                return;
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null)
            {
                Debug.LogError("[ConstGenerator] 'layers' property not found in TagManager.");
                return;
            }
            
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
                    Debug.LogWarning($"[ConstGenerator] Could not add layer '{layerName}'. All user layer slots are full!");
                }
            }

            if (addedCount > 0)
            {
                tagManager.ApplyModifiedProperties();
                EditorUtility.DisplayDialog("Add Layers", $"Successfully added {addedCount} layers.", "OK");
                GUI.FocusControl(null);
            }
            else
            {
                EditorUtility.DisplayDialog("Add Layers", "No new layers were added (they might already exist or slots are full).", "OK");
            }
        }

        private void AddNewTags(string tagNames)
        {
            if (string.IsNullOrEmpty(tagNames))
            {
                Debug.LogWarning("[ConstGenerator] Tag names string is empty.");
                return;
            }

            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogError("[ConstGenerator] TagManager.asset not found!");
                return;
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");
            if (tags == null)
            {
                Debug.LogError("[ConstGenerator] 'tags' property not found in TagManager.");
                return;
            }
            
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
                GUI.FocusControl(null);
            }
            else
            {
                EditorUtility.DisplayDialog("Add Tags", "No new tags were added (they might already exist).", "OK");
            }
        }

        private void AddNewSortingLayers(string sortingLayerNames)
        {
            if (string.IsNullOrEmpty(sortingLayerNames))
            {
                Debug.LogWarning("[ConstGenerator] Sorting layer names string is empty.");
                return;
            }

            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogError("[ConstGenerator] TagManager.asset not found!");
                return;
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayers == null)
            {
                Debug.LogError("[ConstGenerator] 'm_SortingLayers' property not found in TagManager.");
                return;
            }
            
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
                GUI.FocusControl(null);
            }
            else
            {
                EditorUtility.DisplayDialog("Add Sorting Layers", "No new sorting layers were added (they might already exist).", "OK");
            }
        }

        private void AddNewScriptingDefines(string definesString)
        {
            if (string.IsNullOrEmpty(definesString))
            {
                Debug.LogWarning("[ConstGenerator] Scripting defines string is empty.");
                return;
            }

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
            Debug.Log($"[ConstGenerator] Generated: {path}");
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
