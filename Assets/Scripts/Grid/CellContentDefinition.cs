using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Base type for content permanently bound to a grid cell, independent of
    /// whichever PipeDefinition currently occupies that cell after swaps.
    /// Subclass to add new content (checkpoints, FX, obstacles...) without
    /// touching LevelData, PathGrid, or the level grid editor. Pure data -
    /// TrackBlockSpawner reads subclasses' fields into a spawned block's
    /// BlockDefinition (see 0027); reacting to the marble arriving happens
    /// block-side (BlockTrigger and its sibling components, see 0023),
    /// not here.
    /// </summary>
    public abstract class CellContentDefinition : ScriptableObject
    {
        [SerializeField] private string contentId = "Content";
        [SerializeField] private string label = "?";
        public string ContentId => contentId;
        public string Label => label;
    }
}
