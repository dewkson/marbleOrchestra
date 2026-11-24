using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Animates the 2D planning view from one SubLevel to another: the
    /// camera glides from the old SubLevel's framing (CameraFitter) over to
    /// the new one's while the old SubLevel's cards shrink away and the new
    /// one's pop in - both as a wave along the direction of travel, so the
    /// cards appear in order of their position. At the end only the new
    /// SubLevel's grid is left, exactly as PathGrid's normal visibility has
    /// it. MarbleController adds and drives this; editing is locked while
    /// IsAnimating (see GridInputHandler).
    /// </summary>
    public class SubLevelSwitchAnimator : MonoBehaviour
    {
        [SerializeField] private float duration = 1.1f;      // camera glide = whole animation
        [SerializeField] private float cellPopDuration = 0.35f; // per card, inside the glide

        private struct Entry
        {
            public Transform Visual;
            public Vector3 Scale;
            public float Delay;
            public bool Appearing;
        }

        private PathGrid grid;
        private CameraFitter fitter;
        private Camera cam;
        private Coroutine routine;
        private readonly List<Entry> entries = new List<Entry>();

        public bool IsAnimating => routine != null;

        public void Init(PathGrid pathGrid, CameraFitter cameraFitter)
        {
            grid = pathGrid;
            fitter = cameraFitter;
            cam = fitter != null ? fitter.GetComponent<Camera>() : null;
        }

        /// The grid must already have switched to `to` (PathGrid.GoToSubLevel).
        public void Play(int from, int to)
        {
            FinishNow();

            if (grid == null || cam == null || !fitter.TryComputeFitPose(out Vector3 targetPos, out Quaternion targetRot, out float targetSize, out _))
            {
                fitter?.Fit();
                return;
            }

            routine = StartCoroutine(Run(from, to, targetPos, targetRot, targetSize));
        }

        /// Jumps straight to the end state (e.g. Play pressed mid-switch).
        public void FinishNow()
        {
            if (routine == null) return;
            StopCoroutine(routine);
            Complete();
        }

        private IEnumerator Run(int from, int to, Vector3 targetPos, Quaternion targetRot, float targetSize)
        {
            Vector3 startPos = cam.transform.position;
            Quaternion startRot = cam.transform.rotation;
            float startSize = cam.orthographicSize;

            grid.ShowSubLevelVisuals(from); // the old SubLevel stays visible while it shrinks away
            BuildEntries(from, to);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                cam.transform.SetPositionAndRotation(Vector3.Lerp(startPos, targetPos, t), Quaternion.Slerp(startRot, targetRot, t));
                cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);

                foreach (Entry entry in entries)
                {
                    if (entry.Visual == null) continue;
                    float local = Mathf.Clamp01((elapsed - entry.Delay) / cellPopDuration);
                    float factor = entry.Appearing ? EaseOutBack(local) : 1f - local * local;
                    entry.Visual.localScale = entry.Scale * factor;
                }

                yield return null;
            }

            Complete();
        }

        /// Both waves run along the direction from the old SubLevel's center
        /// to the new one's: the old cards nearest the departure side vanish
        /// first, the new cards nearest the arrival side appear first.
        private void BuildEntries(int from, int to)
        {
            entries.Clear();

            RectInt fromArea = grid.GetSubLevelArea(from);
            RectInt toArea = grid.GetSubLevelArea(to);
            Vector2 direction = toArea.center - fromArea.center;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
            direction.Normalize();

            float spread = Mathf.Max(0f, duration - cellPopDuration);
            AddEntries(from, false, direction, spread);
            AddEntries(to, true, direction, spread);
        }

        private void AddEntries(int subLevel, bool appearing, Vector2 direction, float spread)
        {
            List<KeyValuePair<Vector2Int, Transform>> visuals = new List<KeyValuePair<Vector2Int, Transform>>();
            grid.CollectSubLevelVisuals(subLevel, visuals);
            if (visuals.Count == 0) return;

            float min = float.MaxValue, max = float.MinValue;
            foreach (KeyValuePair<Vector2Int, Transform> visual in visuals)
            {
                float p = Vector2.Dot(visual.Key, direction);
                min = Mathf.Min(min, p);
                max = Mathf.Max(max, p);
            }

            foreach (KeyValuePair<Vector2Int, Transform> visual in visuals)
            {
                float p = Vector2.Dot(visual.Key, direction);
                float order = max - min > 0.0001f ? (p - min) / (max - min) : 0f;
                Entry entry = new Entry
                {
                    Visual = visual.Value,
                    Scale = visual.Value.localScale,
                    Delay = order * spread,
                    Appearing = appearing,
                };
                if (appearing) entry.Visual.localScale = Vector3.zero;
                entries.Add(entry);
            }
        }

        private void Complete()
        {
            foreach (Entry entry in entries)
            {
                if (entry.Visual != null) entry.Visual.localScale = entry.Scale;
            }
            entries.Clear();
            routine = null;

            grid?.RefreshSubLevelVisibility();
            fitter?.Fit();
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float m = x - 1f;
            return 1f + c3 * m * m * m + c1 * m * m;
        }
    }
}
