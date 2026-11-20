using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Runtime instance of a pipe placed in a grid cell. Holds gameplay data
    /// (which PipeDefinition, which coordinate) and forwards rendering to PipeVisual.
    /// </summary>
    [RequireComponent(typeof(PipeVisual))]
    public class PathPipe : MonoBehaviour
    {
        public Vector2Int Coord { get; private set; }
        public PipeDefinition Definition { get; private set; }
        public PipeRole Role => Definition != null ? Definition.Role : PipeRole.Normal;
        public bool IsLocked => Definition != null && Definition.Locked;

        private PipeVisual visual;

        private bool sliding;
        private Vector3 slideFrom;
        private Vector3 slideTo;
        private float slideElapsed;
        private float slideDuration;

        private void Awake()
        {
            visual = GetComponent<PipeVisual>();
        }

        /// Glides this card from wherever it currently is (its old cell, or
        /// wherever a drag dropped it) to `localPosition` - the 15-Puzzle
        /// slide animation (see 0067). Coord is already updated by the
        /// caller, so gameplay never waits on the animation.
        public void SlideTo(Vector3 localPosition, float duration)
        {
            if (duration <= 0f)
            {
                sliding = false;
                transform.localPosition = localPosition;
                return;
            }

            sliding = true;
            slideFrom = transform.localPosition;
            slideTo = localPosition;
            slideElapsed = 0f;
            slideDuration = duration;
        }

        private void Update()
        {
            if (!sliding) return;

            slideElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(slideElapsed / slideDuration);
            transform.localPosition = Vector3.Lerp(slideFrom, slideTo, Mathf.SmoothStep(0f, 1f, t));
            if (t >= 1f) sliding = false;
        }

        public void Initialize(PipeDefinition definition, Vector2Int coord, LevelData levelStyle)
        {
            Definition = definition;
            Coord = coord;
            visual.Refresh(definition, levelStyle);
        }

        public void SetCoord(Vector2Int coord)
        {
            Coord = coord;
        }

        public void SetSelected(bool selected)
        {
            visual.SetHighlighted(selected);
        }

        /// See PipeVisual.SetDragElevated - visually raises this pipe
        /// above every other one while it's being dragged (see 0004).
        public void SetDragElevated(bool elevated)
        {
            visual.SetDragElevated(elevated);
        }

        public void SetConnectivity(CellConnectivity connectivity)
        {
            visual.SetConnectivity(connectivity);
        }
    }
}
