using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Trace for a Trigger block WITHOUT an instrument pad (see
    /// TrackBlockSpawner.showInstrumentPads): the marble rolls off the
    /// previous block's exit edge and drops straight into this block's
    /// own groove - shaped like a real throw rather than around the old
    /// pad geometry (TriggerFallMarbleTrace).
    ///
    /// Horizontally the marble NEVER changes pace: its position along the
    /// groove centerline is simply t, the same even "one cell per beat"
    /// every rolling block uses - so it leaves the edge with exactly the
    /// speed it rolled at, flies on at that speed and keeps it after
    /// landing. Following the centerline (TrackBlock.SampleGroovePointLocal)
    /// also keeps a curved Trigger block's flight inside its groove.
    ///
    /// Vertically it's an offset above the groove floor:
    /// 1. FALL (t in [0, fallShare]) - it tips over the edge with zero
    ///    vertical speed and falls FallHeight under constant gravity,
    ///    landing at ImpactT = fallShare. The fall takes the same fraction
    ///    of the beat on every Trigger block, so every note sounds at the
    ///    same phase and the music stays on the grid (see
    ///    TriggerFallMarbleTrace); a higher drop therefore simply means a
    ///    stronger gravity for that block.
    /// 2. BOUNCES - each one restitution times as high as the previous
    ///    drop/bounce, under that SAME gravity, so its length follows from
    ///    its height (duration = 2 * fallShare * sqrt(height / FallHeight))
    ///    exactly like a real bouncing ball. Stops at maxBounces, once a
    ///    hop gets negligibly small, or when the beat runs out.
    /// 3. ROLL - on along the groove to the exit.
    /// </summary>
    public class GrooveDropMarbleTrace : IMarbleTrace
    {
        private const int MaxBounceCapacity = 8;
        private const float MinBounceHeight = 0.003f; // below this a hop isn't visible any more

        private readonly TrackBlock block;
        private readonly float fallHeight;
        private readonly float fallShare;
        private readonly float[] bounceStart = new float[MaxBounceCapacity];
        private readonly float[] bounceDuration = new float[MaxBounceCapacity];
        private readonly float[] bounceHeight = new float[MaxBounceCapacity];
        private readonly int bounceCount;

        /// fallHeight: drop from the previous block's exit to this groove.
        /// fallBeatFraction: share of the beat the fall takes (the impact
        /// phase). restitution: height of each bounce relative to the one
        /// before (0 = no bounce). maxBounces: upper limit of hops.
        public GrooveDropMarbleTrace(TrackBlock block, float fallHeight, float fallBeatFraction, float restitution, int maxBounces)
        {
            this.block = block;
            this.fallHeight = Mathf.Max(fallHeight, 0f);
            fallShare = Mathf.Clamp(fallBeatFraction, 0.05f, 0.9f);

            restitution = Mathf.Clamp(restitution, 0f, 0.95f);
            maxBounces = Mathf.Clamp(maxBounces, 0, MaxBounceCapacity);

            float t = fallShare;
            float height = this.fallHeight;
            int count = 0;
            while (count < maxBounces && this.fallHeight > 0f)
            {
                height *= restitution;
                if (height < MinBounceHeight) break;

                // Same gravity as the fall: g = 2 * fallHeight / fallShare^2,
                // so a hop of apex h takes 2 * sqrt(2h / g).
                float duration = 2f * fallShare * Mathf.Sqrt(height / this.fallHeight);
                if (t + duration > 1f) break;

                bounceStart[count] = t;
                bounceDuration[count] = duration;
                bounceHeight[count] = height;
                count++;
                t += duration;
            }
            bounceCount = count;
        }

        public float ImpactT => fallShare;

        public Vector3 SampleLocal(float t)
        {
            t = Mathf.Clamp01(t);
            return block.SampleGroovePointLocal(t) + Vector3.up * HeightAboveGroove(t);
        }

        private float HeightAboveGroove(float t)
        {
            if (t < fallShare)
            {
                float u = t / fallShare;
                return fallHeight * (1f - u * u); // zero vertical speed at the edge, accelerating down
            }

            for (int i = 0; i < bounceCount; i++)
            {
                float local = t - bounceStart[i];
                if (local < 0f || local > bounceDuration[i]) continue;
                float u = local / bounceDuration[i];
                return 4f * bounceHeight[i] * u * (1f - u); // parabola, apex at the middle
            }

            return 0f;
        }
    }
}
