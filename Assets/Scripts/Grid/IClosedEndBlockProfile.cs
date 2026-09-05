namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Optional IBlockProfile extension for a block whose groove dead-ends
    /// part way through instead of running all the way to one of its edges
    /// - used by Start (open towards its exit, closed towards its entry -
    /// see 0022) and Goal (open towards its entry, closed towards its
    /// exit). TrackBlock.Rebuild() checks for this interface and, when
    /// present, seals the groove with an extra internal wall at local
    /// Z = WallZ, reusing the same cross-section/skirt logic as the
    /// block's own outer end caps. The groove's own shape is otherwise
    /// completely unchanged - a Start/Goal block looks exactly like a
    /// normal block except for this one capped-off portion.
    /// </summary>
    public interface IClosedEndBlockProfile
    {
        /// True: seal the entry side (local Z = -size.y/2), leaving the
        /// exit half open (Start). False: seal the exit side instead,
        /// leaving the entry half open (Goal).
        bool ClosedAtEntry { get; }

        /// Local Z (same coordinate space as TrackBlock's own
        /// -size.y/2..+size.y/2) where the groove/flat transition sits.
        /// Normally a bit past the block's center, into the closed half -
        /// see ClosedEndGrooveBlockProfile's railExtension - so the groove
        /// visibly reaches into a nearby tunnel-portal decoration (see
        /// TunnelPortalDecoration) instead of dead-ending right at its
        /// frame, which would leave a visible strip of plain ground
        /// showing through the tunnel mouth.
        float WallZ { get; }
    }
}
