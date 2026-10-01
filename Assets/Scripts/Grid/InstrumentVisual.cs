using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// An external 3D model for a Trigger block's instrument element (see
    /// 0055 follow-up), used instead of the built-in InstrumentType
    /// geometry when a TriggerCellContent references it.
    /// Prefab conventions: pivot at the middle of the model's base (it sits
    /// on the block's shoulder plane and pulses in place); local +Z is the
    /// "front", which faces the side the marble falls in from; a child
    /// named "LandingPoint" marks the exact spot the marble lands on
    /// (without it, the center of the top of the model's mesh bounds is
    /// used, with a warning).
    /// </summary>
    [CreateAssetMenu(fileName = "InstrumentVisual_", menuName = "MarbleOrchestra/Instrument Visual")]
    public class InstrumentVisual : ScriptableObject
    {
        public const string LandingPointName = "LandingPoint";

        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0.01f)] private float scale = 1f; // uniform scale applied to the prefab to fit the block
        [SerializeField, Range(0.2f, 0.9f)] private float landingOffsetFraction = 0.6f; // distance of the model's PIVOT from the block's center toward the fall side, as a fraction of half the block's width

        [SerializeField] private AnimationClip hitClip; // optional: played once when the marble hits this instrument (no Animator Controller needed). Should end in the model's rest pose
        [SerializeField] private bool flashAlongsideClip = false; // true: the color flash and scale pulse also run while the clip plays; false: the clip replaces them

        [SerializeField, Range(-180f, 180f)] private float yawOffsetDegrees = 0f; // extra rotation around the model's pivot (Y axis) on top of "front (+Z) faces the fall side" - the LandingPoint moves with it
        [SerializeField] private bool overridePulseScale = false; // true: use pulseScaleAmount below instead of the value on the TrackBlock prefab's InstrumentPadFeedback
        [SerializeField, Range(0f, 1f)] private float pulseScaleAmount = 0.25f; // how much bigger the model gets at the peak of the hit pulse (0 = no scaling)

        [SerializeField, Range(-0.9f, 0.9f)] private float lateralOffsetFraction = 0f; // shifts the model's PIVOT sideways, across the fall direction (horizontal, not up/down), as a fraction of half the block's width. Positive = to the right when looking along the marble's fall, negative = to the left

        public GameObject Prefab => prefab;
        public float LateralOffsetFraction => lateralOffsetFraction;
        public float YawOffsetDegrees => yawOffsetDegrees;
        public bool OverridePulseScale => overridePulseScale;
        public float PulseScaleAmount => pulseScaleAmount;
        public AnimationClip HitClip => hitClip;
        public bool FlashAlongsideClip => flashAlongsideClip;
        public float Scale => scale;
        public float LandingOffsetFraction => landingOffsetFraction;

        /// Landing point in the prefab root's local space, already multiplied by Scale.
        public Vector3 LandingLocal
        {
            get
            {
                if (prefab == null) return Vector3.zero;

                Transform root = prefab.transform;
                Transform marker = root.Find(LandingPointName);
                if (marker == null) marker = FindDeep(root, LandingPointName);
                if (marker != null) return root.InverseTransformPoint(marker.position) * scale;

                Debug.LogWarning($"InstrumentVisual '{name}': Prefab hat kein Child '{LandingPointName}' - nehme die Oberseite der Mesh-Bounds.", this);
                return TopCenterOfMeshes(root) * scale;
            }
        }

        private static Transform FindDeep(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                Transform found = FindDeep(child, childName);
                if (found != null) return found;
            }
            return null;
        }

        private static Vector3 TopCenterOfMeshes(Transform root)
        {
            bool any = false;
            Bounds bounds = new Bounds();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Bounds b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
            }
            return any ? new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) : Vector3.zero;
        }
    }
}
