using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// The one shared musical time base of a simulation run (see 0043):
    /// beats elapsed since construction, derived from absolute time every
    /// frame rather than summed up from Time.deltaTime - so no rounding or
    /// frame overshoot ever accumulates, and every track reading it stays
    /// locked to the same beat grid.
    /// Uses Time.timeAsDouble rather than AudioSettings.dspTime: dspTime
    /// only advances once per audio buffer, which would make the marble
    /// visibly stutter, and triggers are fired from frame updates
    /// (PlayOneShot) anyway - so frame time is exactly as precise here.
    /// Sample-accurate audio would need PlayScheduled against dspTime on
    /// top of this.
    /// </summary>
    public class BeatClock
    {
        private readonly double beatsPerSecond;
        private readonly double startTime;

        public BeatClock(float beatsPerSecond)
        {
            this.beatsPerSecond = Mathf.Max(beatsPerSecond, 0.01f);
            startTime = Time.timeAsDouble;
        }

        /// Beats since this clock was created - fractional, 0 at creation.
        public double CurrentBeat => (Time.timeAsDouble - startTime) * beatsPerSecond;
    }
}
