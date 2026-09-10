using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// One block's own marble trace (see IMarbleTrace/0038), resolved
    /// against that block's transform and the track's tempo - i.e.
    /// everything MarbleController needs to play back the marble's path
    /// across ONE block, without knowing anything about grooves, curves or
    /// falls. Handed out by TrackBlockSpawner.TryGetTraceSegment.
    /// This is where the track's musical grid lives: Duration is one beat
    /// (1 / cellsPerSecond) for EVERY block, whatever its geometry, so a
    /// cell is a beat and the sounds a marble sets off stay in time.
    /// </summary>
    public readonly struct TrackTraceSegment
    {
        private readonly TrackBlock block;
        private readonly IMarbleTrace trace;

        public TrackTraceSegment(TrackBlock block, IMarbleTrace trace, float cellsPerSecond)
        {
            this.block = block;
            this.trace = trace;

            Duration = 1f / Mathf.Max(cellsPerSecond, 0.01f);
            ImpactTime = trace != null ? Mathf.Clamp01(trace.ImpactT) * Duration : 0f;
        }

        /// Seconds the marble spends on this block - one beat, the same
        /// for every block on the track.
        public float Duration { get; }

        /// Seconds into this segment at which this block's trigger fires -
        /// its real moment of arrival (see IMarbleTrace.ImpactT).
        public float ImpactTime { get; }

        public bool IsValid => block != null && trace != null;

        /// World position `seconds` into this segment, raised by
        /// verticalOffset (the marble's own radius, so it rests ON the
        /// traced surface instead of sinking into it).
        public Vector3 SampleWorld(float seconds, float verticalOffset)
        {
            if (!IsValid) return Vector3.zero;
            return block.transform.TransformPoint(trace.SampleLocal(seconds / Duration)) + Vector3.up * verticalOffset;
        }
    }
}
