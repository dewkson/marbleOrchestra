using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Visual feedback for a Trigger block's INSTRUMENT ELEMENT (see
    /// 0042): the raised bar the marble lands on - today built by
    /// XylophoneBlockDecoration, later any other instrument's own shape -
    /// briefly flashes and pulses in size when the block is triggered.
    /// Only that element reacts; the TrackBlock's own body (shoulders,
    /// groove) is deliberately left alone, and blocks without an
    /// instrument element (Normal/Start/Goal) show nothing at all -
    /// this component simply stays idle until something hands it an
    /// element via Attach.
    /// It also owns the element's NORMAL look: padColor is applied to it
    /// as soon as it's attached, so the bar reads as an instrument rather
    /// than as more terrain. Both the base and the flash color go through
    /// a MaterialPropertyBlock rather than the Material itself, because
    /// TrackBlockSpawner hands the same shared Material to every block -
    /// mutating it would recolor the whole track (same reasoning as the
    /// block-wide flash this replaces).
    /// Lives on the TrackBlock prefab next to BlockTrigger, whose event it
    /// subscribes to (see 0023) - so the settings below are edited once,
    /// on that prefab, and apply to every instrument block of every track.
    /// </summary>
    [RequireComponent(typeof(TrackBlock))]
    public class InstrumentPadFeedback : MonoBehaviour
    {
        [SerializeField] private Color padColor = new Color(0.85f, 0.72f, 0.35f); // the instrument element's own normal color, applied as soon as it's attached
        [SerializeField] private Color flashColor = Color.white; // color it flashes to when hit - used unless useContentFlashColor takes over below
        [SerializeField] private bool useContentFlashColor = true; // true: a SoundTriggerContent's own FlashColor wins (per-instrument colors, see 0027); false: always use flashColor
        [SerializeField, Range(0f, 1f)] private float scaleAmount = 0.25f; // how much bigger the element gets at the peak of the pulse (0 = no scaling, 0.25 = a quarter larger)
        [SerializeField] private float pulseDuration = 0.18f; // seconds for the whole up-and-back pulse (color and scale together)

        private TrackBlock block;
        private BlockTrigger trigger;
        private Renderer[] elements = System.Array.Empty<Renderer>();
        private Color[] baseColors = System.Array.Empty<Color>(); // per renderer: padColor for built-in geometry, the model's own material color for external models
        private bool usePadColor = true;
        private bool flashOnHit = true;
        private float elementScaleAmount = -1f;
        private Animator animator;
        private AnimationClip hitClip;
        private PlayableGraph graph;
        private AnimationClipPlayable clipPlayable;
        private Coroutine clipRoutine;
        private Transform elementTransform;
        private Vector3 elementBaseScale = Vector3.one;
        private MaterialPropertyBlock propertyBlock;
        private Coroutine pulseRoutine;

        private void Awake()
        {
            block = GetComponent<TrackBlock>();
            trigger = GetComponent<BlockTrigger>();
            propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (trigger != null) trigger.Triggered += HandleTriggered;
        }

        private void OnDisable()
        {
            if (trigger != null) trigger.Triggered -= HandleTriggered;
        }

        /// Hands this component the instrument element to react on -
        /// called by whoever builds that element (TrackBlockSpawner, right
        /// after XylophoneBlockDecoration made it). Until then, and on
        /// every block that never gets one, this component does nothing.
        /// The element's pivot must sit at its own base for the pulse to
        /// grow it in place rather than shift it (see
        /// XylophoneBlockDecoration).
        public void Attach(InstrumentElement instrumentElement)
        {
            elements = instrumentElement != null ? instrumentElement.Renderers : System.Array.Empty<Renderer>();
            usePadColor = instrumentElement == null || instrumentElement.UsePadColor;
            flashOnHit = instrumentElement == null || instrumentElement.Flash;
            elementScaleAmount = instrumentElement != null ? instrumentElement.PulseScaleAmount : -1f;
            animator = instrumentElement != null ? instrumentElement.Animator : null;
            hitClip = instrumentElement != null ? instrumentElement.HitClip : null;
            elementTransform = instrumentElement != null ? instrumentElement.Root : null;
            elementBaseScale = elementTransform != null ? elementTransform.localScale : Vector3.one;

            baseColors = new Color[elements.Length];
            for (int i = 0; i < elements.Length; i++) baseColors[i] = usePadColor ? padColor : MaterialColor(elements[i]);
            ApplyColors(0f, Color.white);
        }

        private static Color MaterialColor(Renderer renderer)
        {
            Material material = renderer != null ? renderer.sharedMaterial : null;
            if (material == null) return Color.white;
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color")) return material.GetColor("_Color");
            return Color.white;
        }

        private void HandleTriggered()
        {
            if (hitClip != null && animator != null) PlayHitClip();

            if (elements.Length == 0 || !flashOnHit) return;

            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        /// Plays the model's hit clip straight through a PlayableGraph (no
        /// Animator Controller), restarting it if the instrument is hit
        /// again before it ended. Afterwards the pose is reset to the
        /// clip's first frame, so the model is back at rest.
        private void PlayHitClip()
        {
            if (!graph.IsValid())
            {
                graph = PlayableGraph.Create("InstrumentHit");
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Hit", animator);
                clipPlayable = AnimationClipPlayable.Create(graph, hitClip);
                clipPlayable.SetApplyFootIK(false);
                output.SetSourcePlayable(clipPlayable);
            }

            if (clipRoutine != null) StopCoroutine(clipRoutine);
            clipPlayable.SetTime(0.0);
            clipPlayable.SetDone(false);
            graph.Play();
            clipRoutine = StartCoroutine(ClipRoutine());
        }

        private IEnumerator ClipRoutine()
        {
            yield return new WaitForSeconds(hitClip.length);

            clipPlayable.SetTime(0.0);
            graph.Evaluate(0f);
            graph.Stop();
            clipRoutine = null;
        }

        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        private IEnumerator PulseRoutine()
        {
            Color peak = useContentFlashColor && block != null ? block.Definition.FlashColor : flashColor;
            float duration = Mathf.Max(pulseDuration, 0.01f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // One half sine over the whole duration: 0 at the start,
                // 1 in the middle, 0 again at the end - the "up and back"
                // shape for color and scale alike, with no snap at either
                // end (a linear ramp would jump back at the peak).
                float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);

                ApplyColors(pulse, peak);
                if (elementTransform != null) elementTransform.localScale = elementBaseScale * (1f + (elementScaleAmount >= 0f ? elementScaleAmount : scaleAmount) * pulse);

                yield return null;
            }

            ApplyColors(0f, peak);
            if (elementTransform != null) elementTransform.localScale = elementBaseScale;
            pulseRoutine = null;
        }

        /// Blends every renderer from its own base color toward `peak` by `amount`.
        /// External models at amount 0 get their property block cleared, so
        /// they look exactly like their own materials again.
        private void ApplyColors(float amount, Color peak)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == null) continue;

                if (!usePadColor && amount <= 0f)
                {
                    elements[i].SetPropertyBlock(null);
                    continue;
                }

                Color color = Color.Lerp(baseColors[i], peak, amount);
                propertyBlock.Clear();
                propertyBlock.SetColor("_BaseColor", color); // URP/Lit
                propertyBlock.SetColor("_Color", color);     // Standard fallback - harmless if the shader lacks either property
                elements[i].SetPropertyBlock(propertyBlock);
            }
        }
    }
}
