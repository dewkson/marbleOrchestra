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

        /// BuildProfile lays points out as: [0] left shoulder corner,
        /// [1..arcSegments+1] the arc sweeping the U itself, [arcSegments+2]
        /// right shoulder corner - so segments 1..arcSegments (connecting
        /// consecutive arc points) are the groove; segment 0 and the last
        /// segment are the flat shoulders on either side of it.
        public bool IsGrooveSegment(int segmentIndex, Vector2 size) => segmentIndex >= 1 && segmentIndex <= arcSegments;
    }
}
