// ───────────────────────────────────────────────────────────────────────
// RULES:
// 1. PROCESS: Use Debug.Log for trace steps.
// 2. SAFETY: Use Debug.LogError in null/boundary checks.
// ───────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using NamPhuThuy.Common;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;

namespace NamPhuThuy.Common
{
    public class Window_AssetNameModifier : EditorWindow
    {
        #region Private Fields
        // EditorPrefs Keys
        private const string PREF_KEY_ASSET_A_PATH = "NamPhuThuy_AssetNameModifier_AssetAPath";
        private const string PREF_KEY_ASSET_B_PATH = "NamPhuThuy_AssetNameModifier_AssetBPath";
        private const string PREF_KEY_TARGET_FOLDER_PATH = "NamPhuThuy_AssetNameModifier_TargetFolderPath";
        private const string PREF_KEY_BASE_NAME_PREFIX = "NamPhuThuy_AssetNameModifier_BaseNamePrefix";
        private const string PREF_KEY_SUFFIX_FORMAT = "NamPhuThuy_AssetNameModifier_SuffixFormat";
        private const string PREF_KEY_START_INDEX = "NamPhuThuy_AssetNameModifier_StartIndex";
        private const string PREF_KEY_INCLUDE_SUBFOLDERS = "NamPhuThuy_AssetNameModifier_IncludeSubfolders";
        private const string PREF_KEY_FILTER_BY_EXTENSION = "NamPhuThuy_AssetNameModifier_FilterByExtension";

        // Swap functionality
        private Object _assetA;
        private Object _assetB;

        // Batch rename functionality
        private DefaultAsset _targetFolder;
        private List<Object> _assetsInFolder = new List<Object>();
        private string _baseNamePrefix = "Asset";
        private string _suffixFormat = "_{0:00}"; // _{0:00} gives _01, _02, etc.
        private int _startIndex = 1;
        private bool _includeSubfolders = false;
        private string _filterByExtension = ""; // Empty = all types

        // UI references
        private VisualElement _listContainer;
        private Button _applyBtn;
        private ObjectField _assetAField;
        private ObjectField _assetBField;
        private ObjectField _folderField;
        private TextField _baseNameField;
        private TextField _suffixFormatField;
        private IntegerField _startIndexField;
        private Toggle _subfoldersToggle;
        private TextField _filterExtensionField;
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Asset Name Modifier")]
        public static void ShowWindow()
        {
            Window_AssetNameModifier window = GetWindow<Window_AssetNameModifier>("Asset Name Modifier");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            Debug.Log("[AssetNameModifier] OnEnable");
            
            // Load saved preferences
            string pathA = EditorPrefs.GetString(PREF_KEY_ASSET_A_PATH, "");
            if (!string.IsNullOrEmpty(pathA)) _assetA = AssetDatabase.LoadAssetAtPath<Object>(pathA);
            
            string pathB = EditorPrefs.GetString(PREF_KEY_ASSET_B_PATH, "");
            if (!string.IsNullOrEmpty(pathB)) _assetB = AssetDatabase.LoadAssetAtPath<Object>(pathB);
            
            string targetPath = EditorPrefs.GetString(PREF_KEY_TARGET_FOLDER_PATH, "");
            if (!string.IsNullOrEmpty(targetPath)) _targetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetPath);
            
            _baseNamePrefix = EditorPrefs.GetString(PREF_KEY_BASE_NAME_PREFIX, "Asset");
            _suffixFormat = EditorPrefs.GetString(PREF_KEY_SUFFIX_FORMAT, "_{0:00}");
            _startIndex = EditorPrefs.GetInt(PREF_KEY_START_INDEX, 1);
            _includeSubfolders = EditorPrefs.GetBool(PREF_KEY_INCLUDE_SUBFOLDERS, false);
            _filterByExtension = EditorPrefs.GetString(PREF_KEY_FILTER_BY_EXTENSION, "");
        }

        private void OnDisable()
        {
            Debug.Log("[AssetNameModifier] OnDisable");
            
            // Save preferences
            EditorPrefs.SetString(PREF_KEY_ASSET_A_PATH, _assetA != null ? AssetDatabase.GetAssetPath(_assetA) : "");
            EditorPrefs.SetString(PREF_KEY_ASSET_B_PATH, _assetB != null ? AssetDatabase.GetAssetPath(_assetB) : "");
            EditorPrefs.SetString(PREF_KEY_TARGET_FOLDER_PATH, _targetFolder != null ? AssetDatabase.GetAssetPath(_targetFolder) : "");
            
            EditorPrefs.SetString(PREF_KEY_BASE_NAME_PREFIX, _baseNamePrefix);
            EditorPrefs.SetString(PREF_KEY_SUFFIX_FORMAT, _suffixFormat);
            EditorPrefs.SetInt(PREF_KEY_START_INDEX, _startIndex);
            EditorPrefs.SetBool(PREF_KEY_INCLUDE_SUBFOLDERS, _includeSubfolders);
            EditorPrefs.SetString(PREF_KEY_FILTER_BY_EXTENSION, _filterByExtension);
        }

