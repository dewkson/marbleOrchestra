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
        [SerializeField] private float bottomHintSpace = 1.2f;

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
            if (!TryComputeFitPose(out Vector3 position, out float orthographicSize)) return;

            cam.orthographicSize = orthographicSize;
            transform.position = position;
        }

        /// Pure query version of Fit()'s math - used by Fit() itself and by
        /// CameraModeTransition (see 0029) to know where the 2D planning
        /// camera belongs without actually moving the camera there.
        public bool TryComputeFitPose(out Vector3 position, out float orthographicSize)
        {
            position = default;
            orthographicSize = default;

            if (grid == null || cam == null || !cam.orthographic) return false;
            if (grid.Width <= 0 || grid.Height <= 0) return false;

            Vector3 min = grid.transform.TransformPoint(grid.CellToLocalPosition(Vector2Int.zero));
            Vector3 max = grid.transform.TransformPoint(grid.CellToLocalPosition(new Vector2Int(grid.Width - 1, grid.Height - 1)));

            float gridWidth = Mathf.Abs(max.x - min.x) + padding * 2f;
            // bottomHintSpace is added only on top of the symmetric padding,
            // then the whole view is shifted down by half of it below - so
            // the grid moves up, freeing exactly that much room at the
            // bottom of the screen for the SPACE hint (PlaybackHintUI).
            float gridHeight = Mathf.Abs(max.y - min.y) + padding * 2f + bottomHintSpace;

            float verticalSize = gridHeight / 2f;
            float horizontalSize = gridWidth / (2f * cam.aspect);

            orthographicSize = Mathf.Max(verticalSize, horizontalSize);

            Vector3 center = (min + max) / 2f;
            center.y -= bottomHintSpace / 2f;
            position = new Vector3(center.x, center.y, transform.position.z);
            return true;
        }
    }
}
