using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Runtime instance of a card placed in a grid cell. Holds gameplay data
    /// (which CardDefinition, which coordinate) and forwards rendering to CardVisual.
    /// </summary>
    [RequireComponent(typeof(CardVisual))]
    public class PathCard : MonoBehaviour
    {
        public Vector2Int Coord { get; private set; }
        public CardDefinition Definition { get; private set; }

        private CardVisual visual;

        private void Awake()
        {
            visual = GetComponent<CardVisual>();
        }

        public void Initialize(CardDefinition definition, Vector2Int coord)
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
    }
}
