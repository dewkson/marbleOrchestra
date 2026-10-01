using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Trace for a Trigger block (see BlockType.Trigger/0039): the marble
    /// doesn't roll through this block at all, it ARRIVES on it - in three
    /// phases (see 0038/0041), all in the block's own local space and all
    /// inside this block's own single beat (see IMarbleTrace):
    /// 1. FALL - it leaves the previous block's exit edge and drops onto
    ///    the top of the pad bar (XylophoneBlockDecoration) on an
    ///    accelerating arc, so the drop reads as a real fall instead of a
    ///    straight diagonal glide. Its landing is this trace's ImpactT:
    ///    the block's sound fires exactly when the marble is seen hitting
    ///    the bar.
    /// 2. BOUNCE - a short hop off the bar that carries it on into the
    ///    mouth of the groove.
    /// 3. ROLL - it rolls out through the groove in the block's own
    ///    OutputDirection, like any other block - following the block's
    ///    real groove centerline (TrackBlock.SampleGroovePointLocal), so a
    ///    Trigger block whose path turns 90° (see 0052 follow-up) rolls
    ///    out along that curve exactly like a turning Normal block does,
    ///    instead of cutting straight across it.
    /// TIMING is what makes this musical rather than physical: the fall
    /// always takes the SAME fraction of the beat (fallBeatFraction),
    /// whatever the block's FallHeight - a deeper fall simply falls
    /// faster. Every Trigger block therefore sounds at the same phase
    /// inside its own beat, and since only Trigger blocks make a sound at
    /// all, every interval between two notes stays a whole number of
    /// beats. Deriving the fall's duration from real gravity instead
    /// (an earlier version) made each note's offset depend on its own
    /// FallHeight, i.e. audibly off the grid.
    /// The remaining beat is split between bounce and roll by the ground
    /// they cover, so the marble keeps an even pace after landing.
    /// The pad's own geometry (height, offset) is read straight from
    /// XylophoneBlockDecoration, so the marble can never land somewhere
    /// the bar isn't - the two can't drift apart.
    /// Because the whole fall lives IN this trace, the block's own surface
    /// no longer has to ramp any of it away (see TrackBlockSpawner's
    /// former fallTiltFraction, dropped with 0041): a Trigger block is
    /// simply flat, sitting FallHeight below the previous one's exit.
    /// </summary>
    public class TriggerFallMarbleTrace : IMarbleTrace
    {
        private readonly TrackBlock block;
        private readonly Vector3 fallStart;   // the previous block's exit, in THIS block's local space
        private readonly Vector3 padLanding;  // top of the pad bar, where the marble hits
        private readonly Vector3 grooveEntry; // point on the block's own groove centerline where the bounce comes down
        private readonly Vector3 exit;        // this block's own true exit point (curve-aware, see SampleGroovePointLocal)

        private readonly float landingT; // where grooveEntry sits along the groove (0 = entry, 1 = exit) - the roll phase samples onward from here
        private readonly float fallShare;
        private readonly float bounceShare;
        private readonly float rollShare;
        private readonly float bounceHeight;

        /// fallSideLocal: horizontal direction, in this block's own local
        /// space, of the side the marble arrives FROM (the same vector
        /// XylophoneBlockDecoration puts the bar on). padLandingLocal: where
        /// the marble lands, in this block's local space (see
        /// InstrumentBlockDecoration.PadLandingLocal). grooveLandingT: fraction (0 = entry, 1 = exit)
        /// along the block's own groove centerline (TrackBlock.
        /// SampleGroovePointLocal) where the bounce comes down - past the
        /// pad, so it reads as a natural hop off the bar. Using this
        /// block's real centerline (rather than a straight line to the
        /// exit) is what makes the roll phase follow a curved Trigger
        /// block's real 90° turn (see 0052 follow-up) instead of cutting
        /// across it. fallBeatFraction: how much of this block's beat the
        /// fall takes - the same for every Trigger block, see the class
        /// remarks.
        public TriggerFallMarbleTrace(TrackBlock block, Vector3 fallSideLocal, float fallHeight,
            Vector3 padLandingLocal, float grooveLandingT, float fallBeatFraction, float bounceHeight)
        {
            this.block = block;
            this.bounceHeight = Mathf.Max(bounceHeight, 0f);
            this.landingT = Mathf.Clamp01(grooveLandingT);

            Vector3 side = new Vector3(fallSideLocal.x, 0f, fallSideLocal.z);
            side = side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.back; // no input direction at all (shouldn't happen on a Trigger block) - assume the usual straight-through entry side

            Vector3 entry = block.EntryPointLocal; // only .y is meaningful for a curved block - see EntryPointLocal's own remarks
            exit = block.SampleGroovePointLocal(1f);
            float halfLength = block.Size.y * 0.5f;

            // The previous block's exit sits exactly on the shared cell
            // edge on the arrival side, FallHeight above this block's own
            // groove floor - that's where this trace has to start for the
            // two blocks' traces to stitch together without a jump.
            fallStart = side * halfLength + Vector3.up * (entry.y + Mathf.Max(fallHeight, 0f));
            padLanding = padLandingLocal;
            grooveEntry = block.SampleGroovePointLocal(landingT);

            fallShare = Mathf.Clamp(fallBeatFraction, 0.05f, 0.9f);

            // What's left of the beat after the fall goes to the bounce
            // and the roll in proportion to the ground each covers, so the
            // marble carries on at one even pace once it has landed.
            float remaining = 1f - fallShare;
            float bounceSpan = HorizontalDistance(padLanding, grooveEntry);
            float rollSpan = HorizontalDistance(grooveEntry, exit);
            float span = bounceSpan + rollSpan;
            bounceShare = span > 1e-6f ? remaining * (bounceSpan / span) : remaining * 0.5f;
            rollShare = remaining - bounceShare;
        }

        /// The marble lands on the bar here - the same beat position on
        /// every Trigger block, see the class remarks.
        public float ImpactT => fallShare;

        public Vector3 SampleLocal(float t)
        {
            t = Mathf.Clamp01(t);

            if (t <= fallShare) return Arc(fallStart, padLanding, t / fallShare, 0f);

            t -= fallShare;
            if (t <= bounceShare) return Arc(padLanding, grooveEntry, bounceShare > 0f ? t / bounceShare : 1f, bounceHeight);

            t -= bounceShare;
            float u = rollShare > 0f ? Mathf.Clamp01(t / rollShare) : 1f;
            return block.SampleGroovePointLocal(Mathf.Lerp(landingT, 1f, u));
        }

        /// Point at normalized progress u on a gravity-shaped arc from
        /// `from` to `to` whose highest point is apexHeight above `from` -
        /// moving horizontally at an even rate, and vertically as
        /// y = launchSpeed*u - gravity*u^2/2, i.e. the shape of a real
        /// throw. Both the "gravity" and the launch speed are SOLVED from
        /// the two endpoints and the apex, rather than fixed: that's what
        /// lets the same arc serve a plain drop (apexHeight 0 gives launch
        /// speed 0 - a marble tipping over an edge), a hop off the pad,
        /// and the odd case of a pad that sits higher than the block the
        /// marble came from (the apex is raised just enough to reach it) -
        /// all landing exactly on their target, in exactly the slice of
        /// the beat they were given.
        private static Vector3 Arc(Vector3 from, Vector3 to, float u, float apexHeight)
        {
            u = Mathf.Clamp01(u);
            Vector3 point = Vector3.Lerp(from, to, u);

            float rise = to.y - from.y;
            float apex = Mathf.Max(apexHeight, rise); // an arc can't come down onto something above its own highest point
            float gravityRoot = Mathf.Sqrt(2f * apex) + Mathf.Sqrt(Mathf.Max(2f * (apex - rise), 0f));
            float gravity = gravityRoot * gravityRoot;
            float launchSpeed = Mathf.Sqrt(2f * gravity * apex);

            point.y = from.y + launchSpeed * u - 0.5f * gravity * u * u;
            return point;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            return new Vector2(b.x - a.x, b.z - a.z).magnitude;
        }
    }
}
