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
    /// plus values it already computes (type, directions, height) - this is
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
        public Direction InputDirection; // direction the marble arrives from; None at Start
        public Direction OutputDirection; // direction the marble leaves in; None at Goal
        public float Height; // world Y of the groove floor at this block's entry point
        public BlockType Type; // Start/Goal/Normal/Trigger - see 0039, independent of PipeRole
        public float FallHeight; // > 0 only for Trigger blocks - see 0039
        public float SurfaceInclination; // this block's own TrackBlock.TiltDegrees, in degrees
        public TriggerBehavior Trigger;
        public AudioClip AudioEvent; // pragmatic first pass - see 0026 notes on a future string instrumentId
        public string Biome; // placeholder - no biome system exists yet, always DefaultBiome today
        public Color FlashColor; // see BlockFlashFeedback (0023) - defaults to Color.white when no content overrides it

        public BlockDefinition(Vector2Int coord, Direction inputDirection, Direction outputDirection, float height,
            BlockType type, float fallHeight, float surfaceInclination, TriggerBehavior trigger, AudioClip audioEvent,
            string biome, Color flashColor)
        {
            Coord = coord;
            InputDirection = inputDirection;
            OutputDirection = outputDirection;
            Height = height;
            Type = type;
            FallHeight = fallHeight;
            SurfaceInclination = surfaceInclination;
            Trigger = trigger;
            AudioEvent = audioEvent;
            Biome = biome;
            FlashColor = flashColor;
        }
    }

    public enum TriggerBehavior
    {
        None,
        OnEnter
    }
}
