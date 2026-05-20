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
    // ─────────────── 1. INTERACTIVE UITK WINDOW ───────────────
    public class Window_SplineEditor_UITK : EditorWindow
    {
        #region Private Fields
        private SplinePath _targetSpline;
        private SerializedObject _serializedObject;

        // UI Values for Transformations
        private Vector3 _offsetValue = Vector3.zero;
        private float _scaleValue = 1.1f;

        // UI Values for Spline Shape Generator (NEW!)
        private SplineShapeType _generatorShapeType = SplineShapeType.Circle;
        private int _generatorPointsCount = 20;
        private float _generatorShapeSize = 3.0f;
        private float _generatorFrequency = 2.0f; // wave cycles, spiral turns, star spikes
        private float _generatorHeightDrift = 0.0f; // helical height drift

        // Static references for Scene view callback sync
        public static Window_SplineEditor_UITK Instance { get; private set; }
        public static bool IsOpen => Instance != null;
        #endregion

        #region Enums
        public enum SplineShapeType
        {
            Line,
            Circle,
            SineWave,
            Spiral,
            Square,
            Heart,
            Star
        }
        #endregion

        #region Menu Item
        [MenuItem("NamPhuThuy/Common/Window - Spline Editor (UITK)")]
        public static void ShowWindow()
        {
            var window = GetWindow<Window_SplineEditor_UITK>("Spline Editor (UITK)");
            window.minSize = new Vector2(450, 700);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            Instance = this;
            Selection.selectionChanged += OnSelectionChanged;
            DetectSelectedSpline();
        }

        private void OnDisable()
        {
            Instance = null;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            DetectSelectedSpline();
        }

        public void CreateGUI()
        {
            BuildGUI();
        }
        #endregion

        #region UI Builders
        private void DetectSelectedSpline()
        {
            SplinePath found = null;
            if (Selection.activeGameObject != null)
            {
                found = Selection.activeGameObject.GetComponent<SplinePath>();
            }

            if (found != _targetSpline)
            {
                _targetSpline = found;
                if (_targetSpline != null)
                {
                    _serializedObject = new SerializedObject(_targetSpline);
                }
                else
                {
                    _serializedObject = null;
                }

                RebuildUI();
            }
        }

        private void RebuildUI()
        {
            rootVisualElement.Clear();
            BuildGUI();
        }

        private void BuildGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 15;
            root.style.paddingRight = 15;
            root.style.paddingTop = 15;
            root.style.paddingBottom = 15;

            // Header Section
            var header = new Label("Spline Path Editor")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 18, unityTextAlign = TextAnchor.MiddleCenter, marginBottom = 8, color = new Color(0.2f, 0.7f, 1f) }
            };
            root.Add(header);

            if (_targetSpline == null)
            {
                // EMPTY STATE
                var helpBox = new HelpBox(
                    "No active SplinePath selected.\n\n" +
                    "Select a GameObject in the Hierarchy containing a SplinePath component, or click the button below to instantiate a new Spline in your scene.",
                    HelpBoxMessageType.Warning);
                helpBox.style.marginBottom = 15;
                root.Add(helpBox);

                var btnCreate = new Button(CreateNewSplineInScene)
                {
                    text = "Create New Spline GameObject",
                    style = { height = 36, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13 }
                };
                root.Add(btnCreate);
            }
            else
            {
                // ACTIVE EDIT STATE
                var helpBox = new HelpBox(
                    $"Editing Active GameObject: '{_targetSpline.gameObject.name}'\n\n" +
                    "You can drag control points directly in the Scene view using standard position handles. Use the controls below to configure parameters, distributions, and smoothing.",
                    HelpBoxMessageType.Info);
                helpBox.style.marginBottom = 10;
                root.Add(helpBox);

                var scroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
                root.Add(scroll);

                // 1. Spline Parameters Box
                scroll.Add(BuildParametersBox());

                // 2. Control Points List
                scroll.Add(BuildPointsListBox());

                // 3. Mathematical Tools & Operations
                scroll.Add(BuildSplineToolsBox());

                // 3.5 Spline Shape Generator (NEW!)
                scroll.Add(BuildShapeGeneratorBox());

                // 4. Transform Operations Box
                scroll.Add(BuildTransformOperationsBox());
            }
        }

        private VisualElement BuildBox(string titleText)
        {
            var box = new VisualElement();
            box.style.borderTopWidth = 1; box.style.borderBottomWidth = 1; box.style.borderLeftWidth = 1; box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0.12f, 0.12f, 0.12f, 1f); box.style.borderBottomColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            box.style.borderLeftColor = new Color(0.12f, 0.12f, 0.12f, 1f); box.style.borderRightColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            box.style.borderTopLeftRadius = 5; box.style.borderTopRightRadius = 5;
            box.style.borderBottomLeftRadius = 5; box.style.borderBottomRightRadius = 5;
            box.style.paddingLeft = 12; box.style.paddingRight = 12; box.style.paddingTop = 10; box.style.paddingBottom = 10;
            box.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
            box.style.marginBottom = 12;

            if (!string.IsNullOrEmpty(titleText))
            {
                var title = new Label(titleText) 
                { 
                    style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13, marginBottom = 8, color = new Color(0.85f, 0.85f, 0.85f) } 
                };
                box.Add(title);
            }

            return box;
        }

        private VisualElement BuildParametersBox()
        {
            var box = BuildBox("Spline Settings");
            
            var loopProp = _serializedObject.FindProperty("closedLoop");
            var loopField = new PropertyField(loopProp, "Closed Loop");
            loopField.Bind(_serializedObject);
            box.Add(loopField);

            var colorProp = _serializedObject.FindProperty("pathColor");
            var colorField = new PropertyField(colorProp, "Path Color");
            colorField.Bind(_serializedObject);
            box.Add(colorField);

            var sizeProp = _serializedObject.FindProperty("handleSize");
            var sizeField = new PropertyField(sizeProp, "Scene Handle Size");
            sizeField.Bind(_serializedObject);
            box.Add(sizeField);

            return box;
        }

        private VisualElement BuildPointsListBox()
        {
            var box = BuildBox("Control Points");
            
            var pointsProp = _serializedObject.FindProperty("points");
            var pointsField = new PropertyField(pointsProp, "Spline Points");
            pointsField.Bind(_serializedObject);
            box.Add(pointsField);

            return box;
        }

        private VisualElement BuildSplineToolsBox()
        {
            var box = BuildBox("Mathematical Tools & Operations");

            // Row 1: Basic Points
            var row1 = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6, justifyContent = Justify.SpaceBetween } };
            
            var btnAdd = new Button(AddPointAtEnd) { text = "Add Point", style = { flexGrow = 1, height = 24, marginRight = 4 } };
            row1.Add(btnAdd);

            var btnReverse = new Button(ReversePath) { text = "Reverse Path", style = { flexGrow = 1, height = 24, marginLeft = 2, marginRight = 2 } };
            row1.Add(btnReverse);

            var btnClear = new Button(ClearPoints) { text = "Clear All", style = { flexGrow = 1, height = 24, marginLeft = 4 } };
            row1.Add(btnClear);
            
            box.Add(row1);

            // Row 2: Distribution and Smoothness
            var row2 = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6, justifyContent = Justify.SpaceBetween } };

            var btnLine = new Button(DistributePointsLinearly) { text = "Line Distribute", style = { flexGrow = 1, height = 24, marginRight = 4 } };
            row2.Add(btnLine);

            var btnCircle = new Button(DistributePointsCircularly) { text = "Circle Distribute", style = { flexGrow = 1, height = 24, marginLeft = 2, marginRight = 2 } };
            row2.Add(btnCircle);

            var btnSmooth = new Button(SmoothSpline) { text = "Catmull Smooth", style = { flexGrow = 1, height = 24, marginLeft = 4, unityFontStyleAndWeight = FontStyle.Bold, color = new Color(0.3f, 0.9f, 0.6f) } };
            row2.Add(btnSmooth);

            box.Add(row2);

            return box;
        }

        private VisualElement BuildShapeGeneratorBox()
        {
            var box = BuildBox("Spline Shape Generator (New)");

            // 1. Shape Type Selection
            var shapeRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6, alignItems = Align.Center } };
            var shapeField = new EnumField("Shape Type", _generatorShapeType) { style = { flexGrow = 1 } };
            shapeField.labelElement.style.minWidth = 130;
            shapeField.RegisterValueChangedCallback(evt => _generatorShapeType = (SplineShapeType)evt.newValue);
            shapeRow.Add(shapeField);
            box.Add(shapeRow);

            // 2. Point Count
            var countRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6, alignItems = Align.Center } };
            var countField = new IntegerField("Points Count") { value = _generatorPointsCount, style = { flexGrow = 1 } };
            countField.labelElement.style.minWidth = 130;
            countField.RegisterValueChangedCallback(evt => _generatorPointsCount = Mathf.Max(2, evt.newValue));
            countRow.Add(countField);
            box.Add(countRow);

            // 3. Shape Size
            var sizeRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6, alignItems = Align.Center } };
            var sizeField = new FloatField("Radius / Scale") { value = _generatorShapeSize, style = { flexGrow = 1 } };
            sizeField.labelElement.style.minWidth = 130;
            sizeField.RegisterValueChangedCallback(evt => _generatorShapeSize = evt.newValue);
            sizeRow.Add(sizeField);
            box.Add(sizeRow);

            // 4. Frequency (Wave/Turns/Spikes)
            var freqRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6, alignItems = Align.Center } };
            var freqField = new FloatField("Wave/Turns/Spikes") { value = _generatorFrequency, style = { flexGrow = 1 } };
            freqField.labelElement.style.minWidth = 130;
            freqField.RegisterValueChangedCallback(evt => _generatorFrequency = evt.newValue);
            freqRow.Add(freqField);
            box.Add(freqRow);

            // 5. Helical Height Drift
            var driftRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8, alignItems = Align.Center } };
            var driftField = new FloatField("3D Height Drift") { value = _generatorHeightDrift, style = { flexGrow = 1 } };
            driftField.labelElement.style.minWidth = 130;
            driftField.RegisterValueChangedCallback(evt => _generatorHeightDrift = evt.newValue);
            driftRow.Add(driftField);
            box.Add(driftRow);

            // 6. Generate Button
            var btnGen = new Button(GenerateShapePath) 
            { 
                text = "Generate Spline Shape Path", 
                style = { height = 28, unityFontStyleAndWeight = FontStyle.Bold, color = new Color(0.9f, 0.55f, 0.15f) } 
            };
            box.Add(btnGen);

            return box;
        }

        private VisualElement BuildTransformOperationsBox()
        {
            var box = BuildBox("Coordinate Transformations");

            // 1. Offset
            var offsetRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6, alignItems = Align.Center } };
            var offsetField = new Vector3Field("Offset Vector") { value = _offsetValue, style = { flexGrow = 1, marginRight = 8 } };
            offsetField.labelElement.style.minWidth = 100;
            offsetField.RegisterValueChangedCallback(evt => _offsetValue = evt.newValue);
            offsetRow.Add(offsetField);

            var btnOffset = new Button(ApplyOffsetToPoints) { text = "Apply Offset", style = { width = 100, height = 22 } };
            offsetRow.Add(btnOffset);
            box.Add(offsetRow);

            // 2. Scale
            var scaleRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var scaleField = new FloatField("Scale Factor") { value = _scaleValue, style = { flexGrow = 1, marginRight = 8 } };
            scaleField.labelElement.style.minWidth = 100;
            scaleField.RegisterValueChangedCallback(evt => _scaleValue = evt.newValue);
            scaleRow.Add(scaleField);

            var btnScale = new Button(ApplyScaleToPoints) { text = "Apply Scale", style = { width = 100, height = 22 } };
            scaleRow.Add(btnScale);
            box.Add(scaleRow);

            return box;
        }
        #endregion

        #region Logic Methods
        private void CreateNewSplineInScene()
        {
            GameObject go = new GameObject("New Spline Path");
            _targetSpline = go.AddComponent<SplinePath>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Spline GameObject");
            DetectSelectedSpline();
        }

        private void AddPointAtEnd()
        {
            if (_targetSpline == null) return;
            Undo.RecordObject(_targetSpline, "Add Spline Point");

            Vector3 newPoint = Vector3.zero;
            if (_targetSpline.points.Count > 0)
            {
                Vector3 last = _targetSpline.points[_targetSpline.points.Count - 1];
                newPoint = last + Vector3.right * 2f;
            }
            _targetSpline.points.Add(newPoint);
            EditorUtility.SetDirty(_targetSpline);
            _serializedObject?.Update();
            Repaint();
        }

        private void ReversePath()
        {
            if (_targetSpline == null || _targetSpline.points == null) return;
            Undo.RecordObject(_targetSpline, "Reverse Spline Path");
            _targetSpline.points.Reverse();
            EditorUtility.SetDirty(_targetSpline);
            _serializedObject?.Update();
            Repaint();
        }

        private void ClearPoints()
        {
            if (_targetSpline == null || _targetSpline.points == null) return;
            bool confirm = EditorUtility.DisplayDialog("Clear Spline", "Are you sure you want to delete all control points?", "Yes", "No");
            if (!confirm) return;

            Undo.RecordObject(_targetSpline, "Clear Spline Points");
            _targetSpline.points.Clear();
            EditorUtility.SetDirty(_targetSpline);
            _serializedObject?.Update();
            Repaint();
        }

        private void DistributePointsLinearly()
        {
            if (_targetSpline == null || _targetSpline.points == null || _targetSpline.points.Count < 2) return;
            Undo.RecordObject(_targetSpline, "Linear Distribution");

            Vector3 start = _targetSpline.points[0];
            Vector3 end = _targetSpline.points[_targetSpline.points.Count - 1];
            int count = _targetSpline.points.Count;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                _targetSpline.points[i] = Vector3.Lerp(start, end, t);
            }

            EditorUtility.SetDirty(_targetSpline);
            _serializedObject?.Update();
            Repaint();
        }

        private void DistributePointsCircularly()
        {
            if (_targetSpline == null || _targetSpline.points == null || _targetSpline.points.Count < 3)
            {
                EditorUtility.DisplayDialog("Circle Distribution", "You need at least 3 points to distribute circularly.", "OK");
                return;
            }
            Undo.RecordObject(_targetSpline, "Circular Distribution");

            int count = _targetSpline.points.Count;
            float radius = 3f;

            for (int i = 0; i < count; i++)
            {
                float angle = i * 2.0f * Mathf.PI / count;
                _targetSpline.points[i] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            }

            EditorUtility.SetDirty(_targetSpline);
            _serializedObject?.Update();
            Repaint();
        }

        private void GenerateShapePath()
        {
            if (_targetSpline == null) return;
            Undo.RecordObject(_targetSpline, "Generate Spline Shape");

            List<Vector3> newPoints = new List<Vector3>();
            int count = _generatorPointsCount;
            float size = _generatorShapeSize;
            float freq = _generatorFrequency;
            float drift = _generatorHeightDrift;

            switch (_generatorShapeType)
            {
                case SplineShapeType.Line:
                    for (int i = 0; i < count; i++)
                    {
                        float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                        newPoints.Add(new Vector3(Mathf.Lerp(-size, size, t), 0, 0));
                    }
                    break;

                case SplineShapeType.Circle:
                    for (int i = 0; i < count; i++)
                    {
                        float t = (float)i / count;
                        float angle = t * 2.0f * Mathf.PI;
                        newPoints.Add(new Vector3(Mathf.Cos(angle) * size, t * drift, Mathf.Sin(angle) * size));
                    }
                    break;

                case SplineShapeType.SineWave:
                    for (int i = 0; i < count; i++)
                    {
                        float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                        float x = Mathf.Lerp(-size, size, t);
                        float angle = t * 2.0f * Mathf.PI * freq;
                        float y = Mathf.Sin(angle) * (size * 0.5f);
                        newPoints.Add(new Vector3(x, y, t * drift));
                    }
                    break;

                case SplineShapeType.Spiral:
                    for (int i = 0; i < count; i++)
                    {
                        float t = count > 1 ? (float)i / (count - 1) : 0f;
                        float angle = t * 2.0f * Mathf.PI * freq;
                        float r = t * size;
                        newPoints.Add(new Vector3(Mathf.Cos(angle) * r, t * drift, Mathf.Sin(angle) * r));
                    }
                    break;

                case SplineShapeType.Square:
                    for (int i = 0; i < count; i++)
                    {
                        float t = (float)i / count * 4f; // 0 to 4 boundaries
                        float x = 0, z = 0;
                        if (t < 1f) { x = -size + 2f * size * t; z = -size; }
                        else if (t < 2f) { x = size; z = -size + 2f * size * (t - 1f); }
                        else if (t < 3f) { x = size - 2f * size * (t - 2f); z = size; }
                        else { x = -size; z = size - 2f * size * (t - 3f); }
                        newPoints.Add(new Vector3(x, 0, z));
                    }
                    break;

                case SplineShapeType.Heart:
                    for (int i = 0; i < count; i++)
                    {
                        float t = (float)i / count * 2f * Mathf.PI;
                        float scale = size / 16f;
                        float x = 16f * Mathf.Pow(Mathf.Sin(t), 3) * scale;
                        float z = (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t)) * scale;
                        newPoints.Add(new Vector3(x, t * drift, z));
                    }
                    break;

                case SplineShapeType.Star:
                    int spikes = Mathf.Max(3, Mathf.RoundToInt(freq));
                    for (int i = 0; i < count; i++)
                    {
                        float t = (float)i / count;
                        float angle = t * 2.0f * Mathf.PI;
                        float spikeVal = Mathf.Cos(angle * spikes);
                        float r = Mathf.Lerp(size * 0.4f, size, (spikeVal + 1f) * 0.5f);
                        newPoints.Add(new Vector3(Mathf.Cos(angle) * r, t * drift, Mathf.Sin(angle) * r));
                    }
                    break;
            }

            _targetSpline.points = newPoints;
            EditorUtility.SetDirty(_targetSpline);
            _serializedObject?.Update();
            Repaint();
            Debug.Log($"Generated {_generatorShapeType} shape with {newPoints.Count} points.");
        }

        private Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            float f0 = -0.5f * t3 + t2 - 0.5f * t;
            float f1 = 1.5f * t3 - 2.5f * t2 + 1.0f;
            float f2 = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
            float f3 = 0.5f * t3 - 0.5f * t2;

            return p0 * f0 + p1 * f1 + p2 * f2 + p3 * f3;
        }

        private void SmoothSpline()
        {
            if (_targetSpline == null || _targetSpline.points == null || _targetSpline.points.Count < 3)
            {
                EditorUtility.DisplayDialog("Smooth Spline Warning", "You need at least 3 points to smooth the spline.", "OK");
                return;
            }

            Undo.RecordObject(_targetSpline, "Smooth Spline");

            List<Vector3> original = new List<Vector3>(_targetSpline.points);
            List<Vector3> smoothed = new List<Vector3>();

            int n = original.Count;
            int subdivisions = 2; // Add 2 intermediate points per segment

            for (int i = 0; i < ( _targetSpline.closedLoop ? n : n - 1 ); i++)
            {
                Vector3 p1 = original[i];
                Vector3 p2 = original[(i + 1) % n];

                // Determine control points P0 and P3
                Vector3 p0, p3;

                if (_targetSpline.closedLoop)
                {
                    p0 = original[(i - 1 + n) % n];
                    p3 = original[(i + 2) % n];
                }
                else
                {
                    p0 = (i == 0) ? p1 + (p1 - p2) : original[i - 1];
                    p3 = (i == n - 2) ? p2 + (p2 - p1) : original[i + 2];
                }

                // Add starting point of segment
                smoothed.Add(p1);

                // Add intermediate subdivided points
                for (int step = 1; step <= subdivisions; step++)
                {
                    float t = (float)step / (subdivisions + 1);
                    smoothed.Add(GetCatmullRomPosition(t, p0, p1, p2, p3));
                }
            }

            // Add the very last point if open loop
            if (!_targetSpline.closedLoop)
            {
                smoothed.Add(original[n - 1]);
            }

            _targetSpline.points = smoothed;
            EditorUtility.SetDirty(_targetSpline);
            _serializedObject?.Update();
            Repaint();

            Debug.Log($"Smoothed spline: increased point count from {n} to {smoothed.Count}.");
        }

        private void ApplyOffsetToPoints()
        {
            if (_targetSpline == null || _targetSpline.points == null || _targetSpline.points.Count == 0) return;
            Undo.RecordObject(_targetSpline, "Offset Spline Points");

            for (int i = 0; i < _targetSpline.points.Count; i++)
            {
                _targetSpline.points[i] += _offsetValue;
            }

            EditorUtility.SetDirty(_targetSpline);
            _serializedObject?.Update();
            Repaint();
            Debug.Log($"Applied offset {_offsetValue} to all spline points.");
        }

        private void ApplyScaleToPoints()
        {
            if (_targetSpline == null || _targetSpline.points == null || _targetSpline.points.Count == 0) return;
            Undo.RecordObject(_targetSpline, "Scale Spline Points");

            // Calculate center
            Vector3 sum = Vector3.zero;
            foreach (var p in _targetSpline.points) sum += p;
            Vector3 center = sum / _targetSpline.points.Count;

            // Scale relative to center
            for (int i = 0; i < _targetSpline.points.Count; i++)
            {
                _targetSpline.points[i] = center + ( _targetSpline.points[i] - center ) * _scaleValue;
            }

            EditorUtility.SetDirty(_targetSpline);
            _serializedObject?.Update();
            Repaint();
            Debug.Log($"Scaled spline points by factor of {_scaleValue} around center {center}.");
        }
        #endregion
    }

    // ─────────────── 2. CUSTOM SCENE-VIEW HANDLES INSPECTOR ───────────────
    [CustomEditor(typeof(SplinePath))]
    public class SplinePathEditor : Editor
    {
        private void OnSceneGUI()
        {
            SplinePath spline = (SplinePath)target;
            if (spline == null || spline.points == null) return;

            bool changed = false;

            for (int i = 0; i < spline.points.Count; i++)
            {
                Vector3 worldPos = spline.transform.TransformPoint(spline.points[i]);
                float size = HandleUtility.GetHandleSize(worldPos) * spline.handleSize;
                
                // Draw color handles: Green for start, Red for end, light blue for internal
                Color handleColor = (i == 0) ? Color.green : (i == spline.points.Count - 1) ? Color.red : spline.pathColor;
                Handles.color = handleColor;
                
                // Draw a position handle in the scene view
                EditorGUI.BeginChangeCheck();
                Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(spline, "Move Spline Point");
                    spline.points[i] = spline.transform.InverseTransformPoint(newWorldPos);
                    changed = true;
                }

                // Draw index label near the point
                Handles.Label(worldPos + Vector3.up * (size * 0.4f), $"P{i}", EditorStyles.boldLabel);
            }

            if (changed)
            {
                EditorUtility.SetDirty(spline);
                
                // Keep the active UITK editor window aligned and in sync
                if (Window_SplineEditor_UITK.IsOpen)
                {
                    Window_SplineEditor_UITK.Instance.Repaint();
                }
            }
        }
    }
#endif
}
