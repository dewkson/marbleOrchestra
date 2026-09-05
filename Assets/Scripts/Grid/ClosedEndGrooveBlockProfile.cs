namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// GrooveBlockProfile for Start/Goal blocks: identical groove shape,
    /// but flagged (via IClosedEndBlockProfile) so TrackBlock seals it with
    /// an internal wall on the side that doesn't connect to a neighboring
    /// block - see 0022.
    /// </summary>
    public class ClosedEndGrooveBlockProfile : GrooveBlockProfile, IClosedEndBlockProfile
    {
        public bool ClosedAtEntry { get; }
        public float WallZ { get; }

        /// railExtension: how far past the block's center (into the closed
        /// half) the groove keeps going before the wall seals it - 0 means
        /// exactly at the center. TrackBlockSpawner sets this to reach just
        /// past where a TunnelPortalDecoration's dark interior starts, so
        /// the groove visibly runs into the tunnel instead of dead-ending
        /// right at its mouth.
        public ClosedEndGrooveBlockProfile(float grooveRadius, float sideWidth, int arcSegments, bool closedAtEntry, float railExtension = 0f)
            : base(grooveRadius, sideWidth, arcSegments)
        {
            ClosedAtEntry = closedAtEntry;
            WallZ = closedAtEntry ? -railExtension : railExtension;
        }
    }
}
