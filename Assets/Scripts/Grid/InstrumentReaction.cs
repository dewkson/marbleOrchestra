using System;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Block-Trigger reaction (see 0023/0024) that reports this block's own
    /// BlockDefinition to a static event instead of playing audio itself
    /// (see 0026) - decouples the block from any specific Audio-/Music-
    /// system. TrackBlockSpawner destroys/rebuilds a track's blocks on
    /// every path change, so a central listener can't subscribe to
    /// individual instances; a static event lets it subscribe once and
    /// have blocks come and go freely (see InstrumentAudioSystem).
    /// </summary>
    [RequireComponent(typeof(TrackBlock))]
    public class InstrumentReaction : MonoBehaviour
    {
        public static event Action<BlockDefinition> Played;

        private TrackBlock block;
        private BlockTrigger trigger;

        private void Awake()
        {
            block = GetComponent<TrackBlock>();
            trigger = GetComponent<BlockTrigger>();
        }

        private void OnEnable()
        {
            if (trigger != null) trigger.Triggered += HandleTriggered;
        }

        private void OnDisable()
        {
            if (trigger != null) trigger.Triggered -= HandleTriggered;
        }

        private void HandleTriggered() => Played?.Invoke(block.Definition);
    }
}
