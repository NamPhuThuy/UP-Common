using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace NamPhuThuy.Common
{
#if UNITY_EDITOR
    public class Window_ComponentsRemover : EditorWindow
    {
        #region Enums
        public enum RemovableComponentType
        {
            NONE = 0,
            MESH_COLLIDER          = 1,
            BOX_COLLIDER           = 2,
            SPHERE_COLLIDER        = 3,
            CAPSULE_COLLIDER       = 4,
            BOX_COLLIDER_2D        = 10,
            CIRCLE_COLLIDER_2D     = 11,
            POLYGON_COLLIDER_2D    = 12,
            EDGE_COLLIDER_2D       = 13,
            RIGIDBODY              = 20,
            RIGIDBODY_2D           = 21,
            MESH_RENDERER          = 30,
            SPRITE_RENDERER        = 31,
            TRAIL_RENDERER         = 32,
            LINE_RENDERER          = 33,
            CANVAS_RENDERER        = 34,
            AUDIO_SOURCE           = 40,
        }
        #endregion

        #region Private Fields
        [SerializeField] private List<GameObject> _targets = new List<GameObject>();
        [SerializeField] private bool _includeChildren = true;
        [SerializeField] private RemovableComponentType _selectedComponentType = RemovableComponentType.MESH_COLLIDER;

        private SerializedObject _serializedObject;
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Components Remover")]
        public static void ShowWindow()
        {
            var window = GetWindow<Window_ComponentsRemover>("Components Remover");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;
            root.style.paddingTop = 20;
            root.style.paddingBottom = 20;

            // Header Section
            var header = new Label("Components Remover")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16, unityTextAlign = TextAnchor.MiddleCenter, marginBottom = 10 }
            };
            root.Add(header);

            var helpBox = new HelpBox("Remove components from targets.", HelpBoxMessageType.Info);
            helpBox.style.marginBottom = 10;
            root.Add(helpBox);

            var mainScroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
            root.Add(mainScroll);

            // Options Section
            mainScroll.Add(BuildOptionsSection());

            // Targets Section
            mainScroll.Add(BuildTargetsSection());

            // Footer Section
            root.Add(BuildFooterSection());
        }
        #endregion

        #region UI Builders
        private VisualElement BuildOptionsSection()
        {
            var box = UITKEditorHelper.BuildBox("Options");

            var typeField = new EnumField("Type", _selectedComponentType);
            typeField.Init(_selectedComponentType);
            typeField.RegisterValueChangedCallback(evt =>
            {
                _selectedComponentType = (RemovableComponentType)evt.newValue;
            });
            box.Add(typeField);

            var childrenToggle = new Toggle("Children") { value = _includeChildren };
            childrenToggle.RegisterValueChangedCallback(evt =>
            {
                _includeChildren = evt.newValue;
            });
            box.Add(childrenToggle);

            return box;
        }

        private VisualElement BuildTargetsSection()
        {
            return UITKEditorHelper.BuildAssetListSection<GameObject>(
                _serializedObject,
                "_targets",
                "Targets",
                "GameObjects",
                _targets,
                () => {}
            );
        }

        private VisualElement BuildFooterSection()
        {
            var footer = new VisualElement { style = { marginTop = 10 } };

            var btnRemove = new Button(RemoveComponentsFromTargets)
            {
                text = "Remove",
                style = { height = 30, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 }
            };
            footer.Add(btnRemove);

            var btnSelectChildren = new Button(SelectChildrenWithSelectedComponent)
            {
                text = "Select Children",
                style = { height = 24, unityFontStyleAndWeight = FontStyle.Normal }
            };
            footer.Add(btnSelectChildren);

            return footer;
        }
        #endregion

        #region Logic
        private Type GetSelectedType()
        {
            switch (_selectedComponentType)
            {
                case RemovableComponentType.MESH_COLLIDER:            return typeof(MeshCollider);
                case RemovableComponentType.BOX_COLLIDER:             return typeof(BoxCollider);
                case RemovableComponentType.SPHERE_COLLIDER:          return typeof(SphereCollider);
                case RemovableComponentType.CAPSULE_COLLIDER:         return typeof(CapsuleCollider);
                case RemovableComponentType.BOX_COLLIDER_2D:          return typeof(BoxCollider2D);
                case RemovableComponentType.CIRCLE_COLLIDER_2D:       return typeof(CircleCollider2D);
                case RemovableComponentType.POLYGON_COLLIDER_2D:      return typeof(PolygonCollider2D);
                case RemovableComponentType.EDGE_COLLIDER_2D:         return typeof(EdgeCollider2D);
                case RemovableComponentType.RIGIDBODY:                return typeof(Rigidbody);
                case RemovableComponentType.RIGIDBODY_2D:             return typeof(Rigidbody2D);
                case RemovableComponentType.MESH_RENDERER:            return typeof(MeshRenderer);
                case RemovableComponentType.SPRITE_RENDERER:          return typeof(SpriteRenderer);
                case RemovableComponentType.AUDIO_SOURCE:             return typeof(AudioSource);
                default:
                    return null;
            }
        }

        private void RemoveComponentsFromTargets()
        {
            _serializedObject.Update();
            if (_targets.Count == 0)
            {
                Debug.LogError("[ComponentsRemover] No targets assigned.");
                return;
            }

            Type selectedType = GetSelectedType();
            if (selectedType == null)
            {
                Debug.LogError("[ComponentsRemover] Unsupported type.");
                return;
            }

            int removedCount = 0;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Components Remover");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject go in _targets)
            {
                if (go == null) continue;

                Component[] components = _includeChildren
                    ? go.GetComponentsInChildren(selectedType, true)
                    : go.GetComponents(selectedType);

                foreach (Component comp in components)
                {
                    if (comp == null) continue;
                    Undo.DestroyObjectImmediate(comp);
                    removedCount++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"Removed: {removedCount}");
        }

        private void SelectChildrenWithSelectedComponent()
        {
            _serializedObject.Update();
            Type selectedType = GetSelectedType();
            if (selectedType == null)
            {
                Debug.LogError("[ComponentsRemover] Unsupported type.");
                return;
            }

            List<GameObject> result = GetChildrenWithSelectedComponent(selectedType);

            if (result.Count == 0)
            {
                Debug.Log("Selected: 0");
                return;
            }

            Selection.objects = result.ToArray();
            Debug.Log($"Selected: {result.Count}");
        }

        private List<GameObject> GetChildrenWithSelectedComponent(Type componentType)
        {
            var collected = new List<GameObject>();
            var seen = new HashSet<GameObject>();

            foreach (GameObject root in _targets)
            {
                if (root == null) continue;

                Component[] comps = root.GetComponentsInChildren(componentType, true);
                foreach (Component comp in comps)
                {
                    if (comp == null) continue;

                    GameObject go = comp.gameObject;
                    if (go != null && !seen.Contains(go))
                    {
                        seen.Add(go);
                        collected.Add(go);
                    }
                }
            }

            return collected;
        }
        #endregion
    }
#endif
}
