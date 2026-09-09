namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// What a TrackBlock IS, geometrically/behaviorally (track-block level) -
    /// independent of PipeRole (grid/pipe level, describes the pipe placed
    /// in the 2D level editor: Normal/Start/Goal). TrackBlockSpawner derives
    /// exactly one BlockType per spawned block from its position in the
    /// path (Start/Goal) and its CellContentDefinition (Trigger), falling
    /// back to Normal - see 0039.
    /// </summary>
    public enum BlockType
    {
        Start,
        Goal,
        Normal,
        Trigger
    }
}
