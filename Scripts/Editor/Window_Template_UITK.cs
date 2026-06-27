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

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace NamPhuThuy.Common
{
#if UNITY_EDITOR
    /// <summary>
    /// Premium UITK Editor Window Template with a blue-vibe color palette,
    /// multiple sub-pages, signature mark branding, and compliance with project styling rules.
    /// Uses grey-scale layout elements with blue-palette clickable highlights.
    /// </summary>
    public class Window_Template_UITK : EditorWindow
    {
        #region Enums (Rule 3)
        public enum TabType
        {
            NONE = 0,
            REGION_1 = 1,
            REGION_2 = 2,
            REGION_3 = 3
        }
        #endregion

        #region Private Fields
        [Header("Template Settings")]
        [SerializeField] private string _exampleText = "Editable value";
        [SerializeField] private DefaultAsset _exampleFolderAsset; // Rule 6 (Folder fields as DefaultAsset)
        [SerializeField] private List<Object> _demoList = new List<Object>();

        // EditorPrefs keys (Rule 4)
        private const string PREF_KEY_EXAMPLE_TEXT_UITK = "NamPhuThuy_TemplateUITK_ExampleText";
        private const string PREF_KEY_EXAMPLE_FOLDER_PATH = "NamPhuThuy_TemplateUITK_FolderPath";
        private const string PREF_KEY_ACTIVE_TAB_UITK = "NamPhuThuy_TemplateUITK_ActiveTab";
        
        // Paths & Signature config (Rule 4)
        private const string SIGNATURE_MARK_RELATIVE_PATH = "../../nam_phu_thuy.png";
        private const string WINDOW_TITLE = "Template UITK (Demo)";

        /*
            NOTE: Using static expression-bodied properties (COLOR => new Color(...)) instead of "static readonly".
            Because Color is a struct (value type), returning it from a getter method constructs it on the stack
            on demand. This takes 0 bytes of persistent static memory in the AppDomain, unlike a static readonly field.
        */
        private static Color COLOR_EDITOR_BG => new Color(0.22f, 0.22f, 0.22f, 1f);          // Unity Editor Default Grey
        private static Color COLOR_GREY_BOX => new Color(0.16f, 0.16f, 0.16f, 0.6f);          // Grey panel background (grey-scale)
        private static Color COLOR_GREY_BORDER => new Color(0.26f, 0.26f, 0.26f, 0.8f);       // Grey panel border (grey-scale)
        
        private static Color COLOR_OCEAN_BLUE => new Color(0.0f, 0.47f, 0.74f, 1f);          // Clickable Blue-Palette Primary (Water/Ocean)
        private static Color COLOR_SKY_BLUE => new Color(0.53f, 0.8f, 0.92f, 1f);            // Clickable Blue-Palette Highlight (Sky)
        private static Color COLOR_FOREST_MIST => new Color(0.8f, 0.8f, 0.8f, 1f);           // Neutral Text color
        
        private static Color COLOR_TAB_INACTIVE_BG => new Color(0.16f, 0.16f, 0.16f, 1f);     // Inactive tab grey background
        private static Color COLOR_TAB_INACTIVE_BORDER => new Color(0.11f, 0.11f, 0.11f, 1f); // Inactive tab grey border
        
        private static Color COLOR_DANGER_BG => new Color(0.55f, 0.15f, 0.15f, 1f);          // Red background for danger actions
        private static Color COLOR_DANGER_BORDER => new Color(0.6f, 0.2f, 0.2f, 0.8f);        // Red border for danger actions

        // Tab state
        private TabType _activeTab = TabType.REGION_1;

        // UI references
        private VisualElement _contentContainer;
        private VisualElement _tabHeaderContainer;
        private TextField _exampleTextField;
        private PropertyField _folderPropertyField;
        private PropertyField _demoListPropertyField;
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Template (UITK)")]
        public static void ShowWindow()
        {
            var window = GetWindow<Window_Template_UITK>(WINDOW_TITLE);
            window.minSize = new Vector2(500, 650);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            Debug.Log("<color=#3B82F6>[Window_Template_UITK]</color> OnEnable"); // Rule 10 (Keywords only)

            // Load persisted settings (Rule 7)
            _exampleText = EditorPrefs.GetString(PREF_KEY_EXAMPLE_TEXT_UITK, "Default Editable Value");
            _activeTab = (TabType)EditorPrefs.GetInt(PREF_KEY_ACTIVE_TAB_UITK, (int)TabType.REGION_1);

            string savedPath = EditorPrefs.GetString(PREF_KEY_EXAMPLE_FOLDER_PATH, "");
            if (!string.IsNullOrEmpty(savedPath))
            {
                _exampleFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(savedPath);
            }
        }

        private void OnDisable()
        {
            Debug.Log("<color=#3B82F6>[Window_Template_UITK]</color> OnDisable"); // Rule 10 (Keywords only)

            // Save persisted settings (Rule 7)
            EditorPrefs.SetString(PREF_KEY_EXAMPLE_TEXT_UITK, _exampleText);
            EditorPrefs.SetInt(PREF_KEY_ACTIVE_TAB_UITK, (int)_activeTab);

            if (_exampleFolderAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(_exampleFolderAsset);
                EditorPrefs.SetString(PREF_KEY_EXAMPLE_FOLDER_PATH, path);
            }
            else
            {
                EditorPrefs.SetString(PREF_KEY_EXAMPLE_FOLDER_PATH, "");
            }
        }

        public void CreateGUI()
        {
            Debug.Log("[Window_Template_UITK] CreateGUI"); // Rule 10 (Keywords only)

            var root = rootVisualElement;
            root.style.backgroundColor = COLOR_EDITOR_BG; // Standard Unity Grey Background (grey-scale)
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
                    backgroundColor = COLOR_GREY_BORDER, // Grey separator line
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
                Debug.LogWarning($"<color=orange>[Window_Template_UITK]</color> Missing Logo: {resolvedPath}");
                // Setup temporary placeholder color
                signatureMark.style.backgroundColor = COLOR_GREY_BOX;
            }
            headerRow.Add(signatureMark);

            // Titles
            var textColumn = new VisualElement { style = { flexGrow = 1 } };
            var mainTitle = new Label("Window Template (UITK)")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 16,
                    color = COLOR_SKY_BLUE // Nature Sky Accent (Blue clickable/interactive highlight)
                }
            };
            var subTitle = new Label("Demo Layout Structure")
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

            bar.Add(CreateNavigationButton("Region 1", TabType.REGION_1));
            bar.Add(CreateNavigationButton("Region 2", TabType.REGION_2));
            bar.Add(CreateNavigationButton("Region 3", TabType.REGION_3));

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
                    backgroundColor = isActive ? COLOR_OCEAN_BLUE : COLOR_TAB_INACTIVE_BG, // Active tabs use ocean blue; inactive use grey
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
                case TabType.REGION_1:
                    BuildRegion1(_contentContainer);
                    break;
                case TabType.REGION_2:
                    BuildRegion2(_contentContainer);
                    break;
                case TabType.REGION_3:
                    BuildRegion3(_contentContainer);
                    break;
            }
        }
        #endregion

        #region Page Content Builders
        /// <summary>
        /// Content page for REGION_1.
        /// </summary>
        private void BuildRegion1(VisualElement container)
        {
            var infoBox = UITKEditorHelper.BuildBox("Box 1");
            infoBox.style.backgroundColor = COLOR_GREY_BOX; // Grey-scale container
            infoBox.style.borderTopColor = COLOR_GREY_BORDER;
            infoBox.style.borderBottomColor = COLOR_GREY_BORDER;
            infoBox.style.borderLeftColor = COLOR_GREY_BORDER;
            infoBox.style.borderRightColor = COLOR_GREY_BORDER;

            var infoLabel = new Label(
                "Description:\n\n" +
                "• Bullet point description 1\n" +
                "• Bullet point description 2\n" +
                "• Bullet point description 3")
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

            // Simple demo action box
            var actionBox = UITKEditorHelper.BuildBox("Box 2");
            actionBox.style.backgroundColor = COLOR_GREY_BOX; // Grey-scale container
            actionBox.style.borderTopColor = COLOR_GREY_BORDER;
            actionBox.style.borderBottomColor = COLOR_GREY_BORDER;
            actionBox.style.borderLeftColor = COLOR_GREY_BORDER;
            actionBox.style.borderRightColor = COLOR_GREY_BORDER;

            var runBtn = new Button(Action1)
            {
                text = "Button 1",
                style =
                {
                    height = 34,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = COLOR_OCEAN_BLUE, // Clickable button uses blue-palette
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            actionBox.Add(runBtn);
            container.Add(actionBox);
        }

        /// <summary>
        /// Content page for REGION_2.
        /// </summary>
        private void BuildRegion2(VisualElement container)
        {
            SerializedObject serializedObject = new SerializedObject(this);

            var listSection = UITKEditorHelper.BuildAssetListSection(
                serializedObject,
                "_demoList",
                "List",
                "Assigned Assets",
                _demoList,
                () => {
                    Debug.Log("<color=#3B82F6>[Window_Template_UITK]</color> Region 2: Updated"); // Rule 10 (Keywords only)
                },
                showLoadAllButton: false // Hide default dialog-based find all
            );

            // Style with a custom grey border
            listSection.style.backgroundColor = COLOR_GREY_BOX; // Grey-scale container
            listSection.style.borderTopColor = COLOR_GREY_BORDER;
            listSection.style.borderBottomColor = COLOR_GREY_BORDER;
            listSection.style.borderLeftColor = COLOR_GREY_BORDER;
            listSection.style.borderRightColor = COLOR_GREY_BORDER;

            container.Add(listSection);
        }

        /// <summary>
        /// Content page for REGION_3.
        /// </summary>
        private void BuildRegion3(VisualElement container)
        {
            var configBox = UITKEditorHelper.BuildBox("Box 1");
            configBox.style.backgroundColor = COLOR_GREY_BOX; // Grey-scale container
            configBox.style.borderTopColor = COLOR_GREY_BORDER;
            configBox.style.borderBottomColor = COLOR_GREY_BORDER;
            configBox.style.borderLeftColor = COLOR_GREY_BORDER;
            configBox.style.borderRightColor = COLOR_GREY_BORDER;

            // Bind values manually to support immediate Undo recording
            _exampleTextField = new TextField("Input Text") { value = _exampleText };
            _exampleTextField.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(this, "Modify Example Text");
                _exampleText = e.newValue;
            });
            configBox.Add(_exampleTextField);

            // Bind target folder configuration (Rule 6)
            SerializedObject serializedObject = new SerializedObject(this);
            SerializedProperty folderProperty = serializedObject.FindProperty("_exampleFolderAsset");

            _folderPropertyField = new PropertyField(folderProperty, "Input Folder");
            _folderPropertyField.Bind(serializedObject);
            configBox.Add(_folderPropertyField);

            container.Add(configBox);

            // Caching options block (Rule 7)
            var actionBox = UITKEditorHelper.BuildBox("Box 2");
            actionBox.style.backgroundColor = COLOR_GREY_BOX; // Grey-scale container
            actionBox.style.borderTopColor = COLOR_DANGER_BORDER;
            actionBox.style.borderBottomColor = COLOR_DANGER_BORDER;
            actionBox.style.borderLeftColor = COLOR_DANGER_BORDER;
            actionBox.style.borderRightColor = COLOR_DANGER_BORDER;

            var resetBtn = new Button(ResetToDefaults)
            {
                text = "Button Reset",
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
        /// Trigger sample system operations.
        /// </summary>
        private void Action1()
        {
            Debug.Log("<color=#3B82F6>[Window_Template_UITK]</color> Action 1: Start"); // Rule 10 (Keywords only)
            
            // Check boundary conditions (Rule 2)
            if (_exampleFolderAsset == null)
            {
                Debug.LogWarning("<color=orange>[Window_Template_UITK]</color> Missing Folder"); // Rule 10 (Keywords only)
            }
            else
            {
                string path = AssetDatabase.GetAssetPath(_exampleFolderAsset);
                Debug.Log($"<color=green>[Window_Template_UITK]</color> Success Path: {path}"); // Rule 10 (Keywords only)
            }
        }

        /// <summary>
        /// Rule 7 (Reset to Defaults option) & Rule 8 (Clear lists, don't re-instantiate).
        /// </summary>
        private void ResetToDefaults()
        {
            Debug.Log("<color=red>[Window_Template_UITK]</color> ResetToDefaults"); // Rule 10 (Keywords only)

            // Clear cached keys
            EditorPrefs.DeleteKey(PREF_KEY_EXAMPLE_TEXT_UITK);
            EditorPrefs.DeleteKey(PREF_KEY_EXAMPLE_FOLDER_PATH);
            EditorPrefs.DeleteKey(PREF_KEY_ACTIVE_TAB_UITK);

            // Re-init variables to baseline
            _exampleText = "Default Editable Value";
            _exampleFolderAsset = null;
            _activeTab = TabType.REGION_1;

            // Clear list structures without re-instantiation (Rule 8)
            if (_demoList != null)
            {
                _demoList.Clear();
            }

            // Reload UI
            Close();
            ShowWindow();
        }
        #endregion
    }
#endif
}