using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Everything a content definition needs to react to the marble arriving
    /// at its cell, without needing to know about PathPipe/PipeDefinition at all.
    /// </summary>
    public readonly struct CellContentContext
    {
        public readonly PathGrid Grid;
        public readonly Vector2Int Coord;
        public readonly AudioSource AudioSource;
        public readonly Marble Marble;

        public CellContentContext(PathGrid grid, Vector2Int coord, AudioSource audioSource, Marble marble)
        {
            Grid = grid;
            Coord = coord;
            AudioSource = audioSource;
            Marble = marble;
        }
    }

    /// <summary>
    /// Base type for content permanently bound to a grid cell, independent of
    /// whichever PipeDefinition currently occupies that cell after swaps.
    /// Subclass to add new content (checkpoints, FX, obstacles...) without
    /// touching LevelData, PathGrid, or the level grid editor.
    /// </summary>
    public abstract class CellContentDefinition : ScriptableObject
    {
        [SerializeField] private string contentId = "Content";
        [SerializeField] private string label = "?";
        public string ContentId => contentId;
        public string Label => label;

        /// Called when the marble arrives at (or spawns on) this cell.
        public abstract void Activate(CellContentContext context);
    }
}
