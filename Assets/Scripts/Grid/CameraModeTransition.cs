using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Lerps the MainCamera between the flat 2D planning view (as computed
    /// by CameraFitter) and a diagonal, isometric-looking view of the
    /// spawned 3D TrackBlocks whenever MarbleController's play state flips
    /// (see 0012's SPACE toggle) - so the 2D-to-3D switch reads as a camera
    /// move rather than a hard cut (see 0029). Stays orthographic the whole
    /// time; only Transform position/rotation and Camera.orthographicSize
    /// are interpolated. Polls MarbleController.IsPlaying every frame,
    /// matching PlaybackHintUI's existing pattern, instead of wiring a new
    /// event into MarbleController.
    /// The 3D target pose is derived every time from
    /// TrackBlockSpawner.TryGetTracksWorldBounds - the exact world bounds
    /// of the currently spawned blocks - by projecting its 8 corners onto
    /// the isometric camera's own right/up/forward axes, so every block
    /// stays fully inside the frame regardless of track length or shape.
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
        [SerializeField] private float yawDegrees = 45f;
        [SerializeField] private float padding = 1f;
        [SerializeField] private float nearMargin = 2f; // extra room between the camera and the nearest block, so it never pokes through the near clip plane
        [SerializeField] private float transitionDuration = 1.1f;

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
        private Quaternion planRotation;
        private bool wasPlaying;
        private Coroutine transitionRoutine;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            if (terrain == null) terrain = FindAnyObjectByType<TrackBlockSpawner>();
            if (cameraFitter == null) cameraFitter = GetComponent<CameraFitter>();

            // CameraFitter (see 0001) never touches rotation, only
            // position/orthographicSize - so whatever rotation the camera
            // has at Awake is the 2D planning rotation for the rest of
            // this component's life.
            planRotation = transform.rotation;
        }

        private void Update()
        {
            if (marbleController == null) return;

            bool isPlaying = marbleController.IsPlaying;
            if (isPlaying == wasPlaying) return;

            wasPlaying = isPlaying;
            StartTransitionTo(isPlaying);
        }

        private void StartTransitionTo(bool playing)
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);

            CameraPose from = new CameraPose(transform.position, transform.rotation, cam.orthographicSize);
            CameraPose to = playing ? ComputeIsometricPose(from) : GetPlanPose(from);

            transitionRoutine = StartCoroutine(LerpPose(from, to));
        }

        private CameraPose GetPlanPose(CameraPose fallback)
        {
            if (cameraFitter != null && cameraFitter.TryComputeFitPose(out Vector3 position, out float orthographicSize))
                return new CameraPose(position, planRotation, orthographicSize);

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
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;

            Vector3 center = bounds.center;
            float maxRight = 0f, maxUp = 0f, maxForward = 0f;

            foreach (Vector3 corner in EnumerateCorners(bounds))
            {
                Vector3 d = corner - center;
                maxRight = Mathf.Max(maxRight, Mathf.Abs(Vector3.Dot(d, right)));
                maxUp = Mathf.Max(maxUp, Mathf.Abs(Vector3.Dot(d, up)));
                maxForward = Mathf.Max(maxForward, Mathf.Abs(Vector3.Dot(d, forward)));
            }

            float orthographicSize = Mathf.Max(maxUp, maxRight / cam.aspect) + padding;
            float distance = maxForward + nearMargin;
            Vector3 position = center - forward * distance;

            return new CameraPose(position, rotation, orthographicSize);
        }

        private static IEnumerable<Vector3> EnumerateCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            yield return new Vector3(min.x, min.y, min.z);
            yield return new Vector3(min.x, min.y, max.z);
            yield return new Vector3(min.x, max.y, min.z);
            yield return new Vector3(min.x, max.y, max.z);
            yield return new Vector3(max.x, min.y, min.z);
            yield return new Vector3(max.x, min.y, max.z);
            yield return new Vector3(max.x, max.y, min.z);
            yield return new Vector3(max.x, max.y, max.z);
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
