using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// The path the marble takes through ONE block in
    /// MovementMode.Kinematic3D (see 0038) - in that block's OWN local
    /// space, parametrized by the block's own BEAT: t goes 0 to 1 while
    /// the marble crosses this one block, and EVERY block takes exactly
    /// the same amount of real time (one beat = 1 / cellsPerSecond, see
    /// TrackTraceSegment).
    /// That equal duration is a hard rule, not an accident of geometry:
    /// the marble is what plays the music, so one cell has to be one beat
    /// for the track to stay in time. What a trace may vary is only HOW it
    /// spends that beat - a fall accelerates, a bounce arcs, a roll is
    /// uniform - never how long the beat is. (A curve therefore moves a
    /// little slower than a straight block, since it covers a shorter arc
    /// in the same beat, and that is exactly what keeps the rhythm even.)
    /// Every trace covers exactly its own block's own extent: it starts
    /// where the marble enters that block and ends where it leaves it, so
    /// consecutive traces stitch end to end and nothing has to reconstruct
    /// the marble's path from neighbouring geometry (which is what
    /// TrackBlockSpawner used to do, at the cost of a half-cell offset
    /// between the horizontal and the vertical motion - see 0041).
    /// Set per block by TrackBlockSpawner (see its CreateTrace) the same
    /// way IBlockProfile is - one universal TrackBlock, swapped behaviour
    /// per block variant, never a subclass.
    /// </summary>
    public interface IMarbleTrace
    {
        /// Local-space position at beat position t (0 = entering this
        /// block, 1 = leaving it). Callers may pass anything; a trace
        /// clamps.
        Vector3 SampleLocal(float t);

        /// Beat position at which this block's trigger fires - the moment
        /// the marble visibly ARRIVES (e.g. hits a Trigger block's pad),
        /// so sound and impact line up.
        /// It is the SAME value for every Trigger block on the track (see
        /// TriggerFallMarbleTrace), which is what keeps the music on a
        /// even grid: only Trigger blocks make a sound at all, so a
        /// constant phase inside the beat shifts all notes alike and
        /// leaves the spacing between them a whole number of beats.
        /// 0 for traces without a distinct impact: the marble simply
        /// entering the block is its arrival.
        float ImpactT { get; }
    }
}
