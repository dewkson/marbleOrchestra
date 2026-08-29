using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Describes WHAT a block is: grid- and content-derived facts about one
    /// path cell, independent of how it's rendered (TrackBlock, see 0018)
    /// and independent of what it musically triggers (a future Music-
    /// System, see 0026). Assembled by TrackBlockSpawner per spawned block
    /// from already-authored data (PipeDefinition.Role via
    /// PathGrid.GetPipe, CellContentDefinition via PathGrid.GetContent)
    /// plus values it already computes (direction, height) - this is
    /// deliberately NOT a new authoring primitive; no new ScriptableObject,
    /// no LevelData/level-editor changes.
    /// </summary>
    /// Not a `readonly struct`/`readonly` fields on purpose, even though
    /// the data is conceptually immutable once assembled: Unity's
    /// serializer does not serialize `readonly` fields at all, which would
    /// make this invisible in the Inspector (TrackBlock exposes it through
    /// a get-only property + [SerializeField] backing field instead, so
    /// external code still can't mutate it through a live block reference).
    [System.Serializable]
    public struct BlockDefinition
    {
        public const string DefaultBiome = "Default";

        public Vector2Int Coord;
        public Direction PathDirection;
        public float Height;
        public PipeRole Type; // reuses the existing Normal/Start/Goal taxonomy
        public TriggerBehavior Trigger;
        public AudioClip AudioEvent; // pragmatic first pass - see 0026 notes on a future string instrumentId
        public string Biome; // placeholder - no biome system exists yet, always DefaultBiome today

        public BlockDefinition(Vector2Int coord, Direction pathDirection, float height, PipeRole type,
            TriggerBehavior trigger, AudioClip audioEvent, string biome)
        {
            Coord = coord;
            PathDirection = pathDirection;
            Height = height;
            Type = type;
            Trigger = trigger;
            AudioEvent = audioEvent;
            Biome = biome;
        }
    }

    public enum TriggerBehavior
    {
        None,
        OnEnter
    }
}
