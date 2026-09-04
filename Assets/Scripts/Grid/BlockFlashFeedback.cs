using System.Collections;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Generic visual feedback for any triggered TrackBlock: briefly
    /// overrides the block's rendered color via a MaterialPropertyBlock -
    /// NOT the shared Material instance directly, since TrackBlockSpawner
    /// assigns the same Material to every block of a track; mutating it
    /// directly would flash all of them at once. Reads which color to
    /// flash from the block's own BlockDefinition.FlashColor (see 0027),
    /// not from a serialized field here - one source of truth for "what
    /// this block looks like when triggered".
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(TrackBlock))]
    public class BlockFlashFeedback : MonoBehaviour
    {
        [SerializeField] private float flashDuration = 0.15f;

        private TrackBlock block;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private BlockTrigger trigger;
        private Coroutine flashRoutine;

        private void Awake()
        {
            block = GetComponent<TrackBlock>();
            meshRenderer = GetComponent<MeshRenderer>();
            propertyBlock = new MaterialPropertyBlock();
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

        private void HandleTriggered() => Flash(block.Definition.FlashColor);

        public void Flash(Color color)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(color));
        }

        private IEnumerator FlashRoutine(Color color)
        {
            propertyBlock.SetColor("_BaseColor", color); // URP/Lit
            propertyBlock.SetColor("_Color", color);      // Standard fallback - harmless if the shader lacks either property

            // TrackBlock's mesh has two submeshes/materials since 0032
            // (shoulders/walls vs. the groove itself) - SetPropertyBlock
            // without an index only ever targets submesh 0, so both must be
            // set explicitly or the groove wouldn't flash along with it.
            for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
                meshRenderer.SetPropertyBlock(propertyBlock, i);

            yield return new WaitForSeconds(flashDuration);

            for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
                meshRenderer.SetPropertyBlock(null, i); // clears the override, reverts to that submesh's own Material color
            flashRoutine = null;
        }
    }
}
