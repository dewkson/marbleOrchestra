using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// What InstrumentBlockDecoration hands to InstrumentPadFeedback: the
    /// element's root (scaled for the pulse - pivot at its base), all its
    /// renderers, and whether the feedback should paint its own pad color
    /// on it (built-in geometry) or keep the model's own materials (external
    /// InstrumentVisual models).
    /// </summary>
    public class InstrumentElement
    {
        public Transform Root { get; private set; }
        public Renderer[] Renderers { get; private set; }
        public bool UsePadColor { get; private set; }
        public Animator Animator { get; private set; } // only for models with a hit clip
        public AnimationClip HitClip { get; private set; }
        public float PulseScaleAmount { get; private set; } = -1f; // < 0: use InstrumentPadFeedback's own scaleAmount
        public bool Flash { get; private set; } // whether the color flash/scale pulse runs on a hit

        public static InstrumentElement Tinted(Renderer renderer) =>
            new InstrumentElement { Root = renderer.transform, Renderers = new[] { renderer }, UsePadColor = true, Flash = true };

        public static InstrumentElement Model(Transform root, Renderer[] renderers, Animator animator, AnimationClip hitClip, bool flash, float pulseScaleAmount) =>
            new InstrumentElement { Root = root, Renderers = renderers, UsePadColor = false, Animator = animator, HitClip = hitClip, Flash = flash, PulseScaleAmount = pulseScaleAmount };
    }
}
