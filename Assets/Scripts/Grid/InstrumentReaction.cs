using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// First concrete Block-Trigger reaction (see 0023/0024): plays this
    /// block's own BlockDefinition.AudioEvent when triggered. Owns its own
    /// AudioSource so it can react autonomously - MarbleController no
    /// longer has (or needs) one of its own, see its class remarks. Reads
    /// its clip from the block's Definition rather than being handed one,
    /// same pattern as BlockFlashFeedback reading FlashColor from there.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(TrackBlock))]
    public class InstrumentReaction : MonoBehaviour
    {
        private TrackBlock block;
        private AudioSource audioSource;
        private BlockTrigger trigger;

        private void Awake()
        {
            block = GetComponent<TrackBlock>();
            audioSource = GetComponent<AudioSource>();
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

        private void HandleTriggered()
        {
            AudioClip clip = block.Definition.AudioEvent;
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
