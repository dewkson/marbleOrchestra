using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Defines the marble's vertical trajectory through one TrackBlock in
    /// Kinematic3D mode (see MarbleController.RunAlongPath3D), on top of the
    /// plain linear rise/fall between the block's own EntryPointLocal.y and
    /// ExitPointLocal.y that TrackBlockSpawner.SampleFloorY otherwise uses.
    /// Swappable per block instance - see IBlockProfile for the analogous
    /// extension point on the mesh's surface shape - so different block
    /// variants can move the marble differently, e.g. a plain groove stays
    /// linear while a future "hop" block variant (see 0022) arcs the marble
    /// up and back down to simulate a jump, purely by shaping this offset -
    /// no physics engine involved.
    /// </summary>
    public interface IBlockMotionTrace
    {
        /// Vertical offset (world-up, added on top of the plain
        /// entry-to-exit height lerp) at progress f (0 = entry, 1 = exit)
        /// through the block. f advances at the track's constant per-block
        /// real-time pace (unchanged), so a nonzero, non-linear offset here
        /// both bends the marble's path and - since f still advances
        /// uniformly - changes its apparent vertical speed as a side effect
        /// (e.g. steep near the ends, flattened at a jump's apex), together
        /// simulating a jump without a separate speed parameter.
        /// Must return 0 at f=0 and f=1 so blocks stay chained exactly at
        /// their entry/exit height - only the interior of the block may
        /// deviate.
        float SampleOffset(float f);
    }
}
