using System;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Generic, content-agnostic trigger for a TrackBlock: fired by
    /// MarbleController when the marble reaches this block's cell (see
    /// TriggerCellContent), regardless of what the block actually does in
    /// response. Deliberately payload-free - sibling components (e.g.
    /// InstrumentPadFeedback, InstrumentReaction) subscribe and decide for
    /// themselves what to do, reading their own data from
    /// GetComponent&lt;TrackBlock&gt;().Definition (see 0027).
    /// </summary>
    public class BlockTrigger : MonoBehaviour
    {
        public event Action Triggered;

        /// Fired for EVERY block's trigger (see 0068) - lets a central
        /// listener such as BlockVisualEffectSystem react without
        /// subscribing to blocks that TrackBlockSpawner keeps destroying
        /// and rebuilding (same reasoning as InstrumentReaction.Played).
        public static event Action<BlockTrigger> AnyFired;

        public void Fire()
        {
            Triggered?.Invoke();
            AnyFired?.Invoke(this);
        }
    }
}
