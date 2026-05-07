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
        
        private GUIStyle _paddedStyle;
        private GUIStyle _centeredButtonStyle;
        private GUIStyle _centeredLabelStyle;

        // Example data field to demonstrate undo recording and EditorPrefs
        [SerializeField] private string _exampleText = "Editable value";
        
        // EditorPrefs Key
        private const string PREF_KEY_EXAMPLE_TEXT = "NamPhuThuy_Template_ExampleText";
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Template (IMGUI)")]
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
            // Load persisted data when the window opens or after script reload
            _exampleText = EditorPrefs.GetString(PREF_KEY_EXAMPLE_TEXT, "Default Editable Value");
        }

        private void OnDisable()
        {
            // Save data when window closes to persist across Unity sessions
            EditorPrefs.SetString(PREF_KEY_EXAMPLE_TEXT, _exampleText);
        }

        private void OnGUI()
        {
            InitializeStyles();

            // Better layout approach: Use a padded GUIStyle instead of GUILayout.BeginArea
            EditorGUILayout.BeginVertical(_paddedStyle);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            GUILayout.Space(10);
            DrawContent();
            GUILayout.Space(10);
            DrawButtons();

            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Initialization
        private void InitializeStyles()
        {
            if (_paddedStyle == null)
            {
                _paddedStyle = new GUIStyle();
                _paddedStyle.padding = new RectOffset(20, 20, 20, 20);
            }

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
        #endregion

        #region Drawing
        private void DrawHeader()
        {
            GUILayout.Label("Window Template (IMGUI)", _centeredLabelStyle);
            EditorGUILayout.HelpBox(
                "Description of what this window does.\n" +
                "This template demonstrates IMGUI best practices including GUI.skin.box, EditorPrefs, change checks, and the Undo system.",
                MessageType.Info);
        }

        private void DrawContent()
        {
            // Use GUI.skin.box for standard visual grouping of sections
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Data Section", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("Example value stored on this window:");
            
            // Example of using BeginChangeCheck to properly record Undo for text fields
            EditorGUI.BeginChangeCheck();
            string newText = EditorGUILayout.TextField("Example Text", _exampleText);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Change Example Text");
                _exampleText = newText;
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(12);

            // Section 1
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Section 1 (List)", _centeredLabelStyle);
            
            _scrollPosSection1 = EditorGUILayout.BeginScrollView(_scrollPosSection1, GUILayout.Height(200));
            for (int i = 0; i < 20; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Item [{i}]", GUILayout.Width(60));
                EditorGUILayout.TextField($"Value {i}");
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    // Call method that uses Undo for remove
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            GUILayout.Space(16);

            // Section 2
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Section 2 (Object Fields)", _centeredLabelStyle);
            
            _scrollPosSection2 = EditorGUILayout.BeginScrollView(_scrollPosSection2, GUILayout.Height(200));
            for (int i = 0; i < 15; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Item [{i}]", GUILayout.Width(60));
                EditorGUILayout.ObjectField(null, typeof(GameObject), false);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    // Call method that uses Undo for remove
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
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
            // 1. Start a group and give it a name (what shows in Edit > Undo)
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Template - Example Undoable Action");
            int undoGroup = Undo.GetCurrentGroup();

            // 2. Record objects BEFORE you modify them
            Undo.RecordObject(this, "Change Example Text");

            // 3. Perform your changes
            _exampleText = "Changed by Action 1";

            // 4. Collapse to a single undo step
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
