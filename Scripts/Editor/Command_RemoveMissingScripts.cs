using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace NamPhuThuy.Common
{
#if UNITY_EDITOR
    public class Command_RemoveMissingScripts : MonoBehaviour
    {
        [MenuItem("NamPhuThuy/Common/Command - Remove Missing Scripts")]
        public static void Remove()
        {
            var objs = Resources.FindObjectsOfTypeAll<GameObject>();
            int count = objs.Sum(GameObjectUtility.RemoveMonoBehavioursWithMissingScript);
            DebugLogger.Log(message:$"Removed {count} missing scripts");
        }
    }
#endif

}