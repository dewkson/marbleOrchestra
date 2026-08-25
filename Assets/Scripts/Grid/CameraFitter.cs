using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Fits the camera's orthographic view to the currently loaded level's
    /// grid, so the whole grid is visible regardless of its size or the
    /// screen's aspect ratio.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        [SerializeField] private PathGrid grid;
        [SerializeField] private float padding = 0.6f;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            Fit();
        }

        public void Fit()
        {
            if (grid == null || cam == null || !cam.orthographic) return;
            if (grid.Width <= 0 || grid.Height <= 0) return;

            Vector3 min = grid.transform.TransformPoint(grid.CellToLocalPosition(Vector2Int.zero));
            Vector3 max = grid.transform.TransformPoint(grid.CellToLocalPosition(new Vector2Int(grid.Width - 1, grid.Height - 1)));

            float gridWidth = Mathf.Abs(max.x - min.x) + padding * 2f;
            float gridHeight = Mathf.Abs(max.y - min.y) + padding * 2f;

            float verticalSize = gridHeight / 2f;
            float horizontalSize = gridWidth / (2f * cam.aspect);

            cam.orthographicSize = Mathf.Max(verticalSize, horizontalSize);

            Vector3 center = (min + max) / 2f;
            transform.position = new Vector3(center.x, center.y, transform.position.z);
        }
    }
}
