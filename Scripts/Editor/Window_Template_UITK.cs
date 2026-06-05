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
// 7. CACHING: Provide a 'Reset to Defaults' button in the options panel to clear/override cached or persisted EditorPrefs values that might become stale or invalid.
// ───────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace NamPhuThuy.Common
{
#if UNITY_EDITOR
    public class Window_Template_UITK : EditorWindow
    {
        #region Private Fields
        // Example data field to demonstrate undo recording and EditorPrefs
        [SerializeField] private string _exampleText = "Editable value";
        
        // EditorPrefs Key
        private const string PREF_KEY_EXAMPLE_TEXT_UITK = "NamPhuThuy_TemplateUITK_ExampleText";

        // UI References
        private TextField _exampleTextField;
        private VisualElement _listContainer;
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Template")]
        public static void ShowWindow()
        {
            var window = GetWindow<Window_Template_UITK>("Window Template");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            Debug.Log("[UITK Template] OnEnable");
            // Load persisted data when the window opens or after script reload
            _exampleText = EditorPrefs.GetString(PREF_KEY_EXAMPLE_TEXT_UITK, "Default Editable Value");
        }

        private void OnDisable()
        {
            Debug.Log("[UITK Template] OnDisable");
            // Save data when window closes to persist across Unity sessions
            EditorPrefs.SetString(PREF_KEY_EXAMPLE_TEXT_UITK, _exampleText);
        }

        // CreateGUI is the UITK equivalent of OnGUI, called once when window is opened
        public void CreateGUI()
        {
            Debug.Log("[UITK Template] CreateGUI");
            var root = rootVisualElement;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;
            root.style.paddingTop = 20;
            root.style.paddingBottom = 20;

            // ── Header Section ──
            var header = new Label("Window Template (UITK)")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16, unityTextAlign = TextAnchor.MiddleCenter, marginBottom = 10 }
            };
            root.Add(header);

            var helpBox = new HelpBox(
                "Description of what this window does.\n" +
                "This template demonstrates UI Toolkit (UITK) best practices including VisualElements, styling, UI bindings, and the Undo system.",
                HelpBoxMessageType.Info);
            root.Add(helpBox);

            var mainScroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1, marginTop = 10 } };
            root.Add(mainScroll);

            // ── Content Sections ──
            mainScroll.Add(BuildDataSection());
            mainScroll.Add(BuildListSection());

            // ── Footer Buttons ──
            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10 } };
            
            var action1Btn = new Button(PerformExampleUndoableAction) 
            { 
                text = "Action 1 (Undo Example)", 
                style = { flexGrow = 1, height = 30, unityFontStyleAndWeight = FontStyle.Bold } 
            };
            buttonRow.Add(action1Btn);

            var action2Btn = new Button(() => Debug.Log("Action 2 Clicked")) 
            { 
                text = "Action 2", 
                style = { flexGrow = 1, height = 30, unityFontStyleAndWeight = FontStyle.Bold } 
            };
            buttonRow.Add(action2Btn);

            root.Add(buttonRow);
        }
        #endregion

        #region UI Builders

        private VisualElement BuildDataSection()
        {
            var box = UITKEditorHelper.BuildBox();

            var title = new Label("Data Section") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } };
            box.Add(title);

            box.Add(new Label("Example value stored on this window:"));

            // UITK TextFields handle their own callbacks
            _exampleTextField = new TextField("Example Text") { value = _exampleText };
            _exampleTextField.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(this, "Change Example Text");
                _exampleText = e.newValue;
            });
            box.Add(_exampleTextField);

            return box;
        }

        private VisualElement BuildListSection()
        {
            var box = UITKEditorHelper.BuildBox();

            var title = new Label("Dynamic List Section") { style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleCenter, marginBottom = 5 } };
            box.Add(title);

            var scroll = new ScrollView { style = { maxHeight = 250, minHeight = 100 } };
            _listContainer = new VisualElement();
            scroll.Add(_listContainer);
            box.Add(scroll);

            RefreshListUI();

            return box;
        }

        private void RefreshListUI()
        {
            _listContainer.Clear();

            for (int i = 0; i < 20; i++)
            {
                int index = i; // local copy for closures
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2, alignItems = Align.Center } };

                row.Add(new Label($"Item [{index}]") { style = { width = 60 } });

                var field = new TextField { value = $"Value {index}", style = { flexGrow = 1 } };
                row.Add(field);

                var removeBtn = new Button(() => { Debug.Log($"Remove item {index}"); }) { text = "✕", style = { width = 25 } };
                row.Add(removeBtn);

                _listContainer.Add(row);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Template: how to wrap an operation in a single Unity Re/Undo group.
        /// </summary>
        private void PerformExampleUndoableAction()
        {
            Debug.Log("[UITK Template] Action Start");

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Template - Example Undoable Action");
            int undoGroup = Undo.GetCurrentGroup();

            Undo.RecordObject(this, "Change Example Text");
            
            _exampleText = "Changed by Action 1";
            
            // Because UITK fields don't auto-update when the serialized property changes behind the scenes
            // unless heavily bound, we update the UI explicitly here.
            if (_exampleTextField != null)
            {
                _exampleTextField.SetValueWithoutNotify(_exampleText);
            }
            else
            {
                Debug.LogError("[UITK Template] Null Text Field!");
            }

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[UITK Template] Action Done");
        }
        #endregion
    }
#endif
}
