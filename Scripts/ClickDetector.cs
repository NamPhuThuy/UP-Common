#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

// Use backtracking to show the path of the clicked UIElement or GameObject
namespace NamPhuThuy.Common
{
    public class ClickDetector : MonoBehaviour
    {
        public enum DetectionMode
        {
            NONE = 0,
            UI_ELEMENT = 1,
            GAME_OBJECT = 2,
            BOTH = 3,
        }

        [SerializeField] private DetectionMode mode = DetectionMode.BOTH;

        public void Update()
        {
            if (mode == DetectionMode.NONE) return;
            if (!Input.GetMouseButtonDown(0)) return;

            switch (mode)
            {
                case DetectionMode.UI_ELEMENT:
                    LogClick(GetClickedUIElement(), "UI Element");
                    break;
                case DetectionMode.GAME_OBJECT:
                    LogClick(GetClickedGameObject(), "GameObject");
                    break;
                case DetectionMode.BOTH:
                    GameObject uiElement = GetClickedUIElement();
                    if (uiElement != null)
                    {
                        LogClick(uiElement, "UI Element");
                        return;
                    }
                    LogClick(GetClickedGameObject(), "GameObject");
                    break;
            }
        }

        private GameObject GetClickedUIElement()
        {
            if (EventSystem.current == null) return null;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count == 0) return null;
            return results[0].gameObject;
        }

        private GameObject GetClickedGameObject()
        {
            if (Camera.main == null)
            {
                DebugLogger.LogError("Main Camera null", context: this);
                return null;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Check 3D physics
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.collider.gameObject;
            }

            // Check 2D physics
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
            if (hit2D.collider != null)
            {
                return hit2D.collider.gameObject;
            }

            return null;
        }

        private void LogClick(GameObject clickedObject, string typeName)
        {
            if (clickedObject == null) return;
            string path = GetHierarchyPath(clickedObject.transform);
            DebugLogger.Log(message: $"[{typeName}] Path: {path}", context: clickedObject);
        }

        private string GetHierarchyPath(Transform transform)
        {
            if (transform.parent == null)
            {
                return transform.name;
            }

            return GetHierarchyPath(transform.parent) + $" -> {transform.name}";
        }
    }

    [CustomEditor(typeof(ClickDetector))]
    public class ClickDetectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.BeginHorizontal();
            GUILayout.Space(40); // Left margin
            // Display the description
            Color textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            GUILayout.Label("Enable this component if you want to Debug the 'location on the Hierarchy tree' of the game object you just clicked", new GUIStyle() { wordWrap = true, normal = { textColor = textColor } });
            GUILayout.Space(40); // Right margin
            GUILayout.EndHorizontal();
        }
    }
}
#endif
