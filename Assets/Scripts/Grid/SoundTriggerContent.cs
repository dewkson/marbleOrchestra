using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Plays an AudioClip once when the marble arrives at this cell.
    /// </summary>
    [CreateAssetMenu(fileName = "Sound_", menuName = "MarbleOrchestra/Cell Content/Sound Trigger")]
    public class SoundTriggerContent : CellContentDefinition
    {
        [SerializeField] private AudioClip clip;
        public AudioClip Clip => clip;

        public override void Activate(CellContentContext context)
        {
            if (clip != null && context.AudioSource != null)
            {
                context.AudioSource.PlayOneShot(clip);
            }
        }
    }
}