        public void CreateGUI()
        {
            Debug.Log("[AssetNameModifier] CreateGUI");
            var root = rootVisualElement;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;
            root.style.paddingTop = 20;
            root.style.paddingBottom = 20;

            // ── Header Section ──
            var header = new Label("Name Modifier")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16, unityTextAlign = TextAnchor.MiddleCenter, marginBottom = 10 }
            };
            root.Add(header);

            var helpBox = new HelpBox("Swap/rename assets.", HelpBoxMessageType.Info);
            root.Add(helpBox);

            var mainScroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1, marginTop = 10 } };
            root.Add(mainScroll);

            // ── Content Sections ──
            mainScroll.Add(BuildSwapSection());
            mainScroll.Add(BuildBatchRenameSection());
            mainScroll.Add(BuildUtilitySection());
        }
        #endregion

        #region UI Builders
        /// <summary>
        /// Creates a reusable box style for visually grouping elements
        /// </summary>
        

        private VisualElement BuildSwapSection()
        {
            var box = UITKEditorHelper.BuildBox("Swap Name");

            var desc = new HelpBox("Swap names of two selected assets.", HelpBoxMessageType.None);
            box.Add(desc);

            _assetAField = new ObjectField("Asset A")
            {
                objectType = typeof(Object),
                value = _assetA
            };
            _assetAField.RegisterValueChangedCallback(e =>
            {
                _assetA = e.newValue;
                EditorPrefs.SetString(PREF_KEY_ASSET_A_PATH, _assetA != null ? AssetDatabase.GetAssetPath(_assetA) : "");
            });
            box.Add(_assetAField);

            _assetBField = new ObjectField("Asset B")
            {
                objectType = typeof(Object),
                value = _assetB
            };
            _assetBField.RegisterValueChangedCallback(e =>
            {
                _assetB = e.newValue;
                EditorPrefs.SetString(PREF_KEY_ASSET_B_PATH, _assetB != null ? AssetDatabase.GetAssetPath(_assetB) : "");
            });
            box.Add(_assetBField);

            // Buttons row
            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };

            var useSelectedBtn = new Button(() =>
            {
                Debug.Log("[AssetNameModifier] Assigning selected assets...");
                AssignSelectedAssets();
                _assetAField.value = _assetA;
                _assetBField.value = _assetB;
            })
            {
                text = "Use Selected",
                style = { flexGrow = 1, height = 25 }
            };
            buttonRow.Add(useSelectedBtn);

            var clearBtn = new Button(() =>
            {
                Debug.Log("[AssetNameModifier] Clearing swap inputs.");
                _assetA = null;
                _assetB = null;
                _assetAField.value = null;
                _assetBField.value = null;
            })
            {
                text = "Clear",
                style = { flexGrow = 1, height = 25 }
            };
            buttonRow.Add(clearBtn);
            box.Add(buttonRow);

            var swapBtn = new Button(PerformSwapWithUndo)
            {
                text = "Swap",
                style = { height = 35, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 5 }
            };
            box.Add(swapBtn);

            return box;
        }

        private VisualElement BuildBatchRenameSection()
        {
            var box = UITKEditorHelper.BuildBox("Batch Rename");

            var desc = new HelpBox("Rename assets incrementally.", HelpBoxMessageType.None);
            box.Add(desc);

            // Folder Field
            _folderField = new ObjectField("Folder")
            {
                objectType = typeof(DefaultAsset),
                value = _targetFolder
            };
            _folderField.RegisterValueChangedCallback(e =>
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
            box.Add(_folderField);

            // Base Name
            _baseNameField = new TextField("Base") { value = _baseNamePrefix };
            _baseNameField.RegisterValueChangedCallback(e =>
            {
                _baseNamePrefix = e.newValue;
                EditorPrefs.SetString(PREF_KEY_BASE_NAME_PREFIX, _baseNamePrefix);
                RefreshListUI();
            });
            box.Add(_baseNameField);

            // Suffix Format
            _suffixFormatField = new TextField("Format") { value = _suffixFormat };
            _suffixFormatField.RegisterValueChangedCallback(e =>
            {
                _suffixFormat = e.newValue;
                EditorPrefs.SetString(PREF_KEY_SUFFIX_FORMAT, _suffixFormat);
                RefreshListUI();
            });
            box.Add(_suffixFormatField);

            var suffixHelp = new HelpBox("Format: _{0:00} (e.g. _01), _{0} (e.g. _1)", HelpBoxMessageType.None);
            box.Add(suffixHelp);

            // Start Index
            _startIndexField = new IntegerField("Start") { value = _startIndex };
            _startIndexField.RegisterValueChangedCallback(e =>
            {
                _startIndex = e.newValue;
                EditorPrefs.SetInt(PREF_KEY_START_INDEX, _startIndex);
                RefreshListUI();
            });
            box.Add(_startIndexField);

            // Include Subfolders Toggle
            _subfoldersToggle = new Toggle("Subfolders") { value = _includeSubfolders };
            _subfoldersToggle.RegisterValueChangedCallback(e =>
            {
                _includeSubfolders = e.newValue;
                EditorPrefs.SetBool(PREF_KEY_INCLUDE_SUBFOLDERS, _includeSubfolders);
            });
            box.Add(_subfoldersToggle);

            // Filter Extension
            _filterExtensionField = new TextField("Filter") { value = _filterByExtension };
            _filterExtensionField.RegisterValueChangedCallback(e =>
            {
                _filterByExtension = e.newValue;
                EditorPrefs.SetString(PREF_KEY_FILTER_BY_EXTENSION, _filterByExtension);
            });
            box.Add(_filterExtensionField);

            var filterHelp = new HelpBox(".png, .prefab, .asset (optional)", HelpBoxMessageType.None);
            box.Add(filterHelp);

            // Buttons Row
            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10 } };
            
            var loadBtn = new Button(() =>
            {
                Debug.Log("[AssetNameModifier] Loading assets from folder...");
                LoadAssetsFromFolder();
                RefreshListUI();
            })
            {
                text = "Load Selected",
                style = { flexGrow = 1, height = 30, unityFontStyleAndWeight = FontStyle.Bold }
            };
            buttonRow.Add(loadBtn);

            var clearListBtn = new Button(() =>
            {
                Debug.Log("[AssetNameModifier] Clearing loaded asset list.");
                _assetsInFolder.Clear();
                RefreshListUI();
            })
            {
                text = "Clear",
                style = { flexGrow = 1, height = 30, unityFontStyleAndWeight = FontStyle.Bold }
            };
            buttonRow.Add(clearListBtn);
            
            box.Add(buttonRow);

            // Dynamic list header
            var listHeader = new Label("Assets") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10 } };
            box.Add(listHeader);

            // Scroll View for dynamic asset list
            var scroll = new ScrollView { style = { maxHeight = 250, minHeight = 100, marginTop = 5 } };
            _listContainer = new VisualElement();
            scroll.Add(_listContainer);
            box.Add(scroll);

            // Apply Button
            _applyBtn = new Button(() =>
            {
                Debug.Log("[AssetNameModifier] Applying batch rename...");
                PerformBatchRenameWithUndo();
                RefreshListUI();
            })
            {
                text = "Rename",
                style = { height = 40, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10 }
            };
            box.Add(_applyBtn);

            RefreshListUI();

            return box;
        }

        private VisualElement BuildUtilitySection()
        {
            var box = UITKEditorHelper.BuildBox("Utility");
            var resetBtn = new Button(() =>
            {
                Undo.RecordObject(this, "Reset to Defaults");
                ResetToDefaults();
            })
            {
                text = "Reset Configs to Defaults",
                style = { height = 24 }
            };
            box.Add(resetBtn);
            return box;
        }

        private void ResetToDefaults()
        {
            _assetA = null;
            _assetB = null;
            _targetFolder = null;
            _assetsInFolder.Clear();
            _baseNamePrefix = "Asset";
            _suffixFormat = "_{0:00}";
            _startIndex = 1;
            _includeSubfolders = false;
            _filterByExtension = "";

            // Clear EditorPrefs
            EditorPrefs.DeleteKey(PREF_KEY_ASSET_A_PATH);
            EditorPrefs.DeleteKey(PREF_KEY_ASSET_B_PATH);
            EditorPrefs.DeleteKey(PREF_KEY_TARGET_FOLDER_PATH);
            EditorPrefs.DeleteKey(PREF_KEY_BASE_NAME_PREFIX);
            EditorPrefs.DeleteKey(PREF_KEY_SUFFIX_FORMAT);
            EditorPrefs.DeleteKey(PREF_KEY_START_INDEX);
            EditorPrefs.DeleteKey(PREF_KEY_INCLUDE_SUBFOLDERS);
            EditorPrefs.DeleteKey(PREF_KEY_FILTER_BY_EXTENSION);

            // Update UI elements
            if (_assetAField != null) _assetAField.value = null;
            if (_assetBField != null) _assetBField.value = null;
            if (_folderField != null) _folderField.value = null;
            if (_baseNameField != null) _baseNameField.value = _baseNamePrefix;
            if (_suffixFormatField != null) _suffixFormatField.value = _suffixFormat;
            if (_startIndexField != null) _startIndexField.value = _startIndex;
            if (_subfoldersToggle != null) _subfoldersToggle.value = _includeSubfolders;
            if (_filterExtensionField != null) _filterExtensionField.value = _filterByExtension;

            RefreshListUI();
            Debug.Log("[AssetNameModifier] Reset configurations to defaults.");
        }

        private void RefreshListUI()
        {
            if (_listContainer == null) return;
            
            _listContainer.Clear();

            if (_assetsInFolder.Count == 0)
            {
                var noAssetsLabel = new Label("No assets.")
                {
                    style = { unityFontStyleAndWeight = FontStyle.Italic, color = Color.gray, marginTop = 10, marginBottom = 10, unityTextAlign = TextAnchor.MiddleCenter }
                };
                _listContainer.Add(noAssetsLabel);
                
                if (_applyBtn != null) _applyBtn.style.display = DisplayStyle.None;
                return;
            }

            if (_applyBtn != null) _applyBtn.style.display = DisplayStyle.Flex;

            for (int i = 0; i < _assetsInFolder.Count; i++)
            {
                int index = i;
                var obj = _assetsInFolder[i];
                if (obj == null) continue;

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2, alignItems = Align.Center } };

                string currentName = obj.name;
                string extension = Path.GetExtension(AssetDatabase.GetAssetPath(obj));
                string newName = "Invalid Format";
                try
                {
                    newName = _baseNamePrefix + string.Format(_suffixFormat, _startIndex + index);
                }
                catch (System.Exception)
                {
                    // Safe formatting fallback
                }

                var idxLabel = new Label($"[{index}]") { style = { width = 30 } };
                row.Add(idxLabel);

                var oldNameLabel = new Label(currentName) { style = { width = 120, unityTextOverflowPosition = TextOverflowPosition.End } };
                row.Add(oldNameLabel);

                var arrowLabel = new Label("→") { style = { width = 20, unityTextAlign = TextAnchor.MiddleCenter } };
                row.Add(arrowLabel);

                var newNameLabel = new Label(newName + extension) { style = { flexGrow = 1, unityFontStyleAndWeight = FontStyle.Bold } };
                row.Add(newNameLabel);

                var removeBtn = new Button(() => 
                {
                    Debug.Log($"[AssetNameModifier] Removing asset at index {index} from list.");
                    RemoveAssetFromListWithUndo(index);
                    RefreshListUI();
                }) 
                { 
                    text = "✕", 
                    style = { width = 25 } 
                };
                row.Add(removeBtn);

                _listContainer.Add(row);
            }
        }
        #endregion

        #region Private Methods - Swap Functionality
        private void PerformSwapWithUndo()
        {
            if (_assetA == null || _assetB == null)
            {
                Debug.LogError("[AssetNameModifier] Asset A or Asset B is null!");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Swap Asset Names");
            int undoGroup = Undo.GetCurrentGroup();

            Debug.Log($"[AssetNameModifier] Attempting to swap asset names: {_assetA.name} ↔ {_assetB.name}");
            SwapAssetNames(_assetA, _assetB);

            Undo.CollapseUndoOperations(undoGroup);
        }

        private void SwapAssetNames(Object a, Object b)
        {
            if (a == null || b == null)
            {
                Debug.LogError("[AssetNameModifier] Assets are null in SwapAssetNames!");
                return;
            }

            string pathA = AssetDatabase.GetAssetPath(a);
            string pathB = AssetDatabase.GetAssetPath(b);

            if (string.IsNullOrEmpty(pathA) || string.IsNullOrEmpty(pathB))
            {
                Debug.LogError($"[AssetNameModifier] Invalid paths. Path A: {pathA}, Path B: {pathB}");
                return;
            }

            if (pathA == pathB)
            {
                Debug.LogError($"[AssetNameModifier] Selected identical asset twice: {pathA}");
                return;
            }

            string nameA = Path.GetFileNameWithoutExtension(pathA);
            string nameB = Path.GetFileNameWithoutExtension(pathB);
            string dirA = Path.GetDirectoryName(pathA);
            string extA = Path.GetExtension(pathA);

            string tempName = nameA + "_temp_swap_" + System.Guid.NewGuid().ToString().Substring(0, 8);
            string tempPath = Path.Combine(dirA, tempName + extA);

            try
            {
                Debug.Log($"[AssetNameModifier] Step 1: Rename '{nameA}' to temp '{tempName}'");
                var err1 = AssetDatabase.RenameAsset(pathA, tempName);
                if (!string.IsNullOrEmpty(err1)) throw new System.Exception(err1);

                Debug.Log($"[AssetNameModifier] Step 2: Rename '{nameB}' to '{nameA}'");
                var err2 = AssetDatabase.RenameAsset(pathB, nameA);
                if (!string.IsNullOrEmpty(err2)) throw new System.Exception(err2);

                Debug.Log($"[AssetNameModifier] Step 3: Rename temp '{tempName}' to '{nameB}'");
                var err3 = AssetDatabase.RenameAsset(tempPath, nameB);
                if (!string.IsNullOrEmpty(err3)) throw new System.Exception(err3);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Swapped: {nameA} <-> {nameB}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AssetNameModifier] Failed: {ex.Message}");
            }
        }

        private void AssignSelectedAssets()
        {
            Object[] selection = Selection.GetFiltered<Object>(SelectionMode.Assets);

            if (selection.Length != 2)
            {
                Debug.LogWarning("[AssetNameModifier] Select 2 assets.");
                return;
            }

            string pathA = AssetDatabase.GetAssetPath(selection[0]);
            string pathB = AssetDatabase.GetAssetPath(selection[1]);

            if (string.IsNullOrEmpty(pathA) || string.IsNullOrEmpty(pathB) || pathA == pathB)
            {
                Debug.LogError("[AssetNameModifier] Invalid selection path.");
                return;
            }

            _assetA = selection[0];
            _assetB = selection[1];
            Debug.Log($"[AssetNameModifier] Assigned Asset A: {_assetA.name}, Asset B: {_assetB.name}");
        }
        #endregion

        #region Private Methods - Batch Rename Functionality
        private void LoadAssetsFromFolder()
        {
            if (_targetFolder == null)
            {
                Debug.LogError("[AssetNameModifier] No target folder.");
                return;
            }

            string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
            
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogError($"[AssetNameModifier] Selected object is not a folder: {folderPath}");
                return;
            }

            _assetsInFolder.Clear();
            
            var selectedObjects = Selection.objects;
            Debug.Log($"[AssetNameModifier] Scanning {selectedObjects.Length} selected objects for folder path: {folderPath}");
            foreach (var obj in selectedObjects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning($"[AssetNameModifier] Can't find asset path for: {obj.name}");
                    continue; 
                }

                if (!_assetsInFolder.Contains(obj))
                {
                    _assetsInFolder.Add(obj);
                }
            }

            Debug.Log($"[AssetNameModifier] Loaded {_assetsInFolder.Count} assets.");
        }

        private void PerformBatchRenameWithUndo()
        {
            if (_assetsInFolder.Count == 0)
            {
                Debug.LogError("[AssetNameModifier] No assets to rename!");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Warning", "Rename assets?", "Rename", "Cancel");
            if (!confirm) return;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Batch Rename Assets");
            int undoGroup = Undo.GetCurrentGroup();

            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < _assetsInFolder.Count; i++)
            {
                if (_assetsInFolder[i] == null) continue;

                string assetPath = AssetDatabase.GetAssetPath(_assetsInFolder[i]);
                string extension = Path.GetExtension(assetPath);
                string newName = "Invalid Format";
                try
                {
                    newName = _baseNamePrefix + string.Format(_suffixFormat, _startIndex + i);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[AssetNameModifier] Suffix formatting error: {ex.Message}");
                    failCount++;
                    continue;
                }

                try
                {
                    string error = AssetDatabase.RenameAsset(assetPath, newName);
                    if (string.IsNullOrEmpty(error))
                    {
                        successCount++;
                    }
                    else
                    {
                        Debug.LogError($"[AssetNameModifier] Failed to rename {_assetsInFolder[i].name}: {error}");
                        failCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[AssetNameModifier] Exception renaming {_assetsInFolder[i].name}: {ex.Message}");
                    failCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"Done: {successCount}");

            _assetsInFolder.Clear();
        }

        private void RemoveAssetFromListWithUndo(int index)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Remove Asset from List");
            int undoGroup = Undo.GetCurrentGroup();

            Undo.RecordObject(this, "Remove Asset");
            _assetsInFolder.RemoveAt(index);

            Undo.CollapseUndoOperations(undoGroup);
        }
        #endregion
    }
}
#endif