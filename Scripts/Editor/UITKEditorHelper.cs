// ───────────────────────────────────────────────────────────────────────
// RULES:
// 1. PROCESS: Use Debug.Log for trace steps.
// 2. SAFETY: Use Debug.LogError in null/boundary checks.
// 3. ENUM FORMAT: If used enum, use the format:
//    public enum Type
//    {
//        NONE = 0, TYPE_1 = 1, TYPE_2 = 2
//    }
// ───────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace NamPhuThuy.Common
{
#if UNITY_EDITOR
    /// <summary>
    /// Centralized helper methods for UI Toolkit (UITK) Editor Windows
    /// ensuring visual consistency and reusability across the TN028 project.
    /// </summary>
    public static class UITKEditorHelper
    {
        // Centralized Clickable Color Palette
        private static readonly Color COLOR_OCEAN_BLUE = new Color(0.0f, 0.47f, 0.74f, 1f);
        private static readonly Color COLOR_DANGER_BG = new Color(0.55f, 0.15f, 0.15f, 1f); // Red for destructive actions

        /// <summary>
        /// Creates a highly premium, visually consistent box for grouping fields in UITK Editor Windows.
        /// </summary>
        /// <param name="titleText">Optional title text to show inside the box.</param>
        /// <returns>A styled VisualElement box.</returns>
        public static VisualElement BuildBox(string titleText = null)
        {
            var box = new VisualElement();
            
            // Consistent Premium Styling (Dark borders, sleek translucent background, rounded corners)
            box.style.borderTopWidth = 1; 
            box.style.borderBottomWidth = 1; 
            box.style.borderLeftWidth = 1; 
            box.style.borderRightWidth = 1;
            
            var borderColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            box.style.borderTopColor = borderColor; 
            box.style.borderBottomColor = borderColor;
            box.style.borderLeftColor = borderColor; 
            box.style.borderRightColor = borderColor;
            
            box.style.borderTopLeftRadius = 5; 
            box.style.borderTopRightRadius = 5;
            box.style.borderBottomLeftRadius = 5; 
            box.style.borderBottomRightRadius = 5;
            
            box.style.paddingLeft = 12; 
            box.style.paddingRight = 12; 
            box.style.paddingTop = 10; 
            box.style.paddingBottom = 10;
            
            box.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
            box.style.marginBottom = 12;

            if (!string.IsNullOrEmpty(titleText))
            {
                var title = new Label(titleText) 
                { 
                    style = 
                    { 
                        unityFontStyleAndWeight = FontStyle.Bold, 
                        fontSize = 13, 
                        marginBottom = 8, 
                        color = new Color(0.85f, 0.85f, 0.85f) 
                    } 
                };
                box.Add(title);
            }

            return box;
        }

        /// <summary>
        /// Builds a highly uniform asset collection section containing a list property field
        /// and helper buttons to add selection, search all in project, or clear.
        /// </summary>
        /// <typeparam name="T">The type of ScriptableObject/Asset to manage.</typeparam>
        public static VisualElement BuildAssetListSection<T>(
            SerializedObject serializedObject,
            string listPropName,
            string sectionTitle,
            string listLabel,
            List<T> listToModify,
            Action onListModified,
            Action<VisualElement> extraButtonsBuilder = null,
            bool showLoadAllButton = true) where T : UnityEngine.Object
        {
            var box = BuildBox(sectionTitle);

            // Property Field for UI bindings
            var listProp = serializedObject.FindProperty(listPropName);
            if (listProp == null)
            {
                Debug.LogError($"[UITKEditorHelper] Property '{listPropName}' not found in target serialized object.");
                return box;
            }

            var propertyField = new PropertyField(listProp, listLabel);
            propertyField.Bind(serializedObject);
            box.Add(propertyField);

            // Helpers Row
            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8, justifyContent = Justify.SpaceBetween } };

            // 1. Add Selected
            var btnAddSelected = new Button(() =>
            {
                var selectedObjects = Selection.objects;
                if (selectedObjects == null || selectedObjects.Length == 0)
                {
                    EditorUtility.DisplayDialog("Warning", "No selection.", "OK");
                    return;
                }

                int addedCount = 0;
                foreach (var obj in selectedObjects)
                {
                    if (obj is T asset)
                    {
                        if (!listToModify.Contains(asset))
                        {
                            listToModify.Add(asset);
                            addedCount++;
                        }
                    }
                }

                serializedObject.Update();
                onListModified?.Invoke();
                Debug.Log($"[UITKEditorHelper] Added: {addedCount} {typeof(T).Name}");
            }) 
            { 
                text = "Add Selected", 
                style = 
                { 
                    flexGrow = 1, 
                    marginRight = 4, 
                    height = 24, 
                    fontSize = 11,
                    backgroundColor = COLOR_OCEAN_BLUE,
                    color = Color.white
                } 
            };
            buttonRow.Add(btnAddSelected);

            // 2. Load All
            if (showLoadAllButton)
            {
                var btnFindAll = new Button(() =>
                {
                    bool confirm = EditorUtility.DisplayDialog(
                        "Warning",
                        "Load all assets?",
                        "Load All",
                        "Cancel"
                    );
                    if (!confirm) return;

                    string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
                    if (guids == null || guids.Length == 0)
                    {
                        EditorUtility.DisplayDialog("Result", "0 assets found.", "OK");
                        return;
                    }

                    int addedCount = 0;
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                        if (asset != null && !listToModify.Contains(asset))
                        {
                            listToModify.Add(asset);
                            addedCount++;
                        }
                    }

                    serializedObject.Update();
                    onListModified?.Invoke();
                    Debug.Log($"[UITKEditorHelper] Loaded: {addedCount} {typeof(T).Name}");
                }) 
                { 
                    text = "Load All", 
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
                buttonRow.Add(btnFindAll);
            }

            // 3. Clear List
            var btnClearList = new Button(() => 
            {
                listToModify.Clear();
                serializedObject.Update();
                onListModified?.Invoke();
                Debug.Log($"[UITKEditorHelper] Cleared: {typeof(T).Name}");
            }) 
            { 
                text = "Clear", 
                style = 
                { 
                    flexGrow = 1, 
                    marginLeft = 4, 
                    height = 24, 
                    fontSize = 11,
                    backgroundColor = COLOR_DANGER_BG, // Styled with Red Danger background
                    color = Color.white
                } 
            };
            buttonRow.Add(btnClearList);

            // 4. Extra Buttons
            extraButtonsBuilder?.Invoke(buttonRow);

            box.Add(buttonRow);
            return box;
        }
    }
#endif
}