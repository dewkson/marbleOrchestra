using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Trace for a Normal block whose path turns 90 degrees (see
    /// TrackBlock.SetCurve/0040): the marble follows that block's own
    /// real, unmodified curved centerline instead of cutting across it.
    /// Like every trace it takes exactly one beat (see IMarbleTrace), so
    /// the marble moves a little slower through a turn than along a
    /// straight block - its quarter-circle arc is genuinely shorter than a
    /// full cell - which is what keeps a turn from stealing time from the
    /// music.
    /// </summary>
    public class CurvedMarbleTrace : IMarbleTrace
    {
        private readonly TrackBlock block;

        public CurvedMarbleTrace(TrackBlock block)
        {
            this.block = block;
        }

        public float ImpactT => 0f;

        public Vector3 SampleLocal(float t) => block.SampleGroovePointLocal(t);
    }
}
