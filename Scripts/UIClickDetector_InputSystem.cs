/*
Author: NamPhuThuy
Github: https://github.com/NamPhuThuy
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NamPhuThuy.Common
{
    public class UIClickDetector_InputSystem : MonoBehaviour
    {
        #region Private Serializable Fields

        //[Header("Flags")]

        //[Header("Stats")]

        #endregion

        #region Private Fields

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            
        }

        private void Update()
        {
            
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion

        #region Editor Methods

        public void ResetValues()
        {
            
        }

        #endregion
    }

    /*#if UNITY_EDITOR
    [CustomEditor(typeof(UIClickDetector_InputSystem))]
    [CanEditMultipleObjects]
    public class UIClickDetector_InputSystemEditor : Editor
    {
        private UIClickDetector_InputSystem script;
        private Texture2D frogIcon;
        
        private void OnEnable()
        {
            frogIcon = Resources.Load<Texture2D>("frog"); // no extension needed
            script = (UIClickDetector_InputSystem)target;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
           

            ButtonResetValues();
        }

        private void ButtonResetValues()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Reset Values", frogIcon), GUILayout.Width(InspectorConst.BUTTON_WIDTH_MEDIUM)))
            {
                script.ResetValues();
                EditorUtility.SetDirty(script); // Mark the object as dirty
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
    #endif*/
}
