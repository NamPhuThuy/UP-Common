// ───────────────────────────────────────────────────────────────────────
// RULES:
// 1. PROCESS: Use Debug.Log for trace steps.
// 2. SAFETY: Use Debug.LogError in null/boundary checks.
// ───────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace NamPhuThuy.Common
{
#if UNITY_EDITOR
    public class Window_ScriptsModifier : EditorWindow
    {
        #region Private Fields
        // EditorPrefs Keys
        private const string PREF_KEY_TARGET_FOLDER_PATH = "NamPhuThuy_ScriptsModifier_TargetFolder";
        private const string PREF_KEY_OLD_NAMESPACE = "NamPhuThuy_ScriptsModifier_OldNamespace";
        private const string PREF_KEY_NEW_NAMESPACE = "NamPhuThuy_ScriptsModifier_NewNamespace";
        private const string PREF_KEY_ARCHIVE_FOLDER_PATH = "NamPhuThuy_ScriptsModifier_ArchiveFolder";

        private DefaultAsset _targetFolder;
        private string _oldNamespace = "Old.Namespace";
        private string _newNamespace = "New.Namespace";
        private DefaultAsset _archiveFolder;
        
        private const string COMMENT_PLACEHOLDER = "#1#";
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Scripts Modifier")]
        public static void ShowWindow()
        {
            Window_ScriptsModifier window = GetWindow<Window_ScriptsModifier>("Scripts Modifier");
            window.minSize = new Vector2(400, 450);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            Debug.Log("[ScriptsModifier] OnEnable");
            
            // Load saved preferences
            string targetPath = EditorPrefs.GetString(PREF_KEY_TARGET_FOLDER_PATH, "");
            if (!string.IsNullOrEmpty(targetPath))
            {
                _targetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetPath);
            }
            
            _oldNamespace = EditorPrefs.GetString(PREF_KEY_OLD_NAMESPACE, "Old.Namespace");
            _newNamespace = EditorPrefs.GetString(PREF_KEY_NEW_NAMESPACE, "New.Namespace");
            
            string archivePath = EditorPrefs.GetString(PREF_KEY_ARCHIVE_FOLDER_PATH, "");
            if (!string.IsNullOrEmpty(archivePath))
            {
                _archiveFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(archivePath);
            }
        }

        private void OnDisable()
        {
            Debug.Log("[ScriptsModifier] OnDisable");
            
            // Save preferences
            if (_targetFolder != null)
            {
                EditorPrefs.SetString(PREF_KEY_TARGET_FOLDER_PATH, AssetDatabase.GetAssetPath(_targetFolder));
            }
            else
            {
                EditorPrefs.SetString(PREF_KEY_TARGET_FOLDER_PATH, "");
            }
            
            EditorPrefs.SetString(PREF_KEY_OLD_NAMESPACE, _oldNamespace);
            EditorPrefs.SetString(PREF_KEY_NEW_NAMESPACE, _newNamespace);
            
            if (_archiveFolder != null)
            {
                EditorPrefs.SetString(PREF_KEY_ARCHIVE_FOLDER_PATH, AssetDatabase.GetAssetPath(_archiveFolder));
            }
            else
            {
                EditorPrefs.SetString(PREF_KEY_ARCHIVE_FOLDER_PATH, "");
            }
        }

        public void CreateGUI()
        {
            Debug.Log("[ScriptsModifier] CreateGUI");
            var root = rootVisualElement;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;
            root.style.paddingTop = 20;
            root.style.paddingBottom = 20;

            // ── Header Section ──
            var header = new Label("Scripts Modification Tool")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16, unityTextAlign = TextAnchor.MiddleCenter, marginBottom = 10 }
            };
            root.Add(header);

            var helpBox = new HelpBox(
                "This tool can perform file modifications that CANNOT be undone with Ctrl+Z.\n" +
                "Please use version control (like Git) to revert changes if needed.",
                HelpBoxMessageType.Warning);
            root.Add(helpBox);

            var mainScroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1, marginTop = 10 } };
            root.Add(mainScroll);

            // ── Content Sections ──
            mainScroll.Add(BuildNamespaceSection());
            mainScroll.Add(BuildArchivingSection());
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

        private VisualElement BuildNamespaceSection()
        {
            var box = BuildBox();

            var title = new Label("Namespace Refactoring") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } };
            box.Add(title);

            // Target Folder
            var targetFolderField = new ObjectField("Target Folder")
            {
                objectType = typeof(DefaultAsset),
                value = _targetFolder
            };
            targetFolderField.RegisterValueChangedCallback(e =>
            {
                _targetFolder = e.newValue as DefaultAsset;
                if (_targetFolder != null)
                {
                    EditorPrefs.SetString(PREF_KEY_TARGET_FOLDER_PATH, AssetDatabase.GetAssetPath(_targetFolder));
                }
                else
                {
                    EditorPrefs.SetString(PREF_KEY_TARGET_FOLDER_PATH, "");
                }
            });
            box.Add(targetFolderField);

            // Old Namespace
            var oldNamespaceField = new TextField("Old Namespace") { value = _oldNamespace };
            oldNamespaceField.RegisterValueChangedCallback(e =>
            {
                _oldNamespace = e.newValue;
                EditorPrefs.SetString(PREF_KEY_OLD_NAMESPACE, _oldNamespace);
            });
            box.Add(oldNamespaceField);

            // New Namespace
            var newNamespaceField = new TextField("New Namespace") { value = _newNamespace };
            newNamespaceField.RegisterValueChangedCallback(e =>
            {
                _newNamespace = e.newValue;
                EditorPrefs.SetString(PREF_KEY_NEW_NAMESPACE, _newNamespace);
            });
            box.Add(newNamespaceField);

            // Change Namespaces Button
            var changeBtn = new Button(ChangeNamespaces)
            {
                text = "Change Namespaces",
                style = { height = 30, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 5 }
            };
            box.Add(changeBtn);

            return box;
        }

        private VisualElement BuildArchivingSection()
        {
            var box = BuildBox();

            var title = new Label("Script Archiving") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } };
            box.Add(title);

            // Target Folder
            var archiveFolderField = new ObjectField("Target Folder")
            {
                objectType = typeof(DefaultAsset),
                value = _archiveFolder
            };
            archiveFolderField.RegisterValueChangedCallback(e =>
            {
                _archiveFolder = e.newValue as DefaultAsset;
                if (_archiveFolder != null)
                {
                    EditorPrefs.SetString(PREF_KEY_ARCHIVE_FOLDER_PATH, AssetDatabase.GetAssetPath(_archiveFolder));
                }
                else
                {
                    EditorPrefs.SetString(PREF_KEY_ARCHIVE_FOLDER_PATH, "");
                }
            });
            box.Add(archiveFolderField);

            // Buttons row (Comment Out / Un-comment)
            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };

            var commentBtn = new Button(CommentOutScripts)
            {
                text = "Comment Out",
                style = { flexGrow = 1, height = 30, unityFontStyleAndWeight = FontStyle.Bold }
            };
            buttonRow.Add(commentBtn);

            var uncommentBtn = new Button(UncommentScripts)
            {
                text = "Un-comment",
                style = { flexGrow = 1, height = 30, unityFontStyleAndWeight = FontStyle.Bold }
            };
            buttonRow.Add(uncommentBtn);

            box.Add(buttonRow);

            return box;
        }
        #endregion

        #region Private Methods
        private void ChangeNamespaces()
        {
            if (_targetFolder == null)
            {
                Debug.LogError("[ScriptsModifier] Please select a target folder for namespace changing.");
                EditorUtility.DisplayDialog("Error", "Please select a target folder for namespace changing.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_oldNamespace))
            {
                Debug.LogError("[ScriptsModifier] Old Namespace cannot be empty for this operation.");
                EditorUtility.DisplayDialog("Error", "Old Namespace cannot be empty for this operation.", "OK");
                return;
            }

            string path = AssetDatabase.GetAssetPath(_targetFolder);
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Debug.LogError($"[ScriptsModifier] The selected folder does not exist: {path}");
                EditorUtility.DisplayDialog("Error", "The selected folder does not exist.", "OK");
                return;
            }

            Debug.Log($"[ScriptsModifier] Requesting confirmation to replace namespace '{_oldNamespace}' with '{_newNamespace}' inside '{path}'...");
            if (EditorUtility.DisplayDialog("Confirm Namespace Change",
                    $"Are you sure you want to replace namespace '{_oldNamespace}' with '{_newNamespace}' in all scripts inside '{path}'?",
                    "Yes, Change Namespaces", "Cancel"))
            {
                ProcessFolderForNamespaceChange(path);
            }
        }

        private void ProcessFolderForNamespaceChange(string path)
        {
            Debug.Log($"[ScriptsModifier] Scanning for .cs files in: {path}");
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Debug.LogError($"[ScriptsModifier] Directory path is invalid or non-existent: {path}");
                return;
            }

            string[] scriptFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            int filesChanged = 0;
            string pattern = @"(^\s*namespace\s+)" + Regex.Escape(_oldNamespace) + @"(?=\s*[\r\n{])";
            string replacement = @"$1" + _newNamespace;

            foreach (string filePath in scriptFiles)
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Debug.LogWarning($"[ScriptsModifier] File not found or path empty: {filePath}");
                    continue;
                }

                string content = File.ReadAllText(filePath);
                if (Regex.IsMatch(content, pattern, RegexOptions.Multiline))
                {
                    Debug.Log($"[ScriptsModifier] Modifying namespace in file: {filePath}");
                    string newContent = Regex.Replace(content, pattern, replacement, RegexOptions.Multiline);
                    File.WriteAllText(filePath, newContent);
                    filesChanged++;
                }
            }
    
            if (filesChanged > 0)
            {
                Debug.Log($"[ScriptsModifier] Successfully changed namespace in {filesChanged} files.");
                EditorUtility.DisplayDialog("Success", $"Successfully changed the namespace in {filesChanged} files.", "OK");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning($"[ScriptsModifier] No scripts with the exact namespace '{_oldNamespace}' were found.");
                EditorUtility.DisplayDialog("No Changes", $"No scripts with the exact namespace '{_oldNamespace}' were found.", "OK");
            }
        }

        private void CommentOutScripts()
        {
            if (_archiveFolder == null)
            {
                Debug.LogError("[ScriptsModifier] Archive folder is null!");
                EditorUtility.DisplayDialog("Error", "Please select a folder to comment out.", "OK");
                return;
            }
            string path = AssetDatabase.GetAssetPath(_archiveFolder);
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Debug.LogError($"[ScriptsModifier] The archive folder does not exist: {path}");
                EditorUtility.DisplayDialog("Error", "The archive folder does not exist.", "OK");
                return;
            }

            Debug.Log($"[ScriptsModifier] Requesting confirmation to comment out all scripts in: {path}");
            if (EditorUtility.DisplayDialog("Confirm Comment Out Scripts",
                    $"Are you sure you want to comment out all .cs files inside '{path}' using multi-line comments?",
                    "Yes, Comment Out", "Cancel"))
            {
                ProcessFolderForCommenting(path, true);
            }
        }

        private void UncommentScripts()
        {
            if (_archiveFolder == null)
            {
                Debug.LogError("[ScriptsModifier] Archive folder is null!");
                EditorUtility.DisplayDialog("Error", "Please select a folder to un-comment.", "OK");
                return;
            }
            string path = AssetDatabase.GetAssetPath(_archiveFolder);
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Debug.LogError($"[ScriptsModifier] The archive folder does not exist: {path}");
                EditorUtility.DisplayDialog("Error", "The archive folder does not exist.", "OK");
                return;
            }

            Debug.Log($"[ScriptsModifier] Requesting confirmation to un-comment all scripts in: {path}");
            if (EditorUtility.DisplayDialog("Confirm Un-comment Scripts",
                    $"Are you sure you want to remove multi-line comments from all .cs files inside '{path}'?",
                    "Yes, Un-comment", "Cancel"))
            {
                ProcessFolderForCommenting(path, false);
            }
        }

        private void ProcessFolderForCommenting(string path, bool shouldComment)
        {
            Debug.Log($"[ScriptsModifier] Scanning for scripts to comment/uncomment in: {path}");
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Debug.LogError($"[ScriptsModifier] Directory path is invalid or non-existent: {path}");
                return;
            }

            string[] scriptFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            int filesChanged = 0;

            foreach (string filePath in scriptFiles)
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Debug.LogWarning($"[ScriptsModifier] File not found or path empty: {filePath}");
                    continue;
                }

                string content = File.ReadAllText(filePath);
                string newContent = content;

                if (shouldComment)
                {
                    // Avoid double-commenting
                    if (!content.StartsWith("/*") && !content.EndsWith("*/"))
                    {
                        Debug.Log($"[ScriptsModifier] Commenting out file: {filePath}");
                        // Replace any inner "*/" to prevent syntax errors
                        string escapedContent = content.Replace("*/", COMMENT_PLACEHOLDER);
                        newContent = "/*\n" + escapedContent + "\n*/";
                        filesChanged++;
                    }
                }
                else // Un-comment
                {
                    // Check if the file is actually commented out in this way
                    if (content.StartsWith("/*") && content.EndsWith("*/"))
                    {
                        Debug.Log($"[ScriptsModifier] Un-commenting file: {filePath}");
                        // Remove the outer '/*' and '*/'
                        string strippedContent = content.Substring(3, content.Length - 6).Trim();
                        // Restore the original "*/" from the placeholder
                        newContent = strippedContent.Replace(COMMENT_PLACEHOLDER, "*/");
                        filesChanged++;
                    }
                }
                
                if (newContent != content)
                {
                    File.WriteAllText(filePath, newContent);
                }
            }

            string action = shouldComment ? "commented out" : "un-commented";
            if (filesChanged > 0)
            {
                Debug.Log($"[ScriptsModifier] Successfully {action} {filesChanged} script(s).");
                EditorUtility.DisplayDialog("Success", $"Successfully {action} {filesChanged} script(s).", "OK");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning($"[ScriptsModifier] No scripts were {action}.");
                EditorUtility.DisplayDialog("No Changes", "No scripts needed to be modified.", "OK");
            }
        }
        #endregion
    }
#endif
}