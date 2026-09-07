// ───────────────────────────────────────────────────────────────────────
// RULES:
// 1. PROCESS: Use Debug.Log for trace steps.
// 2. SAFETY: Use Debug.LogError in null/boundary checks.
// 3. ENUM FORMAT: If used enum, use the format:
//    public enum Type
//    {
//        NONE = 0, TYPE_1 = 1, TYPE_2 = 2
//    }
// 4. STRINGS: Use 'private const string' for resource paths, settings keys, and default folder paths.
// 5. DIALOGS: Use Debug.LogError (or Debug.LogWarning) instead of EditorUtility.DisplayDialog for editor errors/warnings.
// 6. FOLDERS: For fields representing folder paths, use 'DefaultAsset' fields to allow dragging and dropping folders instead of using simple string fields.
// 7. CACHING: Provide a 'Reset to Defaults' button in the options panel calling a method named 'ResetToDefaults()' to clear/override cached or persisted EditorPrefs values that might become stale or invalid.
// 8. LISTS: When resetting list fields, avoid re-instantiating them if they are not null. Clear them instead to prevent issues with serialized property bindings.
// 9. NOTIFICATIONS: Reduce to use addition window to notify information, just Debug.Log it with color and method name prefix.
// 10. LOGGING CONCISENESS: Keep Debug.Log text short and focused mainly on keywords (e.g., "OnEnable", "Action 1: Start", "Success", "ResetToDefaults") to ensure maximum readability and zero clutter.
// 11. IN-MEMORY RESET: When resetting cached keys in ResetToDefaults(), ensure you also clear or re-initialize the corresponding in-memory fields (e.g., set to default asset or null). Otherwise, OnDisable() will re-save the old in-memory values back to EditorPrefs when the window closes to reload.
// ───────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace NamPhuThuy.Common
{
#if UNITY_EDITOR
    /// <summary>
    /// UITK Editor Window to modify script namespaces and archive scripts by commenting them out.
    /// Follows the architecture and styling rules of Window_Template_UITK.
    /// </summary>
    public class Window_ScriptsModifier : EditorWindow
    {
        #region Enums (Rule 3)
        public enum TabType
        {
            NONE = 0,
            NAMESPACE = 1,
            ARCHIVING = 2,
            SETTINGS = 3
        }
        #endregion

        #region Private Fields
        [Header("Namespace Settings")]
        [SerializeField] private string _newNamespace = "New.Namespace";
        [SerializeField] private List<MonoScript> _targetScripts = new List<MonoScript>();

        [Header("Archiving Settings")]
        [SerializeField] private DefaultAsset _archiveFolderAsset; // Rule 6 (Folder fields as DefaultAsset)

        // EditorPrefs keys (Rule 4)
        private const string PREF_KEY_NEW_NAMESPACE = "NamPhuThuy_ScriptsModifier_NewNamespace";
        private const string PREF_KEY_ARCHIVE_FOLDER_PATH = "NamPhuThuy_ScriptsModifier_ArchiveFolderPath";
        private const string PREF_KEY_ACTIVE_TAB = "NamPhuThuy_ScriptsModifier_ActiveTab";

        // Paths & Signature config (Rule 4)
        private const string SIGNATURE_MARK_RELATIVE_PATH = "../../nam_phu_thuy.png";
        private const string WINDOW_TITLE = "Scripts Modifier";
        private const string COMMENT_PLACEHOLDER = "#1#";

        /*
            NOTE: Using static expression-bodied properties (COLOR => ...) instead of "static readonly".
            Because Color is a struct (value type), returning it from a getter method constructs it on the stack
            on demand. Dynamically evaluates EditorGUIUtility.isProSkin to match Unity's Dark/Light editor skin.
        */
        private static Color COLOR_EDITOR_BG => EditorGUIUtility.isProSkin
            ? new Color(0.22f, 0.22f, 0.22f, 1f)
            : new Color(0.78f, 0.78f, 0.78f, 1f);

        private static Color COLOR_GREY_BOX => EditorGUIUtility.isProSkin
            ? new Color(0.16f, 0.16f, 0.16f, 0.6f)
            : new Color(0.72f, 0.72f, 0.72f, 0.4f);

        private static Color COLOR_GREY_BORDER => EditorGUIUtility.isProSkin
            ? new Color(0.26f, 0.26f, 0.26f, 0.8f)
            : new Color(0.60f, 0.60f, 0.60f, 0.8f);

        private static Color COLOR_OCEAN_BLUE => EditorGUIUtility.isProSkin
            ? new Color(0.0f, 0.47f, 0.74f, 1f)
            : new Color(0.05f, 0.42f, 0.70f, 1f);

        private static Color COLOR_SKY_BLUE => EditorGUIUtility.isProSkin
            ? new Color(0.53f, 0.80f, 0.92f, 1f)
            : new Color(0.08f, 0.45f, 0.72f, 1f);

        private static Color COLOR_FOREST_MIST => EditorGUIUtility.isProSkin
            ? new Color(0.8f, 0.8f, 0.8f, 1f)
            : new Color(0.18f, 0.18f, 0.18f, 1f);

        private static Color COLOR_TAB_INACTIVE_BG => EditorGUIUtility.isProSkin
            ? new Color(0.16f, 0.16f, 0.16f, 1f)
            : new Color(0.82f, 0.82f, 0.82f, 1f);

        private static Color COLOR_TAB_INACTIVE_BORDER => EditorGUIUtility.isProSkin
            ? new Color(0.11f, 0.11f, 0.11f, 1f)
            : new Color(0.65f, 0.65f, 0.65f, 1f);

        private static Color COLOR_DANGER_BG => EditorGUIUtility.isProSkin
            ? new Color(0.55f, 0.15f, 0.15f, 1f)
            : new Color(0.75f, 0.20f, 0.20f, 1f);

        private static Color COLOR_DANGER_BORDER => EditorGUIUtility.isProSkin
            ? new Color(0.6f, 0.2f, 0.2f, 0.8f)
            : new Color(0.65f, 0.18f, 0.18f, 0.8f);

        // Tab state
        private TabType _activeTab = TabType.NAMESPACE;

        // UI references
        private VisualElement _contentContainer;
        private VisualElement _tabHeaderContainer;
        private TextField _newNamespaceTextField;
        private PropertyField _archiveFolderPropertyField;
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Scripts Modifier")]
        public static void ShowWindow()
        {
            var window = GetWindow<Window_ScriptsModifier>(WINDOW_TITLE);
            window.minSize = new Vector2(500, 650);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            Debug.Log("<color=#3B82F6>[Window_ScriptsModifier]</color> OnEnable"); // Rule 10 (Keywords only)

            // Load persisted settings (Rule 7)
            _newNamespace = EditorPrefs.GetString(PREF_KEY_NEW_NAMESPACE, "New.Namespace");
            _activeTab = (TabType)EditorPrefs.GetInt(PREF_KEY_ACTIVE_TAB, (int)TabType.NAMESPACE);

            string archivePath = EditorPrefs.GetString(PREF_KEY_ARCHIVE_FOLDER_PATH, "");
            if (!string.IsNullOrEmpty(archivePath))
            {
                _archiveFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(archivePath);
            }
        }

        private void OnDisable()
        {
            Debug.Log("<color=#3B82F6>[Window_ScriptsModifier]</color> OnDisable"); // Rule 10 (Keywords only)

            // Save persisted settings (Rule 7)
            EditorPrefs.SetString(PREF_KEY_NEW_NAMESPACE, _newNamespace);
            EditorPrefs.SetInt(PREF_KEY_ACTIVE_TAB, (int)_activeTab);

            if (_archiveFolderAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(_archiveFolderAsset);
                EditorPrefs.SetString(PREF_KEY_ARCHIVE_FOLDER_PATH, path);
            }
            else
            {
                EditorPrefs.SetString(PREF_KEY_ARCHIVE_FOLDER_PATH, "");
            }
        }

        public void CreateGUI()
        {
            Debug.Log("[Window_ScriptsModifier] CreateGUI"); // Rule 10 (Keywords only)

            var root = rootVisualElement;
            root.style.backgroundColor = COLOR_EDITOR_BG;
            root.style.paddingLeft = 14;
            root.style.paddingRight = 14;
            root.style.paddingTop = 14;
            root.style.paddingBottom = 14;

            // 1. Signature Header Row
            root.Add(BuildHeader());

            // 2. Navigation Tab Bar
            _tabHeaderContainer = BuildNavigation();
            root.Add(_tabHeaderContainer);

            // Separator line
            var separator = new VisualElement
            {
                style =
                {
                    height = 2,
                    backgroundColor = COLOR_GREY_BORDER,
                    marginTop = 4,
                    marginBottom = 12
                }
            };
            root.Add(separator);

            // 3. Dynamic content container
            _contentContainer = new ScrollView(ScrollViewMode.Vertical)
            {
                style = { flexGrow = 1 }
            };
            root.Add(_contentContainer);

            // Render current page content
            RefreshRegion();
        }
        #endregion

        #region Layout Builders
        /// <summary>
        /// Builds the top branding header containing the signature mark image.
        /// </summary>
        private VisualElement BuildHeader()
        {
            var headerRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingBottom = 10,
                    marginBottom = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = COLOR_GREY_BORDER
                }
            };

            // Signature mark visual element (nam_phu_thuy.png)
            var signatureMark = new VisualElement
            {
                style =
                {
                    width = 44,
                    height = 44,
                    marginRight = 12,
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6
                }
            };

            // Resolve relative path to absolute asset path
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string scriptDir = Path.GetDirectoryName(scriptPath);
            string combinedPath = Path.Combine(scriptDir, SIGNATURE_MARK_RELATIVE_PATH);
            string fullPath = Path.GetFullPath(combinedPath).Replace("\\", "/");
            string resolvedPath = "Assets" + fullPath.Substring(Application.dataPath.Length);

            // Loading texture dynamically (Rule 1)
            var signatureTex = AssetDatabase.LoadAssetAtPath<Texture2D>(resolvedPath);
            if (signatureTex != null)
            {
                signatureMark.style.backgroundImage = signatureTex;
            }
            else
            {
                // Safety callback (Rule 2 / Rule 9 / Rule 10)
                Debug.LogWarning($"<color=orange>[Window_ScriptsModifier]</color> Missing Logo: {resolvedPath}");
                signatureMark.style.backgroundColor = COLOR_GREY_BOX;
            }
            headerRow.Add(signatureMark);

            // Titles
            var textColumn = new VisualElement { style = { flexGrow = 1 } };
            var mainTitle = new Label("Scripts Modifier")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 16,
                    color = COLOR_SKY_BLUE
                }
            };
            var subTitle = new Label("Modify Namespaces & Archive C# Scripts")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    unityFontStyleAndWeight = FontStyle.Normal
                }
            };
            textColumn.Add(mainTitle);
            textColumn.Add(subTitle);
            headerRow.Add(textColumn);

            return headerRow;
        }

        /// <summary>
        /// Builds the navigation tabs bar.
        /// </summary>
        private VisualElement BuildNavigation()
        {
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart
                }
            };

            bar.Add(CreateNavigationButton("Namespace", TabType.NAMESPACE));
            bar.Add(CreateNavigationButton("Archiving", TabType.ARCHIVING));
            bar.Add(CreateNavigationButton("Settings", TabType.SETTINGS));

            return bar;
        }

        /// <summary>
        /// Instantiates a styled tab button with hover/active states.
        /// </summary>
        private Button CreateNavigationButton(string label, TabType tab)
        {
            bool isActive = _activeTab == tab;
            var btn = new Button(() => SwitchRegion(tab))
            {
                text = label,
                style =
                {
                    flexGrow = 1,
                    height = 28,
                    fontSize = 12,
                    marginLeft = 2,
                    marginRight = 2,
                    unityFontStyleAndWeight = isActive ? FontStyle.Bold : FontStyle.Normal,
                    backgroundColor = isActive ? COLOR_OCEAN_BLUE : COLOR_TAB_INACTIVE_BG,
                    color = isActive ? Color.white : COLOR_FOREST_MIST,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = isActive ? COLOR_SKY_BLUE : COLOR_TAB_INACTIVE_BORDER,
                    borderBottomColor = isActive ? COLOR_SKY_BLUE : COLOR_TAB_INACTIVE_BORDER,
                    borderLeftColor = isActive ? COLOR_SKY_BLUE : COLOR_TAB_INACTIVE_BORDER,
                    borderRightColor = isActive ? COLOR_SKY_BLUE : COLOR_TAB_INACTIVE_BORDER,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };

            return btn;
        }

        /// <summary>
        /// Updates the current active subpage.
        /// </summary>
        private void SwitchRegion(TabType newTab)
        {
            if (_activeTab == newTab) return;
            _activeTab = newTab;

            // Rebuild tab headers to display active/inactive states
            var parent = _tabHeaderContainer.parent;
            int siblingIdx = parent.IndexOf(_tabHeaderContainer);
            parent.Remove(_tabHeaderContainer);
            _tabHeaderContainer = BuildNavigation();
            parent.Insert(siblingIdx, _tabHeaderContainer);

            RefreshRegion();
        }

        /// <summary>
        /// Refreshes the content inside the scroll area based on active tab selection.
        /// </summary>
        private void RefreshRegion()
        {
            _contentContainer.Clear();

            switch (_activeTab)
            {
                case TabType.NAMESPACE:
                    BuildNamespaceRegion(_contentContainer);
                    break;
                case TabType.ARCHIVING:
                    BuildArchivingRegion(_contentContainer);
                    break;
                case TabType.SETTINGS:
                    BuildSettingsRegion(_contentContainer);
                    break;
            }
        }
        #endregion

        #region Page Content Builders
        /// <summary>
        /// Content page for NAMESPACE: updates all added scripts with the new namespace.
        /// </summary>
        private void BuildNamespaceRegion(VisualElement container)
        {
            // 1. New Namespace Configuration Box
            var configBox = UITKEditorHelper.BuildBox("Namespace Configuration");
            configBox.style.backgroundColor = COLOR_GREY_BOX;
            configBox.style.borderTopColor = COLOR_GREY_BORDER;
            configBox.style.borderBottomColor = COLOR_GREY_BORDER;
            configBox.style.borderLeftColor = COLOR_GREY_BORDER;
            configBox.style.borderRightColor = COLOR_GREY_BORDER;

            var descLabel = new Label("All added scripts below will have their namespace set to this value.")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    marginBottom = 8
                }
            };
            configBox.Add(descLabel);

            _newNamespaceTextField = new TextField("New Namespace") { value = _newNamespace };
            _newNamespaceTextField.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(this, "Modify New Namespace");
                _newNamespace = e.newValue;
            });
            configBox.Add(_newNamespaceTextField);
            container.Add(configBox);

            // 2. Added Target Scripts List Box
            SerializedObject serializedObject = new SerializedObject(this);

            var listSection = UITKEditorHelper.BuildAssetListSection(
                serializedObject,
                "_targetScripts",
                "Target Scripts",
                "Assigned Scripts",
                _targetScripts,
                () => {
                    Debug.Log("<color=#3B82F6>[Window_ScriptsModifier]</color> Target Scripts Updated");
                },
                extraButtonsBuilder: buttonRow =>
                {
                    var btnAddFolder = new Button(() =>
                    {
                        var folders = Selection.GetFiltered<DefaultAsset>(SelectionMode.Assets);
                        if (folders == null || folders.Length == 0)
                        {
                            Debug.LogWarning("<color=orange>[Window_ScriptsModifier]</color> Select folder(s) in Project view first.");
                            return;
                        }

                        int added = 0;
                        foreach (var folder in folders)
                        {
                            string folderPath = AssetDatabase.GetAssetPath(folder);
                            if (!Directory.Exists(folderPath)) continue;

                            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { folderPath });
                            foreach (string guid in guids)
                            {
                                string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                                if (script != null && !_targetScripts.Contains(script))
                                {
                                    _targetScripts.Add(script);
                                    added++;
                                }
                            }
                        }

                        serializedObject.Update();
                        Debug.Log($"<color=#3B82F6>[Window_ScriptsModifier]</color> Added: {added} scripts from folder(s)");
                    })
                    {
                        text = "Add Folder",
                        style =
                        {
                            flexGrow = 1,
                            marginLeft = 2,
                            marginRight = 2,
                            height = 24,
                            fontSize = 11,
                            backgroundColor = COLOR_OCEAN_BLUE,
                            color = Color.white
                        }
                    };
                    buttonRow.Insert(1, btnAddFolder);
                },
                showLoadAllButton: false
            );

            listSection.style.backgroundColor = COLOR_GREY_BOX;
            listSection.style.borderTopColor = COLOR_GREY_BORDER;
            listSection.style.borderBottomColor = COLOR_GREY_BORDER;
            listSection.style.borderLeftColor = COLOR_GREY_BORDER;
            listSection.style.borderRightColor = COLOR_GREY_BORDER;
            container.Add(listSection);

            // 3. Action Box
            var actionBox = UITKEditorHelper.BuildBox("Apply");
            actionBox.style.backgroundColor = COLOR_GREY_BOX;
            actionBox.style.borderTopColor = COLOR_GREY_BORDER;
            actionBox.style.borderBottomColor = COLOR_GREY_BORDER;
            actionBox.style.borderLeftColor = COLOR_GREY_BORDER;
            actionBox.style.borderRightColor = COLOR_GREY_BORDER;

            var applyBtn = new Button(ChangeNamespaces)
            {
                text = "Apply New Namespace",
                style =
                {
                    height = 34,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = COLOR_OCEAN_BLUE,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            actionBox.Add(applyBtn);
            container.Add(actionBox);
        }

        /// <summary>
        /// Content page for ARCHIVING: comments out or uncomments scripts in an archive folder.
        /// </summary>
        private void BuildArchivingRegion(VisualElement container)
        {
            var folderBox = UITKEditorHelper.BuildBox("Target Archive Folder");
            folderBox.style.backgroundColor = COLOR_GREY_BOX;
            folderBox.style.borderTopColor = COLOR_GREY_BORDER;
            folderBox.style.borderBottomColor = COLOR_GREY_BORDER;
            folderBox.style.borderLeftColor = COLOR_GREY_BORDER;
            folderBox.style.borderRightColor = COLOR_GREY_BORDER;

            var descLabel = new Label("Select a folder containing scripts to comment out or uncomment.")
            {
                style =
                {
                    fontSize = 12,
                    color = COLOR_FOREST_MIST,
                    marginBottom = 8
                }
            };
            folderBox.Add(descLabel);

            SerializedObject serializedObject = new SerializedObject(this);
            SerializedProperty folderProperty = serializedObject.FindProperty("_archiveFolderAsset");

            _archiveFolderPropertyField = new PropertyField(folderProperty, "Archive Folder");
            _archiveFolderPropertyField.Bind(serializedObject);
            folderBox.Add(_archiveFolderPropertyField);

            container.Add(folderBox);

            // Actions Box
            var actionBox = UITKEditorHelper.BuildBox("Archiving Actions");
            actionBox.style.backgroundColor = COLOR_GREY_BOX;
            actionBox.style.borderTopColor = COLOR_GREY_BORDER;
            actionBox.style.borderBottomColor = COLOR_GREY_BORDER;
            actionBox.style.borderLeftColor = COLOR_GREY_BORDER;
            actionBox.style.borderRightColor = COLOR_GREY_BORDER;

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };

            var commentBtn = new Button(CommentOutScripts)
            {
                text = "Comment Scripts",
                style =
                {
                    flexGrow = 1,
                    height = 32,
                    marginRight = 4,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = COLOR_OCEAN_BLUE,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            buttonRow.Add(commentBtn);

            var uncommentBtn = new Button(UncommentScripts)
            {
                text = "Uncomment Scripts",
                style =
                {
                    flexGrow = 1,
                    height = 32,
                    marginLeft = 4,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = COLOR_OCEAN_BLUE,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            buttonRow.Add(uncommentBtn);

            actionBox.Add(buttonRow);
            container.Add(actionBox);
        }

        /// <summary>
        /// Content page for SETTINGS: instructions and Reset to Defaults option.
        /// </summary>
        private void BuildSettingsRegion(VisualElement container)
        {
            var infoBox = UITKEditorHelper.BuildBox("Summary & Instructions");
            infoBox.style.backgroundColor = COLOR_GREY_BOX;
            infoBox.style.borderTopColor = COLOR_GREY_BORDER;
            infoBox.style.borderBottomColor = COLOR_GREY_BORDER;
            infoBox.style.borderLeftColor = COLOR_GREY_BORDER;
            infoBox.style.borderRightColor = COLOR_GREY_BORDER;

            var infoLabel = new Label(
                "Scripts Modifier Guide:\n\n" +
                "• Target Scripts: Add scripts individually, from selection, or in batch via 'Add Folder'.\n" +
                "• New Namespace: Updates all added scripts without needing an old namespace.\n" +
                "• Archiving: Comments out scripts safely using inner placeholders to avoid syntax errors.\n" +
                "• Backup: Remember to use Git to review and track script changes.")
            {
                style =
                {
                    fontSize = 12,
                    color = COLOR_FOREST_MIST,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            infoBox.Add(infoLabel);
            container.Add(infoBox);

            // Caching options block (Rule 7)
            var actionBox = UITKEditorHelper.BuildBox("Reset Options");
            actionBox.style.backgroundColor = COLOR_GREY_BOX;
            actionBox.style.borderTopColor = COLOR_DANGER_BORDER;
            actionBox.style.borderBottomColor = COLOR_DANGER_BORDER;
            actionBox.style.borderLeftColor = COLOR_DANGER_BORDER;
            actionBox.style.borderRightColor = COLOR_DANGER_BORDER;

            var resetBtn = new Button(ResetToDefaults)
            {
                text = "Reset to Defaults",
                style =
                {
                    height = 32,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = COLOR_DANGER_BG,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            actionBox.Add(resetBtn);
            container.Add(actionBox);
        }
        #endregion

        #region Operations / Methods
        /// <summary>
        /// Changes namespace for all assigned scripts in _targetScripts to _newNamespace.
        /// </summary>
        private void ChangeNamespaces()
        {
            Debug.Log("<color=#3B82F6>[Window_ScriptsModifier]</color> ChangeNamespaces: Start"); // Rule 10 (Keywords only)

            // Boundary checks (Rule 2)
            if (_targetScripts == null || _targetScripts.Count == 0)
            {
                Debug.LogError("<color=red>[Window_ScriptsModifier]</color> No scripts added in list.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_newNamespace))
            {
                Debug.LogError("<color=red>[Window_ScriptsModifier]</color> New namespace is empty.");
                return;
            }

            string trimmedNamespace = _newNamespace.Trim();
            int filesChanged = 0;
            string pattern = @"(^\s*namespace\s+)[A-Za-z0-9_.]+";

            foreach (var script in _targetScripts)
            {
                if (script == null) continue;

                string path = AssetDatabase.GetAssetPath(script);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    Debug.LogWarning($"<color=orange>[Window_ScriptsModifier]</color> File not found: {path}");
                    continue;
                }

                string content = File.ReadAllText(path);
                string newContent;

                if (Regex.IsMatch(content, pattern, RegexOptions.Multiline))
                {
                    newContent = Regex.Replace(content, pattern, $"$1{trimmedNamespace}", RegexOptions.Multiline);
                }
                else
                {
                    newContent = WrapInNamespace(content, trimmedNamespace);
                }

                if (newContent != content)
                {
                    Debug.Log($"<color=#3B82F6>[Window_ScriptsModifier]</color> Modifying: {Path.GetFileName(path)}");
                    File.WriteAllText(path, newContent);
                    filesChanged++;
                }
            }

            if (filesChanged > 0)
            {
                Debug.Log($"<color=green>[Window_ScriptsModifier]</color> Success: {filesChanged} scripts updated.");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("<color=orange>[Window_ScriptsModifier]</color> No changes made.");
            }
        }

        /// <summary>
        /// Wraps content that has no namespace inside a block namespace, keeping using directives outside.
        /// </summary>
        private static string WrapInNamespace(string content, string ns)
        {
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var usings = new StringBuilder();
            var body = new StringBuilder();
            bool inPreamble = true;

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (inPreamble && (trimmed.StartsWith("using ") || trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*")))
                {
                    usings.AppendLine(lines[i]);
                }
                else
                {
                    inPreamble = false;
                    body.AppendLine(lines[i]);
                }
            }

            return $"{usings.ToString().TrimEnd()}\n\nnamespace {ns}\n{{\n{body.ToString().TrimEnd()}\n}}\n";
        }

        /// <summary>
        /// Comments out all scripts in the selected archive folder.
        /// </summary>
        private void CommentOutScripts()
        {
            Debug.Log("<color=#3B82F6>[Window_ScriptsModifier]</color> CommentOutScripts: Start"); // Rule 10 (Keywords only)

            if (_archiveFolderAsset == null)
            {
                Debug.LogError("<color=red>[Window_ScriptsModifier]</color> Archive folder is null.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(_archiveFolderAsset);
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Debug.LogError($"<color=red>[Window_ScriptsModifier]</color> Folder does not exist: {path}");
                return;
            }

            ProcessFolderForCommenting(path, true);
        }

        /// <summary>
        /// Uncomments all scripts in the selected archive folder.
        /// </summary>
        private void UncommentScripts()
        {
            Debug.Log("<color=#3B82F6>[Window_ScriptsModifier]</color> UncommentScripts: Start"); // Rule 10 (Keywords only)

            if (_archiveFolderAsset == null)
            {
                Debug.LogError("<color=red>[Window_ScriptsModifier]</color> Archive folder is null.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(_archiveFolderAsset);
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Debug.LogError($"<color=red>[Window_ScriptsModifier]</color> Folder does not exist: {path}");
                return;
            }

            ProcessFolderForCommenting(path, false);
        }

        private void ProcessFolderForCommenting(string path, bool shouldComment)
        {
            Debug.Log($"<color=#3B82F6>[Window_ScriptsModifier]</color> Archiving in: {path}");
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Debug.LogError($"<color=red>[Window_ScriptsModifier]</color> Directory invalid: {path}");
                return;
            }

            string[] scriptFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            int filesChanged = 0;

            foreach (string filePath in scriptFiles)
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Debug.LogWarning($"<color=orange>[Window_ScriptsModifier]</color> File not found: {filePath}");
                    continue;
                }

                string content = File.ReadAllText(filePath);
                string newContent = content;

                if (shouldComment)
                {
                    if (!content.StartsWith("/*") && !content.EndsWith("*/"))
                    {
                        Debug.Log($"<color=#3B82F6>[Window_ScriptsModifier]</color> Commenting: {Path.GetFileName(filePath)}");
                        string escapedContent = content.Replace("*/", COMMENT_PLACEHOLDER);
                        newContent = "/*\n" + escapedContent + "\n*/";
                        filesChanged++;
                    }
                }
                else // Un-comment
                {
                    if (content.StartsWith("/*") && content.EndsWith("*/"))
                    {
                        Debug.Log($"<color=#3B82F6>[Window_ScriptsModifier]</color> Uncommenting: {Path.GetFileName(filePath)}");
                        int start = content.IndexOf("/*") + 2;
                        int end = content.LastIndexOf("*/");
                        string strippedContent = content.Substring(start, end - start).Trim();
                        newContent = strippedContent.Replace(COMMENT_PLACEHOLDER, "*/");
                        filesChanged++;
                    }
                }

                if (newContent != content)
                {
                    File.WriteAllText(filePath, newContent);
                }
            }

            if (filesChanged > 0)
            {
                Debug.Log($"<color=green>[Window_ScriptsModifier]</color> Success: {filesChanged} files processed.");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("<color=orange>[Window_ScriptsModifier]</color> No changes.");
            }
        }

        /// <summary>
        /// Rule 7 (Reset to Defaults option) & Rule 8 (Clear lists, don't re-instantiate).
        /// </summary>
        private void ResetToDefaults()
        {
            Debug.Log("<color=red>[Window_ScriptsModifier]</color> ResetToDefaults"); // Rule 10 (Keywords only)

            // Clear cached keys
            EditorPrefs.DeleteKey(PREF_KEY_NEW_NAMESPACE);
            EditorPrefs.DeleteKey(PREF_KEY_ARCHIVE_FOLDER_PATH);
            EditorPrefs.DeleteKey(PREF_KEY_ACTIVE_TAB);

            // Re-init variables to baseline
            _newNamespace = "New.Namespace";
            _archiveFolderAsset = null;
            _activeTab = TabType.NAMESPACE;

            // Clear list structures without re-instantiation (Rule 8)
            if (_targetScripts != null)
            {
                _targetScripts.Clear();
            }

            // Reload UI
            Close();
            ShowWindow();
        }
        #endregion
    }
#endif
}