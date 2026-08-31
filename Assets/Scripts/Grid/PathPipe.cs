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

        private void Awake()
        {
            visual = GetComponent<PipeVisual>();
        }

        public void Initialize(PipeDefinition definition, Vector2Int coord)
        {
            Definition = definition;
            Coord = coord;
            visual.Refresh(definition);
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
