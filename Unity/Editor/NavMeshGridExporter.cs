#if UNITY_EDITOR
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace HakJang.Editor
{
    public class NavMeshGridExporter : OdinEditorWindow
    {
        [Title("NavMesh → Grid Converter", "NavMesh를 2D 그리드로 변환하여 JSON으로 추출합니다.")]

        [BoxGroup("Settings")]
        [LabelText("Cell Size (m)"), Min(0.1f)]
        [SerializeField] private float _cellSize = 1f;

        [BoxGroup("Bounds")]
        [LabelText("Center")]
        [SerializeField] private Vector3 _boundsCenter;

        [BoxGroup("Bounds")]
        [LabelText("Size"), ReadOnly]
        [SerializeField] private Vector3 _boundsSize;

        [BoxGroup("Bounds/Info")]
        [ShowInInspector, ReadOnly, HideLabel]
        private string GridInfo => _boundsInitialized
            ? $"Grid Size: {Mathf.CeilToInt(_boundsSize.x / _cellSize)} x {Mathf.CeilToInt(_boundsSize.z / _cellSize)} ({Mathf.CeilToInt(_boundsSize.x / _cellSize) * Mathf.CeilToInt(_boundsSize.z / _cellSize)} cells)"
            : "NavMesh 감지 필요";

        private Bounds _bounds;
        private bool _boundsInitialized;
        private int[,] _previewGrid;
        private bool _showSceneGizmo;

        [MenuItem("RT/NavMesh Grid Exporter")]
        public static void ShowWindow()
        {
            GetWindow<NavMeshGridExporter>("NavMesh Grid Exporter");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SceneView.duringSceneGui += OnSceneGUI;
            CalculateBoundsFromNavMesh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        [BoxGroup("Bounds")]
        [Button("Auto Detect Bounds", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        private void CalculateBoundsFromNavMesh()
        {
            var triangulation = NavMesh.CalculateTriangulation();

            if (triangulation.vertices.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "NavMesh가 베이크되지 않았습니다.", "OK");
                return;
            }

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var v in triangulation.vertices)
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

            _bounds = new Bounds((min + max) / 2f, max - min);
            _boundsCenter = _bounds.center;
            _boundsSize = _bounds.size;
            _boundsInitialized = true;
        }

        private void SyncBounds()
        {
            _bounds.center = _boundsCenter;
            _bounds.size = _boundsSize;
        }

        [HorizontalGroup("Actions", Width = 0.5f)]
        [Button("Preview", ButtonSizes.Large), GUIColor(0.5f, 1f, 0.5f)]
        private void Preview()
        {
            SyncBounds();
            _previewGrid = GenerateGrid();
            _showSceneGizmo = true;
            SceneView.RepaintAll();
        }

        [HorizontalGroup("Actions")]
        [Button("Export JSON", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.3f)]
        private void ExportJson()
        {
            SyncBounds();
            var grid = GenerateGrid();
            if (grid == null) return;

            int height = grid.GetLength(0);
            int width = grid.GetLength(1);

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"cellSize\": {_cellSize},");
            sb.AppendLine($"  \"width\": {width},");
            sb.AppendLine($"  \"height\": {height},");
            sb.AppendLine($"  \"originX\": {_bounds.min.x},");
            sb.AppendLine($"  \"originZ\": {_bounds.min.z},");
            sb.AppendLine("  \"grid\": [");

            for (int z = 0; z < height; z++)
            {
                sb.Append("    [");
                for (int x = 0; x < width; x++)
                {
                    sb.Append(grid[z, x]);
                    if (x < width - 1) sb.Append(',');
                }
                sb.Append(']');
                if (z < height - 1) sb.Append(',');
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.Append('}');

            string path = EditorUtility.SaveFilePanel("Export Grid", "", "navmesh_grid", "json");
            if (string.IsNullOrEmpty(path)) return;

            File.WriteAllText(path, sb.ToString());
            EditorUtility.DisplayDialog("Complete", $"Exported: {width}x{height} grid\n{path}", "OK");
        }

        private int[,] GenerateGrid()
        {
            var triangulation = NavMesh.CalculateTriangulation();
            var vertices = triangulation.vertices;
            var indices = triangulation.indices;

            int width = Mathf.CeilToInt(_bounds.size.x / _cellSize);
            int height = Mathf.CeilToInt(_bounds.size.z / _cellSize);
            var grid = new int[height, width];

            for (int z = 0; z < height; z++)
                for (int x = 0; x < width; x++)
                    grid[z, x] = 1;

            Vector3 origin = _bounds.min;

            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 a = vertices[indices[i]];
                Vector3 b = vertices[indices[i + 1]];
                Vector3 c = vertices[indices[i + 2]];

                float minX = Mathf.Min(a.x, b.x, c.x);
                float maxX = Mathf.Max(a.x, b.x, c.x);
                float minZ = Mathf.Min(a.z, b.z, c.z);
                float maxZ = Mathf.Max(a.z, b.z, c.z);

                int startX = Mathf.Max(0, Mathf.FloorToInt((minX - origin.x) / _cellSize));
                int endX = Mathf.Min(width - 1, Mathf.CeilToInt((maxX - origin.x) / _cellSize));
                int startZ = Mathf.Max(0, Mathf.FloorToInt((minZ - origin.z) / _cellSize));
                int endZ = Mathf.Min(height - 1, Mathf.CeilToInt((maxZ - origin.z) / _cellSize));

                for (int gz = startZ; gz <= endZ; gz++)
                {
                    for (int gx = startX; gx <= endX; gx++)
                    {
                        if (grid[gz, gx] == 0) continue;

                        float worldX = origin.x + (gx + 0.5f) * _cellSize;
                        float worldZ = origin.z + (gz + 0.5f) * _cellSize;

                        if (PointInTriangleXZ(worldX, worldZ, a, b, c))
                            grid[gz, gx] = 0;
                    }
                }
            }

            return grid;
        }

        private static bool PointInTriangleXZ(float px, float pz, Vector3 a, Vector3 b, Vector3 c)
        {
            float d1 = CrossSign(px, pz, a.x, a.z, b.x, b.z);
            float d2 = CrossSign(px, pz, b.x, b.z, c.x, c.z);
            float d3 = CrossSign(px, pz, c.x, c.z, a.x, a.z);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        private static float CrossSign(float p1x, float p1z, float p2x, float p2z, float p3x, float p3z)
        {
            return (p1x - p3x) * (p2z - p3z) - (p2x - p3x) * (p1z - p3z);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_previewGrid == null || !_showSceneGizmo) return;

            int height = _previewGrid.GetLength(0);
            int width = _previewGrid.GetLength(1);

            Vector3 origin = _bounds.min;
            Color walkableColor = new Color(0f, 1f, 0f, 0.3f);
            Color blockedColor = new Color(1f, 0f, 0f, 0.3f);
            float y = _bounds.center.y + 0.05f;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    Handles.color = _previewGrid[z, x] == 0 ? walkableColor : blockedColor;

                    Vector3 center = new Vector3(
                        origin.x + (x + 0.5f) * _cellSize,
                        y,
                        origin.z + (z + 0.5f) * _cellSize
                    );

                    Vector3 size = new Vector3(_cellSize, 0f, _cellSize);
                    Handles.DrawSolidRectangleWithOutline(
                        new[]
                        {
                            center + new Vector3(-size.x, 0, -size.z) * 0.5f,
                            center + new Vector3(-size.x, 0, size.z) * 0.5f,
                            center + new Vector3(size.x, 0, size.z) * 0.5f,
                            center + new Vector3(size.x, 0, -size.z) * 0.5f,
                        },
                        Handles.color,
                        Color.clear
                    );
                }
            }
        }

        [OnInspectorGUI, PropertyOrder(99)]
        private void DrawSceneGizmoToggle()
        {
            if (_previewGrid == null) return;

            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            _showSceneGizmo = EditorGUILayout.Toggle("Scene View Overlay", _showSceneGizmo);
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        [OnInspectorGUI, PropertyOrder(100)]
        private void DrawPreview()
        {
            if (_previewGrid == null) return;

            int height = _previewGrid.GetLength(0);
            int width = _previewGrid.GetLength(1);

            EditorGUILayout.Space(10);

            var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField($"Preview ({width} x {height})", style);

            EditorGUILayout.Space(5);

            float maxPreviewSize = 300f;
            float cellPixel = Mathf.Min(maxPreviewSize / width, maxPreviewSize / height, 8f);
            cellPixel = Mathf.Max(cellPixel, 2f);

            float totalWidth = width * cellPixel;
            float totalHeight = height * cellPixel;

            Rect area = GUILayoutUtility.GetRect(totalWidth, totalHeight);
            float offsetX = area.x + (area.width - totalWidth) * 0.5f;

            Color walkableColor = new Color(0.3f, 0.85f, 0.4f);
            Color blockedColor = new Color(0.85f, 0.25f, 0.25f);

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    Rect cell = new Rect(
                        offsetX + x * cellPixel,
                        area.y + (height - 1 - z) * cellPixel,
                        cellPixel, cellPixel
                    );
                    EditorGUI.DrawRect(cell, _previewGrid[z, x] == 0 ? walkableColor : blockedColor);
                }
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(12, 12), walkableColor);
            GUILayout.Label("Walkable", GUILayout.Width(60));
            GUILayout.Space(10);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(12, 12), blockedColor);
            GUILayout.Label("Blocked", GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
