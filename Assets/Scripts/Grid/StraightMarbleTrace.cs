using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// The plain case (see IMarbleTrace/0038): the marble rolls in a
    /// straight line, at a steady pace, from one local point to another
    /// over this block's own beat. Used for every straight-through block,
    /// and - with a shortened range - for Start/Goal, whose groove is
    /// sealed part way through (see ClosedEndGrooveBlockProfile.WallZ) so
    /// the marble appears at / vanishes into that wall inside the tunnel
    /// portal's mouth instead of at the block's own outer edge. Those two
    /// cover less ground in their beat than a full block, i.e. the marble
    /// is simply slower there - the beat itself stays the same.
    /// </summary>
    public class StraightMarbleTrace : IMarbleTrace
    {
        private readonly Vector3 from;
        private readonly Vector3 to;

        public StraightMarbleTrace(Vector3 fromLocal, Vector3 toLocal)
        {
            from = fromLocal;
            to = toLocal;
        }

        /// Trace along this block's own groove floor between two local Z
        /// positions - the usual way to build one, since a block's own
        /// rollable line is fully described by its Entry/ExitPointLocal.
        public static StraightMarbleTrace BetweenZ(TrackBlock block, float fromZ, float toZ)
        {
            return new StraightMarbleTrace(GrooveFloorAtZ(block, fromZ), GrooveFloorAtZ(block, toZ));
        }

        /// Point on this block's own groove floor at local Z - i.e. its
        /// Entry-to-Exit line, which already carries the block's own tilt
        /// (see TrackBlock.EntryPointLocal). Shared with
        /// TriggerFallMarbleTrace, which needs the same line for the part
        /// of its own trace that actually rolls.
        public static Vector3 GrooveFloorAtZ(TrackBlock block, float z)
        {
            Vector3 entry = block.EntryPointLocal;
            Vector3 exit = block.ExitPointLocal;
            return Vector3.Lerp(entry, exit, Mathf.InverseLerp(entry.z, exit.z, z));
        }

        public float ImpactT => 0f;

        public Vector3 SampleLocal(float t) => Vector3.Lerp(from, to, Mathf.Clamp01(t));
    }
}
