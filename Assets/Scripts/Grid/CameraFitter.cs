using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Fits the camera's orthographic view to the currently loaded level's
    /// grid, so the whole grid is visible regardless of its size or the
    /// screen's aspect ratio - and regardless of how the grid/camera are
    /// oriented in the scene (e.g. the top-down planning view added for
    /// 0029, as opposed to the original front-on view): the math projects
    /// the grid's world-space corners onto the camera's OWN right/up/
    /// forward axes (see BoundsCameraMath) rather than assuming world X/Y.
    /// The camera's rotation for this fit is whatever it already has at
    /// Awake (the scene's authored 2D planning angle) - Fit() never
    /// changes it again, so CameraModeTransition (see 0029) can freely
    /// rotate the same camera into its isometric 3D pose without ever
    /// fighting this component; TryComputeFitPose always reports that
    /// original rotation back, not the camera's current, possibly
    /// mid-transition one.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        [SerializeField] private PathGrid grid;
        [SerializeField] private float padding = 0.6f;

        private Camera cam;
        private Quaternion planRotation;
        private Vector3 planPosition; // raw position at Awake, before CameraModeTransition (see 0029) can ever move the camera into its 3D pose - see TryComputeFitPose

        private void Awake()
        {
            cam = GetComponent<Camera>();
            planRotation = transform.rotation;
            planPosition = transform.position;
        }

        private void Start()
        {
            Fit();
        }

        public void Fit()
        {
            if (!TryComputeFitPose(out Vector3 position, out Quaternion rotation, out float orthographicSize, out _)) return;

            cam.orthographicSize = orthographicSize;
            transform.SetPositionAndRotation(position, rotation);
        }

        /// Pure query version of Fit()'s math - used by Fit() itself and by
        /// CameraModeTransition (see 0029) to know where the 2D planning
        /// camera belongs without actually moving the camera there.
        ///
        /// focus is the point this framing centers on. It matters to
        /// CameraModeTransition separately from position, because an
        /// orthographic camera's position along its own forward axis says
        /// nothing about which point it is framing - and its 2D-to-3D
        /// transition has to interpolate the framed point, not the camera
        /// position, to keep its camera distance free of visual side
        /// effects (see CameraModeTransition.LerpPose).
        public bool TryComputeFitPose(out Vector3 position, out Quaternion rotation, out float orthographicSize, out Vector3 focus)
        {
            position = default;
            rotation = planRotation;
            orthographicSize = default;
            focus = default;

            if (grid == null || cam == null || !cam.orthographic) return false;
            if (grid.Width <= 0 || grid.Height <= 0) return false;

            Bounds bounds = ComputeGridWorldBounds();
            BoundsCameraMath.Extents extents = BoundsCameraMath.MeasureExtents(bounds, planRotation);

            Vector3 forward = planRotation * Vector3.forward;
            Vector3 up = planRotation * Vector3.up;

            // The camera only covers the left game column (see
            // GameViewLayout), so its own pixel aspect decides the fit; the
            // grid is centered in it with just the padding as margin.
            float aspect = (float)cam.pixelWidth / Mathf.Max(1, cam.pixelHeight);
            float verticalSize = extents.Up + padding;
            float horizontalSize = (extents.Right + padding) / aspect;
            orthographicSize = Mathf.Max(verticalSize, horizontalSize);

            Vector3 center = bounds.center;

            // Keep whatever distance from the grid plane the camera was
            // originally placed at in the scene (along its own forward
            // axis) - Fit() only ever needs to solve the in-plane framing,
            // not how far back the camera sits. Deliberately measured from
            // the Awake-time position, NOT the camera's current one: once
            // CameraModeTransition (see 0029) has moved the camera into
            // its 3D pose and back, transform.position no longer reflects
            // the original 2D distance at all.
            float forwardOffset = Vector3.Dot(planPosition - center, forward);
            position = center + forward * forwardOffset;
            focus = center;
            return true;
        }

        /// World-space bounds of the four corner cells of the currently
        /// active SubLevel's own area (see 0046's PathGrid.ActiveSubLevelArea -
        /// the whole grid if no SubLevels are defined, keeping older
        /// single-puzzle levels framed exactly as before), using the
        /// grid's actual transform (position AND rotation) - not just its
        /// local cell coordinates - so it's correct for any grid
        /// orientation, not only an axis-aligned one.
        private Bounds ComputeGridWorldBounds()
        {
            RectInt area = grid.ActiveSubLevelArea;
            Vector2Int min = new Vector2Int(area.xMin, area.yMin);
            Vector2Int max = new Vector2Int(area.xMax - 1, area.yMax - 1);

            Vector3 c00 = grid.transform.TransformPoint(grid.CellToLocalPosition(min));
            Vector3 c10 = grid.transform.TransformPoint(grid.CellToLocalPosition(new Vector2Int(max.x, min.y)));
            Vector3 c01 = grid.transform.TransformPoint(grid.CellToLocalPosition(new Vector2Int(min.x, max.y)));
            Vector3 c11 = grid.transform.TransformPoint(grid.CellToLocalPosition(max));

            Bounds bounds = new Bounds(c00, Vector3.zero);
            bounds.Encapsulate(c10);
            bounds.Encapsulate(c01);
            bounds.Encapsulate(c11);
            return bounds;
        }
    }
}
