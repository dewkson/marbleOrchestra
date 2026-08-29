using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// TrackBlock profile for the marble rail: a semicircular U groove
    /// between flat shoulders (side view: ---u---), built via
    /// GrooveProfileUtility. The groove's own radius/shoulder width define
    /// the block's true width, independent of TrackBlock.Size.x - callers
    /// (TrackBlockSpawner) should keep Size.x consistent with these so the
    /// declared footprint matches what's actually rendered.
    /// </summary>
    public class GrooveBlockProfile : IBlockProfile
    {
        private readonly float grooveRadius;
        private readonly float sideWidth;
        private readonly int arcSegments;

        public GrooveBlockProfile(float grooveRadius, float sideWidth, int arcSegments)
        {
            this.grooveRadius = grooveRadius;
            this.sideWidth = sideWidth;
            this.arcSegments = arcSegments;
        }

        public Vector2[] BuildCrossSection(Vector2 size)
        {
            return GrooveProfileUtility.BuildProfile(grooveRadius, sideWidth, arcSegments);
        }

        /// The groove's floor (its lowest point, where a marble rests).
        public Vector3 EntryPoint(Vector2 size) => new Vector3(0f, -grooveRadius, -size.y * 0.5f);

        public Vector3 ExitPoint(Vector2 size) => new Vector3(0f, -grooveRadius, size.y * 0.5f);
    }
}
