using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
    /// TrackBlockSpawner.TryGetTracksWorldBounds - the world bounds of the
    /// currently active SubLevel's own spawned blocks (see 0046 - other,
    /// already-unlocked SubLevels may be looping alongside it, but the
    /// guided camera only ever frames the one being worked on) - by
    /// projecting its 8 corners onto the isometric camera's own
    /// right/up/forward axes (BoundsCameraMath), so every block stays
    /// fully inside the frame regardless of track length or shape. The
    /// marble it then follows (see FollowMarble) is likewise scoped to
    /// that same active SubLevel (MarbleController.PrimaryMarbleTransform).
    /// The 2D target pose (position, rotation AND size)
    /// is asked from CameraFitter wholesale, rather than this component
    /// keeping its own copy of the planning rotation - CameraFitter is the
    /// single source of truth for what the 2D view looks like.
    /// Lives on the MainCamera; marbleController/terrain/cameraFitter are
    /// wired in the Inspector or auto-found at Awake.
    /// A left-click/touch drag or a scroll/pinch zoom while simulating
    /// breaks FollowMarble's auto-centering and hands the camera to the
    /// player instead (see 0044): position pans along the camera's own
    /// right/up axes and zoom only ever touches orthographicSize, so the
    /// fixed isometric rotation from pitchDegrees/yawDegrees is never
    /// touched by player input. FreeCameraReturnButtonUI reads IsFreeCamera
    /// to show a button back to ReturnToGuidedCamera, which simply lets
    /// FollowMarble's own SmoothDamp ease back onto the marble.
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
        [SerializeField] private float transitionDuration = 1.1f;

        // How far the camera pulls BACK along its own forward axis beyond
        // the nearest geometry - used both for the isometric whole-track
        // pose and for the marble follow, so there is exactly one number
        // deciding clipping headroom.
        //
        // The projection is orthographic, so this changes nothing about
        // framing, apparent size or perspective: it only decides how much
        // room there is in front of the camera. Hence the deliberately
        // huge value. A modest margin used to be enough for the settled
        // poses, but the 2D->3D transition lerps position and rotation
        // independently (see LerpPose), so mid-transition the camera swept
        // close enough to the terrain for the near clip plane to slice
        // visibly through blocks. Starting from this far out, no point on
        // that path ever gets near the terrain again.
        //
        // Cheap, too: an orthographic camera's depth buffer is linear, so
        // a view slab this deep costs none of the depth precision the same
        // range would cost under perspective projection.
        [SerializeField] private float forwardClearance = 2000f;
        [SerializeField] private float followOrthographicSize = 1.5f; // tighter zoom used once following starts, replacing the whole-track framing
        [SerializeField] private float followSmoothTime = 0.3f; // SmoothDamp time constant - the "sanft" in sanfter Kamera-Follow

        [Header("Free Camera (see 0044)")]
        [SerializeField] private float freeCamDragThresholdPixels = 8f; // same idea as GridInputHandler's own threshold - short of this a press is still just a press, not yet a pan
        [SerializeField] private float scrollZoomSpeed = 0.01f; // orthographicSize change per Mouse.scroll.y unit - tune in the Inspector, scroll units vary by platform
        [SerializeField] private float pinchZoomSpeed = 0.01f; // orthographicSize change per pixel of two-finger distance change
        [SerializeField] private float minFreeOrthographicSize = 0.3f;
        [SerializeField] private float maxFreeOrthographicSize = 50f;

        /// A camera pose expressed as the point it FRAMES plus how far
        /// back it sits from that point along its own forward axis -
        /// deliberately not as a raw world position.
        ///
        /// Under orthographic projection the distance is invisible: only
        /// Focus, Rotation and OrthographicSize decide what ends up on
        /// screen. Splitting the two apart is what lets the transition
        /// interpolate the framing on its own while each pose keeps its
        /// own camera distance - see LerpPose, which rebuilds the position
        /// from the CURRENT rotation every frame.
        private readonly struct CameraPose
        {
            public readonly Vector3 Focus;
            public readonly Quaternion Rotation;
            public readonly float OrthographicSize;
            public readonly float Distance;

            public CameraPose(Vector3 focus, Quaternion rotation, float orthographicSize, float distance)
            {
                Focus = focus;
                Rotation = rotation;
                OrthographicSize = orthographicSize;
                Distance = distance;
            }
        }

        private Camera cam;
        private bool wasPlaying;
        private Coroutine transitionRoutine;
        private Vector3 followVelocity;
        private float followSizeVelocity;

        // Where the camera is currently pointed, and how far back it sits
        // from there. Kept as state rather than read back off the
        // transform, because a position alone can't say which point along
        // the forward axis the camera means to frame - and that is exactly
        // what a transition has to start from (see CurrentPose).
        private Vector3 focus;
        private float focusDistance;
        private bool focusKnown;

        private bool isFreeCamera;
        private bool mousePressed;
        private bool mouseDragging;
        private Vector2 mousePressScreenPos;
        private Vector2 mouseLastScreenPos;
        private bool touchDragging;
        private Vector2 touchPressScreenPos;
        private Vector2 touchLastScreenPos;
        private float pinchLastDistance = -1f;

        public bool IsFreeCamera => isFreeCamera;

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
                ReturnToGuidedCamera(); // entering or leaving simulation always drops any free-cam pan/zoom from the previous run
                StartTransitionTo(isPlaying);
                return;
            }

            // Once the one-shot entry transition into the isometric view
            // has settled, keep gently re-centering on the marble every
            // frame instead of staying fixed on the whole-track pose it
            // ended on - unless the player has taken the camera into free
            // mode (see 0044), in which case their pan/zoom input drives
            // it instead.
            if (!isPlaying || transitionRoutine != null) return;

            HandleFreeCameraInput();
            if (!isFreeCamera) FollowMarble();
        }

        private void StartTransitionTo(bool playing)
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            if (playing) followVelocity = Vector3.zero; // fresh follow, no leftover SmoothDamp momentum from a previous run

            CameraPose from = CurrentPose();
            CameraPose to = playing ? ComputeIsometricPose(from) : GetPlanPose(from);

            transitionRoutine = StartCoroutine(LerpPose(from, to));
        }

        /// Hands control back from free camera to FollowMarble - no
        /// snapping, FollowMarble's own SmoothDamp (see below) eases the
        /// camera from wherever the player left it back onto the marble.
        public void ReturnToGuidedCamera()
        {
            isFreeCamera = false;
            mousePressed = false;
            mouseDragging = false;
            touchDragging = false;
            pinchLastDistance = -1f;
            followVelocity = Vector3.zero;
            followSizeVelocity = 0f;
        }

        /// The 2D planning pose, straight from CameraFitter - including
        /// the camera distance the scene authored for it, which is left
        /// exactly as it was: the 2D view has never had a clipping problem
        /// to solve, and GridInputHandler raycasts from this camera.
        private CameraPose GetPlanPose(CameraPose fallback)
        {
            if (cameraFitter == null || !cameraFitter.TryComputeFitPose(out Vector3 position, out Quaternion rotation, out float orthographicSize, out Vector3 planFocus))
                return fallback;

            float distance = Vector3.Dot(planFocus - position, rotation * Vector3.forward);
            return new CameraPose(planFocus, rotation, orthographicSize, distance);
        }

        /// The pose the camera is in right now - the starting point of
        /// every transition. Reads the framed point from the tracked
        /// state, falling back to the 2D planning pose the very first time
        /// (nothing has moved the camera off it yet at that point).
        private CameraPose CurrentPose()
        {
            if (!focusKnown)
            {
                CameraPose plan = GetPlanPose(new CameraPose(transform.position, transform.rotation, cam.orthographicSize, 0f));
                focus = plan.Focus;
                focusDistance = plan.Distance;
                focusKnown = true;
            }

            return new CameraPose(focus, transform.rotation, cam.orthographicSize, focusDistance);
        }

        /// The single place the camera transform is written from a pose -
        /// keeps the tracked framing state and the transform from ever
        /// drifting apart.
        private void ApplyPose(Vector3 newFocus, Quaternion rotation, float distance, float orthographicSize)
        {
            focus = newFocus;
            focusDistance = distance;
            focusKnown = true;

            transform.SetPositionAndRotation(newFocus - (rotation * Vector3.forward) * distance, rotation);
            cam.orthographicSize = orthographicSize;
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
            BoundsCameraMath.Extents extents = BoundsCameraMath.MeasureExtents(bounds, rotation);

            float orthographicSize = Mathf.Max(extents.Up, extents.Right / cam.aspect) + padding;
            float distance = extents.Forward + forwardClearance;

            WidenClipRangeFor(extents.Forward);

            return new CameraPose(bounds.center, rotation, orthographicSize, distance);
        }

        /// Pushes the far clip plane out far enough that forwardClearance
        /// can never move geometry out the BACK of the view slab - moving
        /// the camera away from the track only helps if what it gains at
        /// the near plane isn't lost at the far one.
        ///
        /// forwardExtent is the track's half-depth along the camera's
        /// forward axis, so the farthest point of it sits at
        /// forwardClearance + 2 * forwardExtent; the doubling on top is
        /// slack for FollowMarble, which frames a single marble rather
        /// than the whole track and can therefore sit closer to one end of
        /// it. Only ever widens the authored value, never narrows it.
        private void WidenClipRangeFor(float forwardExtent)
        {
            float required = forwardClearance + 4f * forwardExtent + 1f;
            if (cam.farClipPlane < required) cam.farClipPlane = required;
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

            // Smoothed on the framed point rather than on the camera
            // position, for the same reason LerpPose is - the rotation is
            // fixed here, so the two are equivalent in practice, but it
            // keeps the tracked framing state honest.
            Vector3 newFocus = Vector3.SmoothDamp(focus, target.position, ref followVelocity, followSmoothTime);
            float size = Mathf.SmoothDamp(cam.orthographicSize, followOrthographicSize, ref followSizeVelocity, followSmoothTime);

            ApplyPose(newFocus, transform.rotation, forwardClearance, size);
        }

        /// Only called once simulating and the entry transition has
        /// settled (see Update) - GridInputHandler already disables itself
        /// entirely while playing, so a left-click/touch drag here can
        /// never steal a gesture from pipe editing.
        private void HandleFreeCameraInput()
        {
            HandleMousePan();
            HandleTouchPan();
            HandleScrollZoom();
            HandlePinchZoom();
        }

        private void HandleMousePan()
        {
            if (Mouse.current == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                mousePressed = true;
                mouseDragging = false;
                mousePressScreenPos = Mouse.current.position.ReadValue();
                return;
            }

            if (!mousePressed) return;

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                mousePressed = false;
                mouseDragging = false;
                return;
            }

            Vector2 currentScreenPos = Mouse.current.position.ReadValue();

            if (!mouseDragging)
            {
                if ((currentScreenPos - mousePressScreenPos).sqrMagnitude < freeCamDragThresholdPixels * freeCamDragThresholdPixels) return;
                mouseDragging = true;
                mouseLastScreenPos = currentScreenPos; // start panning from here, so the drag doesn't jump by the threshold distance on the first moved frame
            }

            ApplyPan(currentScreenPos - mouseLastScreenPos);
            mouseLastScreenPos = currentScreenPos;
        }

        /// Single-finger drag pans, mirroring HandleMousePan - only acts
        /// while exactly one finger is down, so a second finger joining in
        /// hands off to HandlePinchZoom instead of the two fighting over
        /// the same gesture.
        private void HandleTouchPan()
        {
            if (Touchscreen.current == null) return;

            TouchControl primary = Touchscreen.current.primaryTouch;
            if (ActiveTouchCount() != 1 || !primary.press.isPressed)
            {
                touchDragging = false;
                return;
            }

            Vector2 currentScreenPos = primary.position.ReadValue();

            if (primary.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                touchDragging = false;
                touchPressScreenPos = currentScreenPos;
                return;
            }

            if (!touchDragging)
            {
                if ((currentScreenPos - touchPressScreenPos).sqrMagnitude < freeCamDragThresholdPixels * freeCamDragThresholdPixels) return;
                touchDragging = true;
                touchLastScreenPos = currentScreenPos;
            }

            ApplyPan(currentScreenPos - touchLastScreenPos);
            touchLastScreenPos = currentScreenPos;
        }

        private void HandleScrollZoom()
        {
            if (Mouse.current == null) return;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f)) return;

            // Scrolling "up" (positive) zooms in, so it shrinks orthographicSize.
            ApplyZoomDelta(-scroll * scrollZoomSpeed);
        }

        /// Distance between the first two pressed fingers, frame over
        /// frame: fingers spreading apart zooms in (shrinks
        /// orthographicSize), pinching together zooms out.
        private void HandlePinchZoom()
        {
            if (Touchscreen.current == null || ActiveTouchCount() < 2)
            {
                pinchLastDistance = -1f;
                return;
            }

            TouchControl first = null, second = null;
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (!touch.press.isPressed) continue;
                if (first == null) first = touch;
                else if (second == null) { second = touch; break; }
            }

            if (first == null || second == null)
            {
                pinchLastDistance = -1f;
                return;
            }

            float distance = Vector2.Distance(first.position.ReadValue(), second.position.ReadValue());
            if (pinchLastDistance < 0f)
            {
                pinchLastDistance = distance; // first frame of the pinch - nothing to compare against yet
                return;
            }

            float delta = distance - pinchLastDistance;
            pinchLastDistance = distance;
            if (Mathf.Approximately(delta, 0f)) return;

            ApplyZoomDelta(-delta * pinchZoomSpeed);
        }

        private int ActiveTouchCount()
        {
            if (Touchscreen.current == null) return 0;

            int count = 0;
            foreach (TouchControl touch in Touchscreen.current.touches)
                if (touch.press.isPressed) count++;
            return count;
        }

        /// Moves the camera along its own right/up axes only - the fixed
        /// isometric rotation is never touched - by exactly the world
        /// distance the given screen-space delta covers at the camera's
        /// current orthographic size, so the content under the cursor/
        /// finger tracks the drag 1:1.
        private void ApplyPan(Vector2 screenDelta)
        {
            if (screenDelta == Vector2.zero) return;
            isFreeCamera = true;

            float worldPerPixel = (cam.orthographicSize * 2f) / Mathf.Max(1f, cam.pixelHeight);
            Vector3 right = transform.rotation * Vector3.right;
            Vector3 up = transform.rotation * Vector3.up;

            // Purely sideways/up - never along forward - so the camera
            // distance is untouched and the framed point simply travels
            // with the camera.
            ApplyPose(focus - (right * screenDelta.x + up * screenDelta.y) * worldPerPixel, transform.rotation, focusDistance, cam.orthographicSize);
        }

        private void ApplyZoomDelta(float delta)
        {
            isFreeCamera = true;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + delta, minFreeOrthographicSize, maxFreeOrthographicSize);
        }

        /// Interpolates the FRAMING - the point on screen, the rotation
        /// and the zoom - and rebuilds the camera position from that every
        /// frame, rather than lerping the two end positions directly.
        ///
        /// That distinction is what keeps forwardClearance invisible.
        /// Position and rotation interpolate independently, so a raw
        /// position lerp puts the camera at the average of two points that
        /// each only make sense under their own rotation. The error is
        /// proportional to the camera distance: at the old distance of a
        /// couple of units it was a barely visible wobble, but it scales
        /// with the clearance, and at a distance chosen large enough to
        /// rule out near-plane clipping it would sling the track clean off
        /// screen mid-transition. Anchoring on the framed point instead
        /// makes the whole thing independent of how far back the camera
        /// sits - which is what an orthographic projection should do.
        private IEnumerator LerpPose(CameraPose from, CameraPose to)
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

                ApplyPose(
                    Vector3.Lerp(from.Focus, to.Focus, t),
                    Quaternion.Slerp(from.Rotation, to.Rotation, t),
                    Mathf.Lerp(from.Distance, to.Distance, t),
                    Mathf.Lerp(from.OrthographicSize, to.OrthographicSize, t));

                yield return null;
            }

            ApplyPose(to.Focus, to.Rotation, to.Distance, to.OrthographicSize);
            transitionRoutine = null;
        }
    }
}
