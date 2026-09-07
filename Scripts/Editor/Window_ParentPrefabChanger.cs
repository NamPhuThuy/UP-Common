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
    /// UITK Editor Window to replace or change the parent Prefab Asset of selected GameObjects.
    /// Includes a toggle to keep or reset overrides, matching Unity's modern PrefabReplacing workflow.
    /// Complies strictly with Window_Template_UITK architecture and project coding rules.
    /// </summary>
    public class Window_ParentPrefabChanger : EditorWindow
    {
        #region Enums (Rule 3)
        public enum TabType
        {
            NONE = 0,
            REPLACE = 1,
            SETTINGS = 2
        }

        public enum MatchMode
        {
            NONE = 0,
            BY_NAME = 1,
            BY_HIERARCHY = 2
        }
        #endregion

        #region Private Fields
        [Header("Target Prefab")]
        [SerializeField] private GameObject _targetPrefab;

        [Header("Options")]
        [SerializeField] private bool _keepOverrides = true;
        [SerializeField] private bool _changeRootNameToAssetName = false;
        [SerializeField] private bool _autoResolveToInstanceRoot = true;
        [SerializeField] private bool _allowNonPrefabReplacement = true;
        [SerializeField] private MatchMode _matchMode = MatchMode.BY_NAME;

        [Header("Targets")]
        [SerializeField] private List<GameObject> _targetGameObjects = new List<GameObject>();

        // EditorPrefs keys (Rule 4)
        private const string PREF_KEY_TARGET_PREFAB_PATH = "NamPhuThuy_ParentPrefabChanger_TargetPrefabPath";
        private const string PREF_KEY_KEEP_OVERRIDES = "NamPhuThuy_ParentPrefabChanger_KeepOverrides";
        private const string PREF_KEY_CHANGE_NAME = "NamPhuThuy_ParentPrefabChanger_ChangeName";
        private const string PREF_KEY_AUTO_RESOLVE_ROOT = "NamPhuThuy_ParentPrefabChanger_AutoResolveRoot";
        private const string PREF_KEY_ALLOW_NON_PREFAB = "NamPhuThuy_ParentPrefabChanger_AllowNonPrefab";
        private const string PREF_KEY_MATCH_MODE = "NamPhuThuy_ParentPrefabChanger_MatchMode";
        private const string PREF_KEY_ACTIVE_TAB = "NamPhuThuy_ParentPrefabChanger_ActiveTab";

        // Paths & Signature config (Rule 4)
        private const string SIGNATURE_MARK_RELATIVE_PATH = "../../nam_phu_thuy.png";
        private const string WINDOW_TITLE = "Parent Prefab Changer";

        /*
            NOTE: Using static expression-bodied properties (COLOR => ...) instead of "static readonly".
            Dynamically evaluates EditorGUIUtility.isProSkin to match Unity's Dark/Light editor skin.
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
        private TabType _activeTab = TabType.REPLACE;

        // UI references
        private VisualElement _contentContainer;
        private VisualElement _tabHeaderContainer;
        private SerializedObject _serializedObject;
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Parent Prefab Changer")]
        public static void ShowWindow()
        {
            var window = GetWindow<Window_ParentPrefabChanger>(WINDOW_TITLE);
            window.minSize = new Vector2(500, 650);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            Debug.Log("<color=#3B82F6>[Window_ParentPrefabChanger]</color> OnEnable"); // Rule 10

            _serializedObject = new SerializedObject(this);

            // Load persisted settings (Rule 7)
            string savedPrefabPath = EditorPrefs.GetString(PREF_KEY_TARGET_PREFAB_PATH, "");
            if (!string.IsNullOrEmpty(savedPrefabPath))
            {
                _targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(savedPrefabPath);
            }

            _keepOverrides = EditorPrefs.GetBool(PREF_KEY_KEEP_OVERRIDES, true);
            _changeRootNameToAssetName = EditorPrefs.GetBool(PREF_KEY_CHANGE_NAME, false);
            _autoResolveToInstanceRoot = EditorPrefs.GetBool(PREF_KEY_AUTO_RESOLVE_ROOT, true);
            _allowNonPrefabReplacement = EditorPrefs.GetBool(PREF_KEY_ALLOW_NON_PREFAB, true);
            _matchMode = (MatchMode)EditorPrefs.GetInt(PREF_KEY_MATCH_MODE, (int)MatchMode.BY_NAME);
            _activeTab = (TabType)EditorPrefs.GetInt(PREF_KEY_ACTIVE_TAB, (int)TabType.REPLACE);
        }

        private void OnDisable()
        {
            Debug.Log("<color=#3B82F6>[Window_ParentPrefabChanger]</color> OnDisable"); // Rule 10

            // Save persisted settings (Rule 7)
            if (_targetPrefab != null)
            {
                string path = AssetDatabase.GetAssetPath(_targetPrefab);
                EditorPrefs.SetString(PREF_KEY_TARGET_PREFAB_PATH, path);
            }
            else
            {
                EditorPrefs.SetString(PREF_KEY_TARGET_PREFAB_PATH, "");
            }

            EditorPrefs.SetBool(PREF_KEY_KEEP_OVERRIDES, _keepOverrides);
            EditorPrefs.SetBool(PREF_KEY_CHANGE_NAME, _changeRootNameToAssetName);
            EditorPrefs.SetBool(PREF_KEY_AUTO_RESOLVE_ROOT, _autoResolveToInstanceRoot);
            EditorPrefs.SetBool(PREF_KEY_ALLOW_NON_PREFAB, _allowNonPrefabReplacement);
            EditorPrefs.SetInt(PREF_KEY_MATCH_MODE, (int)_matchMode);
            EditorPrefs.SetInt(PREF_KEY_ACTIVE_TAB, (int)_activeTab);
        }

        public void CreateGUI()
        {
            Debug.Log("[Window_ParentPrefabChanger] CreateGUI"); // Rule 10

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

            var signatureTex = AssetDatabase.LoadAssetAtPath<Texture2D>(resolvedPath);
            if (signatureTex != null)
            {
                signatureMark.style.backgroundImage = signatureTex;
            }
            else
            {
                Debug.LogWarning($"<color=orange>[Window_ParentPrefabChanger]</color> Missing Logo: {resolvedPath}");
                signatureMark.style.backgroundColor = COLOR_GREY_BOX;
            }
            headerRow.Add(signatureMark);

            // Titles
            var textColumn = new VisualElement { style = { flexGrow = 1 } };
            var mainTitle = new Label("Parent Prefab Changer")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 16,
                    color = COLOR_SKY_BLUE
                }
            };
            var subTitle = new Label("Swap Prefabs & Manage Overrides")
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

            bar.Add(CreateNavigationButton("Replace", TabType.REPLACE));
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
                case TabType.REPLACE:
                    BuildReplacePage(_contentContainer);
                    break;
                case TabType.SETTINGS:
                    BuildSettingsPage(_contentContainer);
                    break;
            }
        }
        #endregion

        #region Page Content Builders
        /// <summary>
        /// Main Replace Page: Target Prefab, Options (Keep Overrides toggle), Targets List, and Execute button.
        /// </summary>
        private void BuildReplacePage(VisualElement container)
        {
            // 1. Instructions / Description Box
            var infoBox = UITKEditorHelper.BuildBox("Instructions");
            infoBox.style.backgroundColor = COLOR_GREY_BOX;
            infoBox.style.borderTopColor = COLOR_GREY_BORDER;
            infoBox.style.borderBottomColor = COLOR_GREY_BORDER;
            infoBox.style.borderLeftColor = COLOR_GREY_BORDER;
            infoBox.style.borderRightColor = COLOR_GREY_BORDER;

            var infoLabel = new Label(
                "• Select target GameObjects in Hierarchy or assign them in the list below.\n" +
                "• Assign the new Prefab Asset to use as the parent prefab.\n" +
                "• Toggle 'Keep Overrides' to preserve modified properties or revert to defaults.\n" +
                "• Click 'Change Parent Prefabs' to execute with full Undo support.")
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

            // 2. Target Prefab Asset Box
            var prefabBox = UITKEditorHelper.BuildBox("New Parent Prefab");
            prefabBox.style.backgroundColor = COLOR_GREY_BOX;
            prefabBox.style.borderTopColor = COLOR_GREY_BORDER;
            prefabBox.style.borderBottomColor = COLOR_GREY_BORDER;
            prefabBox.style.borderLeftColor = COLOR_GREY_BORDER;
            prefabBox.style.borderRightColor = COLOR_GREY_BORDER;

            var prefabField = new ObjectField("Prefab Asset")
            {
                objectType = typeof(GameObject),
                value = _targetPrefab
            };

            var prefabHint = new Label(GetPrefabHintText())
            {
                style =
                {
                    fontSize = 11,
                    color = _targetPrefab != null ? COLOR_SKY_BLUE : COLOR_FOREST_MIST,
                    marginTop = 4,
                    whiteSpace = WhiteSpace.Normal
                }
            };

            prefabField.RegisterValueChangedCallback(evt =>
            {
                GameObject chosen = evt.newValue as GameObject;
                if (chosen == null)
                {
                    _targetPrefab = null;
                    prefabHint.text = "No Prefab Asset assigned.";
                    prefabHint.style.color = COLOR_FOREST_MIST;
                    return;
                }

                // If user selected a scene object, resolve its source prefab asset
                if (!PrefabUtility.IsPartOfPrefabAsset(chosen))
                {
                    GameObject sourceAsset = PrefabUtility.GetCorrespondingObjectFromSource(chosen);
                    if (sourceAsset != null)
                    {
                        chosen = sourceAsset;
                        prefabField.SetValueWithoutNotify(chosen);
                    }
                    else
                    {
                        Debug.LogError("<color=red>[Window_ParentPrefabChanger]</color> Object is not a valid Prefab Asset.");
                        _targetPrefab = null;
                        prefabField.SetValueWithoutNotify(null);
                        prefabHint.text = "Invalid object. Must be a Prefab Asset.";
                        prefabHint.style.color = COLOR_DANGER_BG;
                        return;
                    }
                }

                Undo.RecordObject(this, "Set Target Prefab");
                _targetPrefab = chosen;
                prefabHint.text = $"Asset: {AssetDatabase.GetAssetPath(_targetPrefab)}";
                prefabHint.style.color = COLOR_SKY_BLUE;
            });

            prefabBox.Add(prefabField);
            prefabBox.Add(prefabHint);
            container.Add(prefabBox);

            // 3. Options Box
            var optionsBox = UITKEditorHelper.BuildBox("Options");
            optionsBox.style.backgroundColor = COLOR_GREY_BOX;
            optionsBox.style.borderTopColor = COLOR_GREY_BORDER;
            optionsBox.style.borderBottomColor = COLOR_GREY_BORDER;
            optionsBox.style.borderLeftColor = COLOR_GREY_BORDER;
            optionsBox.style.borderRightColor = COLOR_GREY_BORDER;

            // Keep Overrides Toggle (User-requested feature)
            var keepOverridesToggle = new Toggle("Keep Overrides")
            {
                value = _keepOverrides,
                style = { unityFontStyleAndWeight = FontStyle.Bold }
            };
            keepOverridesToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(this, "Toggle Keep Overrides");
                _keepOverrides = evt.newValue;
            });
            optionsBox.Add(keepOverridesToggle);

            var keepOverridesDesc = new Label("When enabled, preserves existing property and component overrides on instances. When disabled, resets overrides to match the new prefab asset.")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    marginLeft = 20,
                    marginBottom = 8,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            optionsBox.Add(keepOverridesDesc);

            // Rename to Asset Name Toggle
            var renameToggle = new Toggle("Change Name to Prefab Name")
            {
                value = _changeRootNameToAssetName
            };
            renameToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(this, "Toggle Rename Option");
                _changeRootNameToAssetName = evt.newValue;
            });
            optionsBox.Add(renameToggle);

            var renameDesc = new Label("If enabled, renames the instance root GameObject to match the new Prefab Asset name.")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    marginLeft = 20,
                    marginBottom = 8,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            optionsBox.Add(renameDesc);

            // Auto Resolve Root Toggle
            var autoResolveToggle = new Toggle("Auto Resolve to Instance Root")
            {
                value = _autoResolveToInstanceRoot
            };
            autoResolveToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(this, "Toggle Auto Resolve Root");
                _autoResolveToInstanceRoot = evt.newValue;
            });
            optionsBox.Add(autoResolveToggle);

            var autoResolveDesc = new Label("If a child GameObject inside a prefab is selected, automatically targets its nearest prefab instance root.")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    marginLeft = 20,
                    marginBottom = 4,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            optionsBox.Add(autoResolveDesc);

            container.Add(optionsBox);

            // 4. Target GameObjects Box (Asset List Section)
            _serializedObject.Update();
            var targetsSection = UITKEditorHelper.BuildAssetListSection(
                _serializedObject,
                "_targetGameObjects",
                "Target GameObjects",
                "Assigned GameObjects",
                _targetGameObjects,
                () => {
                    Debug.Log("<color=#3B82F6>[Window_ParentPrefabChanger]</color> Targets List: Updated");
                },
                extraButtonsBuilder: row =>
                {
                    // Additional helper button: Load From Hierarchy
                    var btnLoadHierarchy = new Button(LoadFromHierarchySelection)
                    {
                        text = "From Hierarchy",
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
                    row.Insert(1, btnLoadHierarchy);
                },
                showLoadAllButton: false
            );

            targetsSection.style.backgroundColor = COLOR_GREY_BOX;
            targetsSection.style.borderTopColor = COLOR_GREY_BORDER;
            targetsSection.style.borderBottomColor = COLOR_GREY_BORDER;
            targetsSection.style.borderLeftColor = COLOR_GREY_BORDER;
            targetsSection.style.borderRightColor = COLOR_GREY_BORDER;

            var targetsHint = new Label("Tip: If the list is empty, current Hierarchy selection will be used directly.")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    marginTop = 6,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            targetsSection.Add(targetsHint);

            container.Add(targetsSection);

            // 5. Action Execution Box
            var actionBox = UITKEditorHelper.BuildBox("Action");
            actionBox.style.backgroundColor = COLOR_GREY_BOX;
            actionBox.style.borderTopColor = COLOR_GREY_BORDER;
            actionBox.style.borderBottomColor = COLOR_GREY_BORDER;
            actionBox.style.borderLeftColor = COLOR_GREY_BORDER;
            actionBox.style.borderRightColor = COLOR_GREY_BORDER;

            var runBtn = new Button(ChangeParentPrefabs)
            {
                text = "Change Parent Prefabs",
                style =
                {
                    height = 36,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13,
                    backgroundColor = COLOR_OCEAN_BLUE,
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
        /// Settings Page: Advanced Options & Reset to Defaults.
        /// </summary>
        private void BuildSettingsPage(VisualElement container)
        {
            var advancedBox = UITKEditorHelper.BuildBox("Advanced Settings");
            advancedBox.style.backgroundColor = COLOR_GREY_BOX;
            advancedBox.style.borderTopColor = COLOR_GREY_BORDER;
            advancedBox.style.borderBottomColor = COLOR_GREY_BORDER;
            advancedBox.style.borderLeftColor = COLOR_GREY_BORDER;
            advancedBox.style.borderRightColor = COLOR_GREY_BORDER;

            // Match Mode Field
            var matchModeField = new EnumField("Object Match Mode", _matchMode);
            matchModeField.Init(_matchMode);
            matchModeField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(this, "Modify Match Mode");
                _matchMode = (MatchMode)evt.newValue;
            });
            advancedBox.Add(matchModeField);

            var matchDesc = new Label("ByName: matches child objects by name. ByHierarchy: matches by relative hierarchy structure.")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    marginLeft = 20,
                    marginBottom = 8,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            advancedBox.Add(matchDesc);

            // Allow Non-Prefab Replacement
            var nonPrefabToggle = new Toggle("Allow Non-Prefab Replacement")
            {
                value = _allowNonPrefabReplacement
            };
            nonPrefabToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(this, "Toggle Non-Prefab Replacement");
                _allowNonPrefabReplacement = evt.newValue;
            });
            advancedBox.Add(nonPrefabToggle);

            var nonPrefabDesc = new Label("If a selected GameObject is not a prefab instance, replaces it with a new instance of the target prefab at the same transform location.")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    marginLeft = 20,
                    marginBottom = 4,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            advancedBox.Add(nonPrefabDesc);

            container.Add(advancedBox);

            // Caching options block (Rule 7)
            var resetBox = UITKEditorHelper.BuildBox("Defaults & Reset");
            resetBox.style.backgroundColor = COLOR_GREY_BOX;
            resetBox.style.borderTopColor = COLOR_DANGER_BORDER;
            resetBox.style.borderBottomColor = COLOR_DANGER_BORDER;
            resetBox.style.borderLeftColor = COLOR_DANGER_BORDER;
            resetBox.style.borderRightColor = COLOR_DANGER_BORDER;

            var resetLabel = new Label("Clear cached EditorPrefs preferences and restore all options to their baseline defaults.")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    marginBottom = 8,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            resetBox.Add(resetLabel);

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
            resetBox.Add(resetBtn);
            container.Add(resetBox);
        }
        #endregion

        #region Operations / Methods
        /// <summary>
        /// Changes the parent Prefab Asset of the target GameObjects.
        /// </summary>
        private void ChangeParentPrefabs()
        {
            Debug.Log("<color=#3B82F6>[Window_ParentPrefabChanger]</color> Action: Start"); // Rule 10

            // Safety & boundary checks (Rule 2)
            if (_targetPrefab == null)
            {
                Debug.LogError("<color=red>[Window_ParentPrefabChanger]</color> Target Prefab is not assigned.");
                return;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(_targetPrefab))
            {
                Debug.LogError("<color=red>[Window_ParentPrefabChanger]</color> Target object must be a Prefab Asset from the Project folder.");
                return;
            }

            _serializedObject.Update();

            // Collect target candidates (assigned list or Hierarchy selection)
            List<GameObject> candidates = new List<GameObject>();
            if (_targetGameObjects != null && _targetGameObjects.Count > 0)
            {
                for (int i = 0; i < _targetGameObjects.Count; i++)
                {
                    GameObject go = _targetGameObjects[i];
                    if (go == null) continue;
                    candidates.Add(go);
                }
            }
            else
            {
                GameObject[] selected = Selection.gameObjects;
                for (int i = 0; i < selected.Length; i++)
                {
                    GameObject go = selected[i];
                    if (go == null) continue;
                    candidates.Add(go);
                }
            }

            if (candidates.Count == 0)
            {
                Debug.LogError("<color=red>[Window_ParentPrefabChanger]</color> No valid GameObjects assigned or selected in Hierarchy.");
                return;
            }

            // Resolve and deduplicate instance roots
            HashSet<GameObject> uniqueRoots = new HashSet<GameObject>();
            List<GameObject> resolvedTargets = new List<GameObject>();

            for (int i = 0; i < candidates.Count; i++)
            {
                GameObject candidate = candidates[i];
                if (candidate == null) continue;

                if (PrefabUtility.IsPartOfPrefabInstance(candidate))
                {
                    GameObject root = _autoResolveToInstanceRoot
                        ? PrefabUtility.GetNearestPrefabInstanceRoot(candidate)
                        : candidate;

                    if (root == null)
                        root = candidate;

                    if (uniqueRoots.Add(root))
                    {
                        resolvedTargets.Add(root);
                    }
                }
                else
                {
                    if (uniqueRoots.Add(candidate))
                    {
                        resolvedTargets.Add(candidate);
                    }
                }
            }

            if (resolvedTargets.Count == 0)
            {
                Debug.LogError("<color=red>[Window_ParentPrefabChanger]</color> Resolved 0 valid targets.");
                return;
            }

            // Prepare replacement settings
            var settings = new PrefabReplacingSettings
            {
                prefabOverridesOptions = _keepOverrides
                    ? PrefabOverridesOptions.KeepAllPossibleOverrides
                    : PrefabOverridesOptions.ClearAllNonDefaultOverrides,
                changeRootNameToAssetName = _changeRootNameToAssetName,
                objectMatchMode = _matchMode == MatchMode.BY_HIERARCHY
                    ? ObjectMatchMode.ByHierarchy
                    : ObjectMatchMode.ByName
            };

            // Wrap entire batch in a single Undo group
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Change Parent Prefabs");
            int undoGroup = Undo.GetCurrentGroup();

            int replacedCount = 0;
            int failedCount = 0;
            List<GameObject> newSelection = new List<GameObject>();

            try
            {
                for (int i = 0; i < resolvedTargets.Count; i++)
                {
                    GameObject target = resolvedTargets[i];
                    if (target == null) continue;

                    if (PrefabUtility.IsPartOfPrefabInstance(target))
                    {
                        // Check if target is a valid instance root
                        if (!PrefabUtility.IsAnyPrefabInstanceRoot(target))
                        {
                            Debug.LogWarning($"<color=orange>[Window_ParentPrefabChanger]</color> Skipped {target.name}: Not a prefab root.");
                            failedCount++;
                            continue;
                        }

                        // Replace Prefab Asset
                        PrefabUtility.ReplacePrefabAssetOfPrefabInstance(target, _targetPrefab, settings, InteractionMode.UserAction);

                        // If not keeping overrides, revert all overrides to match new asset defaults
                        if (!_keepOverrides)
                        {
                            PrefabUtility.RevertPrefabInstance(target, InteractionMode.UserAction);
                        }

                        newSelection.Add(target);
                        replacedCount++;
                    }
                    else
                    {
                        // Handle plain scene GameObjects
                        if (!_allowNonPrefabReplacement)
                        {
                            Debug.LogWarning($"<color=orange>[Window_ParentPrefabChanger]</color> Skipped {target.name}: Not a prefab instance.");
                            failedCount++;
                            continue;
                        }

                        Transform oldTransform = target.transform;
                        Transform parent = oldTransform.parent;
                        int siblingIndex = oldTransform.GetSiblingIndex();
                        Vector3 localPos = oldTransform.localPosition;
                        Quaternion localRot = oldTransform.localRotation;
                        Vector3 localScale = oldTransform.localScale;
                        string originalName = target.name;
                        int layer = target.layer;
                        string tag = target.tag;

                        GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(_targetPrefab, target.scene);
                        Undo.RegisterCreatedObjectUndo(newInstance, "Replace with Prefab");

                        newInstance.transform.SetParent(parent, false);
                        newInstance.transform.localPosition = localPos;
                        newInstance.transform.localRotation = localRot;
                        newInstance.transform.localScale = localScale;
                        newInstance.transform.SetSiblingIndex(siblingIndex);
                        newInstance.layer = layer;
                        newInstance.tag = tag;

                        if (!_changeRootNameToAssetName)
                        {
                            newInstance.name = originalName;
                        }

                        Undo.DestroyObjectImmediate(target);

                        newSelection.Add(newInstance);
                        replacedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red>[Window_ParentPrefabChanger]</color> Exception: {ex.Message}");
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            if (newSelection.Count > 0)
            {
                Selection.objects = newSelection.ToArray();
            }

            Debug.Log($"<color=green>[Window_ParentPrefabChanger]</color> Success: Replaced {replacedCount}, Failed: {failedCount}"); // Rule 10
        }

        /// <summary>
        /// Populates the target list directly from current Hierarchy selection.
        /// </summary>
        private void LoadFromHierarchySelection()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("<color=orange>[Window_ParentPrefabChanger]</color> Hierarchy selection is empty.");
                return;
            }

            _serializedObject.Update();
            _targetGameObjects.Clear(); // Rule 8: clear without re-instantiation

            for (int i = 0; i < selected.Length; i++)
            {
                GameObject go = selected[i];
                if (go != null && !_targetGameObjects.Contains(go))
                {
                    _targetGameObjects.Add(go);
                }
            }

            _serializedObject.Update();
            Debug.Log($"<color=#3B82F6>[Window_ParentPrefabChanger]</color> Loaded {selected.Length} objects from Hierarchy.");
        }

        /// <summary>
        /// Returns a formatted hint string for the current target prefab.
        /// </summary>
        private string GetPrefabHintText()
        {
            if (_targetPrefab == null)
            {
                return "Drop a Prefab Asset here to use as the new parent prefab.";
            }

            string path = AssetDatabase.GetAssetPath(_targetPrefab);
            return $"Asset: {path}";
        }

        /// <summary>
        /// Resets persisted EditorPrefs and in-memory properties to baseline defaults (Rules 7, 8, 11).
        /// </summary>
        private void ResetToDefaults()
        {
            Debug.Log("<color=red>[Window_ParentPrefabChanger]</color> ResetToDefaults"); // Rule 10

            // Clear cached keys
            EditorPrefs.DeleteKey(PREF_KEY_TARGET_PREFAB_PATH);
            EditorPrefs.DeleteKey(PREF_KEY_KEEP_OVERRIDES);
            EditorPrefs.DeleteKey(PREF_KEY_CHANGE_NAME);
            EditorPrefs.DeleteKey(PREF_KEY_AUTO_RESOLVE_ROOT);
            EditorPrefs.DeleteKey(PREF_KEY_ALLOW_NON_PREFAB);
            EditorPrefs.DeleteKey(PREF_KEY_MATCH_MODE);
            EditorPrefs.DeleteKey(PREF_KEY_ACTIVE_TAB);

            // Re-initialize in-memory variables (Rule 11)
            _targetPrefab = null;
            _keepOverrides = true;
            _changeRootNameToAssetName = false;
            _autoResolveToInstanceRoot = true;
            _allowNonPrefabReplacement = true;
            _matchMode = MatchMode.BY_NAME;
            _activeTab = TabType.REPLACE;

            // Clear list structures without re-instantiation (Rule 8)
            if (_targetGameObjects != null)
            {
                _targetGameObjects.Clear();
            }

            // Reload UI
            Close();
            ShowWindow();
        }
        #endregion
    }
#endif
}
