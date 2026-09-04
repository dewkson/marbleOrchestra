using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Defines a TrackBlock's rollable top-surface shape. Swapping the
    /// profile (not the component type) is how the one universal TrackBlock
    /// prefab serves different purposes (flat generic block, grooved track
    /// segment, ...) - a Unity prefab can't swap a component's type per
    /// instance, so this is the extension point instead of subclassing.
    /// </summary>
    public interface IBlockProfile
    {
        /// 2D cross-section (x = lateral offset, y = vertical offset from
        /// the block's top reference plane), ordered left to right, swept
        /// from the block's entry edge to its exit edge.
        Vector2[] BuildCrossSection(Vector2 size);

        /// Local-space point on the rollable surface at the entry edge
        /// (local Z = -size.y/2), used to chain this block to the previous
        /// one's exit point.
        Vector3 EntryPoint(Vector2 size);

        /// Local-space point on the rollable surface at the exit edge
        /// (local Z = +size.y/2), used to chain this block to the next
        /// one's entry point.
        Vector3 ExitPoint(Vector2 size);

        /// Whether the top-surface segment between BuildCrossSection's
        /// points[segmentIndex] and points[segmentIndex + 1] is part of the
        /// rollable groove itself, as opposed to a flat shoulder - lets
        /// TrackBlock render the groove with its own material (a different
        /// color from the shoulders, e.g. earthy brown vs. grass - see
        /// 0032) without needing to know the profile's own geometry.
        bool IsGrooveSegment(int segmentIndex, Vector2 size);
    }
}
