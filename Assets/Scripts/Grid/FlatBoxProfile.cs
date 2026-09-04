using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Default TrackBlock profile: a flat plank spanning the block's full
    /// width, no groove. Used by generic/gameplay/instrument blocks that
    /// don't need a marble rail.
    /// </summary>
    public class FlatBoxProfile : IBlockProfile
    {
        public static readonly FlatBoxProfile Instance = new FlatBoxProfile();

        public Vector2[] BuildCrossSection(Vector2 size)
        {
            float halfWidth = size.x * 0.5f;
            return new[] { new Vector2(-halfWidth, 0f), new Vector2(halfWidth, 0f) };
        }

        public Vector3 EntryPoint(Vector2 size) => new Vector3(0f, 0f, -size.y * 0.5f);

        public Vector3 ExitPoint(Vector2 size) => new Vector3(0f, 0f, size.y * 0.5f);

        public bool IsGrooveSegment(int segmentIndex, Vector2 size) => false; // no groove - a flat plank
    }
}
