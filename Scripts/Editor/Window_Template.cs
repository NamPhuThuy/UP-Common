using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NamPhuThuy.Common
{
#if UNITY_EDITOR
    public class Window_Template : EditorWindow
    {
        #region Private Fields
        private Vector2 _scrollPos;
        private Vector2 _scrollPosSection1;
        private Vector2 _scrollPosSection2;
        private GUIStyle _centeredButtonStyle;
        private GUIStyle _centeredLabelStyle;

        // Example data field to demonstrate undo recording
        [SerializeField] private string _exampleText = "Editable value";
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Template")]
        public static void ShowWindow()
        {
            Window_Template window = GetWindow<Window_Template>("Window Template");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            // Initialize data when window opens
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
            
            // Main scroll view that wraps everything
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
            GUILayout.Label("Window Template", _centeredLabelStyle);
            EditorGUILayout.HelpBox(
                "Description of what this window does.\n" +
                "This template also shows how to use the Unity Re/Undo system from an editor window.",
                MessageType.Info);
        }

        private void DrawContent()
        {
            // Simple example field whose changes can be recorded with Undo
            EditorGUILayout.LabelField("Example value stored on this window:");
            _exampleText = EditorGUILayout.TextField("Example Text", _exampleText);

            GUILayout.Space(12);

            // Section 1
            GUILayout.Label("Section 1", _centeredLabelStyle);
            _scrollPosSection1 = EditorGUILayout.BeginScrollView(_scrollPosSection1, GUILayout.Height(250));
            
            for (int i = 0; i < 20; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Item [{i}]", GUILayout.Width(60));
                EditorGUILayout.TextField($"Value {i}");
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    // Example: here you would call a method that uses Undo for remove
                    // ExampleRemoveWithUndo(targetObject);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();

            GUILayout.Space(16);

            // Section 2
            GUILayout.Label("Section 2", _centeredLabelStyle);
            _scrollPosSection2 = EditorGUILayout.BeginScrollView(_scrollPosSection2, GUILayout.Height(250));
            
            for (int i = 0; i < 15; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Item [{i}]", GUILayout.Width(60));
                EditorGUILayout.ObjectField(null, typeof(GameObject), false);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    // Example: here you would call a method that uses Undo for remove
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawButtons()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Action 1 (Undo Example)", _centeredButtonStyle, GUILayout.Height(30)))
            {
                PerformExampleUndoableAction();
            }

            if (GUILayout.Button("Action 2", _centeredButtonStyle, GUILayout.Height(30)))
            {
                // Button action
            }

            GUILayout.EndHorizontal();
        }
        
        #endregion
        
        
        #region Private Methods
      

        /// <summary>
        /// Template: how to wrap an operation in a single Unity Re/Undo group.
        /// Copy this pattern for your own editor operations.
        /// </summary>
        private void PerformExampleUndoableAction()
        {
            // 1\. Start a group and give it a name (what shows in Edit \> Undo)
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Template - Example Undoable Action");
            int undoGroup = Undo.GetCurrentGroup();

            // 2\. Record objects BEFORE you modify them
            // In this simple example we just record the window itself
            Undo.RecordObject(this, "Change Example Text");

            // 3\. Perform your changes
            _exampleText = "Changed by Action 1";

            // 4\. Collapse to a single undo step
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("Performed example undoable action from WindowTemplate.");
        }

        /// <summary>
        /// Template: how to delete an object using the Undo system.
        /// Call this instead of `Object.DestroyImmediate`.
        /// </summary>
        private static void DestroyObjectWithUndo(Object target, string groupName = "Delete Object")
        {
            if (target == null) return;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(groupName);
            int undoGroup = Undo.GetCurrentGroup();

            Undo.DestroyObjectImmediate(target);

            Undo.CollapseUndoOperations(undoGroup);
        }
        #endregion
    }
#endif
}
