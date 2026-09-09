namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Implemented by CellContentDefinition subclasses that turn a block
    /// into BlockType.Trigger (fall height + pad geometry, see 0039) - e.g.
    /// SoundTriggerContent, XylophonePadContent. TrackBlockSpawner only
    /// checks for this interface, never a concrete type, so future trigger
    /// contents (drum, etc. - see 0025) fall into place without spawner
    /// changes.
    /// </summary>
    public interface ITriggerCellContent
    {
        float FallHeight { get; }
    }
}
