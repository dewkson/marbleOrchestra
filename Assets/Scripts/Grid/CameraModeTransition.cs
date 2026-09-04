using System.Collections;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Lerps the MainCamera between the flat 2D planning view (as computed
    /// by CameraFitter, top-down onto the grid - see 0029) and a diagonal,
    /// isometric-looking view of the spawned 3D TrackBlocks whenever
    /// MarbleController's play state flips (see 0012's SPACE toggle) - so
    /// the 2D-to-3D switch reads as a camera move rather than a hard cut.
    /// Once that entry transition settles, the camera doesn't stay put: it
    /// keeps gently re-centering (SmoothDamp, see FollowMarble) on
    /// MarbleController's currently tracked marble at a closer, dedicated
    /// zoom than the whole-track framing it arrived at (see 0035).
    /// Stays orthographic the whole time; only Transform position/rotation
    /// and Camera.orthographicSize are interpolated. Polls
    /// MarbleController.IsPlaying every frame, matching PlaybackHintUI's
    /// existing pattern, instead of wiring a new event into
    /// MarbleController.
    /// The 3D target pose is derived every time from
    /// TrackBlockSpawner.TryGetTracksWorldBounds - the exact world bounds
    /// of the currently spawned blocks - by projecting its 8 corners onto
    /// the isometric camera's own right/up/forward axes (BoundsCameraMath),
    /// so every block stays fully inside the frame regardless of track
    /// length or shape. The 2D target pose (position, rotation AND size)
    /// is asked from CameraFitter wholesale, rather than this component
    /// keeping its own copy of the planning rotation - CameraFitter is the
    /// single source of truth for what the 2D view looks like.
    /// Lives on the MainCamera; marbleController/terrain/cameraFitter are
    /// wired in the Inspector or auto-found at Awake.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraModeTransition : MonoBehaviour
    {
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private TrackBlockSpawner terrain;
        [SerializeField] private CameraFitter cameraFitter;

        [SerializeField] private float pitchDegrees = 35.264f; // true isometric tilt
        [SerializeField] private float yawDegrees = -45f;
        [SerializeField] private float padding = 1f;
        [SerializeField] private float nearMargin = 2f; // extra room between the camera and the nearest block, so it never pokes through the near clip plane
        [SerializeField] private float transitionDuration = 1.1f;

        [SerializeField] private float followDistance = 3f; // camera-to-marble distance along the isometric forward axis while following - closer than the whole-track fit, so the marble reads as the focus
        [SerializeField] private float followOrthographicSize = 1.5f; // tighter zoom used once following starts, replacing the whole-track framing
        [SerializeField] private float followSmoothTime = 0.3f; // SmoothDamp time constant - the "sanft" in sanfter Kamera-Follow

        private readonly struct CameraPose
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly float OrthographicSize;

            public CameraPose(Vector3 position, Quaternion rotation, float orthographicSize)
            {
                Position = position;
                Rotation = rotation;
                OrthographicSize = orthographicSize;
            }
        }

        private Camera cam;
        private bool wasPlaying;
        private Coroutine transitionRoutine;
        private Vector3 followVelocity;
        private float followSizeVelocity;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            if (terrain == null) terrain = FindAnyObjectByType<TrackBlockSpawner>();
            if (cameraFitter == null) cameraFitter = GetComponent<CameraFitter>();
        }

        private void Update()
        {
            if (marbleController == null) return;

            bool isPlaying = marbleController.IsPlaying;
            if (isPlaying != wasPlaying)
            {
                wasPlaying = isPlaying;
                StartTransitionTo(isPlaying);
                return;
            }

            // Once the one-shot entry transition into the isometric view
            // has settled, keep gently re-centering on the marble every
            // frame instead of staying fixed on the whole-track pose it
            // ended on.
            if (isPlaying && transitionRoutine == null) FollowMarble();
        }

        private void StartTransitionTo(bool playing)
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            if (playing) followVelocity = Vector3.zero; // fresh follow, no leftover SmoothDamp momentum from a previous run

            CameraPose from = new CameraPose(transform.position, transform.rotation, cam.orthographicSize);
            CameraPose to = playing ? ComputeIsometricPose(from) : GetPlanPose(from);

            transitionRoutine = StartCoroutine(LerpPose(from, to));
        }

        private CameraPose GetPlanPose(CameraPose fallback)
        {
            if (cameraFitter != null && cameraFitter.TryComputeFitPose(out Vector3 position, out Quaternion rotation, out float orthographicSize))
                return new CameraPose(position, rotation, orthographicSize);

            return fallback;
        }

        /// Frames terrain's current track bounds fully on screen from a
        /// fixed isometric angle: rotates into that angle first, then
        /// measures how far each of the bounds' 8 corners reaches along
        /// the camera's own right/up/forward axes, so the orthographic
        /// size and camera distance are exactly as large as needed - never
        /// more, never so little a block gets clipped.
        private CameraPose ComputeIsometricPose(CameraPose fallback)
        {
            if (terrain == null || !terrain.TryGetTracksWorldBounds(out Bounds bounds)) return fallback;

            Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            Vector3 forward = rotation * Vector3.forward;

            BoundsCameraMath.Extents extents = BoundsCameraMath.MeasureExtents(bounds, rotation);

            float orthographicSize = Mathf.Max(extents.Up, extents.Right / cam.aspect) + padding;
            float distance = extents.Forward + nearMargin;
            Vector3 position = bounds.center - forward * distance;

            return new CameraPose(position, rotation, orthographicSize);
        }

        /// Keeps the camera centered on MarbleController's currently
        /// tracked marble, at the same fixed isometric rotation the entry
        /// transition ended on but a closer, dedicated follow distance/
        /// zoom instead of the whole-track framing - smoothed with
        /// SmoothDamp (rather than snapping straight to the marble) so the
        /// follow reads as gentle even though the marble itself can change
        /// direction abruptly at track corners.
        private void FollowMarble()
        {
            Transform target = marbleController.PrimaryMarbleTransform;
            if (target == null) return;

            Vector3 forward = transform.rotation * Vector3.forward;
            Vector3 desiredPosition = target.position - forward * followDistance;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, followOrthographicSize, ref followSizeVelocity, followSmoothTime);
        }

        private IEnumerator LerpPose(CameraPose from, CameraPose to)
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

                transform.position = Vector3.Lerp(from.Position, to.Position, t);
                transform.rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t);
                cam.orthographicSize = Mathf.Lerp(from.OrthographicSize, to.OrthographicSize, t);

                yield return null;
            }

            transform.position = to.Position;
            transform.rotation = to.Rotation;
            cam.orthographicSize = to.OrthographicSize;
            transitionRoutine = null;
        }
    }
}
