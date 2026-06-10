using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if UNITY_EDITOR
namespace NamPhuThuy.Common
{
    public class Command_SceneSwitcher : Editor
    {
        private const string SWITCH_SCENE_MENU_NAME = "NamPhuThuy/Common/Command - Scene Switcher";
        private const string ALT = "&";

        #region LoadSceneShortCut

        [MenuItem(SWITCH_SCENE_MENU_NAME + "/Scene 0 " + ALT + "1")]
        static void LoadScene0()
        {
            LoadSceneByIndex(0);
        }

        [MenuItem(SWITCH_SCENE_MENU_NAME + "/Scene 1 " + ALT + "2")]
        static void LoadScene1()
        {
            LoadSceneByIndex(1);
        }

        [MenuItem(SWITCH_SCENE_MENU_NAME + "/Scene 2 " + ALT + "3")]
        static void LoadScene2()
        {
            LoadSceneByIndex(2);
        }

        [MenuItem(SWITCH_SCENE_MENU_NAME + "/Scene 3 " + ALT + "4")]
        static void LoadScene3()
        {
            LoadSceneByIndex(3);
        }
        
        [MenuItem(SWITCH_SCENE_MENU_NAME + "/Scene 4 " + ALT + "5")]
        static void LoadScene4()
        {
            LoadSceneByIndex(4);
        }
        
        [MenuItem(SWITCH_SCENE_MENU_NAME + "/Scene 5 " + ALT + "6")]
        static void LoadScene5()
        {
            LoadSceneByIndex(5);
        }
        
        [MenuItem(SWITCH_SCENE_MENU_NAME + "/Scene 6 " + ALT + "7")]
        static void LoadScene6()
        {
            LoadSceneByIndex(6);
        }

        [MenuItem(SWITCH_SCENE_MENU_NAME + "/Scene 7 " + ALT + "8")]
        static void LoadScene7()
        {
            LoadSceneByIndex(7);
        }
        
        [MenuItem(SWITCH_SCENE_MENU_NAME + "/Scene 8 " + ALT + "9")]
        static void LoadScene8()
        {
            LoadSceneByIndex(8);
        }
        
        [MenuItem(SWITCH_SCENE_MENU_NAME + "/Scene 9 " + ALT + "0")]
        static void LoadScene9()
        {
            LoadSceneByIndex(9);
        }
        
        static void LoadSceneByIndex(int buildIndex)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            // Get path for the scene by build index
            string scenePath = GetScenePathByIndex(buildIndex);

            if (!string.IsNullOrEmpty(scenePath))
            {
                EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                Debug.LogError($"No scene found at build index {buildIndex}. Please check your build settings.");
            }
        }

        private static string GetScenePathByIndex(int buildIndex)
        {
            // Validate index
            if (buildIndex < 0 || buildIndex >= EditorBuildSettings.scenes.Length)
            {
                Common.DebugLogger.LogError($"Build index {buildIndex} is out of range.", Color.black);
                return null;
            }

            // Get scene path from build settings
            var scene = EditorBuildSettings.scenes[buildIndex];
            return scene.enabled ? scene.path : null;
        }

        #endregion
    }
}
#endif