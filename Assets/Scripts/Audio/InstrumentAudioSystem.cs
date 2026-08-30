using UnityEngine;
using MarbleOrchestra.Grid;

namespace MarbleOrchestra.Audio
{
    /// <summary>
    /// The one place in the project that actually knows how to turn an
    /// InstrumentReaction.Played event into sound (see 0026) - blocks
    /// themselves only report WHAT was triggered (BlockDefinition), never
    /// play anything directly. Currently plays BlockDefinition.AudioEvent
    /// straight away; a future Music-System (rhythm quantization, mixing,
    /// per-instrument volume/polyphony rules) would replace only this
    /// class's insides, not the block-side contract.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class InstrumentAudioSystem : MonoBehaviour
    {
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            InstrumentReaction.Played += HandlePlayed;
        }

        private void OnDisable()
        {
            InstrumentReaction.Played -= HandlePlayed;
        }

        private void HandlePlayed(BlockDefinition definition)
        {
            if (definition.AudioEvent != null) audioSource.PlayOneShot(definition.AudioEvent);
        }
    }
}
