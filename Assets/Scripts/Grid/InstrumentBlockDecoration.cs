using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Builds the 3D element of a Trigger block for a given InstrumentType
    /// (see 0055) and answers the two geometry questions
    /// TriggerFallMarbleTrace and TrackBlockSpawner need per instrument:
    /// how high the landing surface is (PadTopY) and how far from the
    /// block's center it sits (PadCenterOffset). Xylophone delegates to
    /// XylophoneBlockDecoration; the round instruments are built here from
    /// stacked (truncated) cylinders whose TOP CENTER is the landing point.
    /// Every element's pivot sits at the middle of its base, which is what
    /// lets InstrumentPadFeedback pulse its scale in place.
    /// </summary>
    public static class InstrumentBlockDecoration
    {
        private const int Sides = 24;
        private const float EdgeMarginFraction = 0.03f;
        private const float DrumRadiusFraction = 0.36f; // of halfCell

        /// Height of the landing surface above the block's shoulder plane.
        public static float PadTopY(InstrumentType type, float grooveRadius, InstrumentVisual visual = null)
        {
            if (HasModel(visual)) return visual.LandingLocal.y;

            switch (type)
            {
                case InstrumentType.Timpani: return grooveRadius * 0.8f;
                case InstrumentType.Snare: return grooveRadius * 0.5f;
                case InstrumentType.HiHat: return grooveRadius * 0.9f;
                default: return XylophoneBlockDecoration.PadTopY(grooveRadius);
            }
        }

        /// Distance from the block's center to the landing point, toward the fall side.
        public static float PadCenterOffset(InstrumentType type, float grooveRadius, float sideWidth, InstrumentVisual visual = null)
        {
            if (HasModel(visual)) return (grooveRadius + sideWidth) * visual.LandingOffsetFraction;

            if (type == InstrumentType.Xylophone) return XylophoneBlockDecoration.PadCenterOffset(grooveRadius, sideWidth);

            float halfCell = grooveRadius + sideWidth;
            return halfCell - Radius(halfCell) - halfCell * EdgeMarginFraction;
        }

        private static float Radius(float halfCell) => halfCell * DrumRadiusFraction;

        /// Returns the element's MeshRenderer (to hand to InstrumentPadFeedback).
        public static InstrumentElement Build(InstrumentType type, TrackBlock block, Vector3 fallSideLocal, float grooveRadius, float sideWidth, Material material, InstrumentVisual visual = null)
        {
            if (HasModel(visual)) return BuildModel(visual, block, fallSideLocal, grooveRadius, sideWidth);

            if (type == InstrumentType.Xylophone)
                return InstrumentElement.Tinted(XylophoneBlockDecoration.Build(block, fallSideLocal, grooveRadius, sideWidth, material));

            float halfCell = grooveRadius + sideWidth;
            float height = PadTopY(type, grooveRadius);
            float radius = Radius(halfCell);
            float offset = PadCenterOffset(type, grooveRadius, sideWidth);

            // Same axis logic as XylophoneBlockDecoration.Build: the fall side is always along local X or Z.
            bool fallAlongX = Mathf.Abs(fallSideLocal.x) >= Mathf.Abs(fallSideLocal.z);
            float sign = fallAlongX ? Mathf.Sign(fallSideLocal.x) : Mathf.Sign(fallSideLocal.z);
            if (sign == 0f) sign = -1f;
            Vector3 center = fallAlongX ? new Vector3(sign * offset, 0f, 0f) : new Vector3(0f, 0f, sign * offset);

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            switch (type)
            {
                case InstrumentType.Timpani: // tapered bowl with a rim at the skin
                    AppendCylinder(0f, height * 0.75f, radius * 0.55f, radius * 0.95f, vertices, triangles);
                    AppendCylinder(height * 0.75f, height, radius, radius, vertices, triangles);
                    break;
                case InstrumentType.Snare: // shallow shell with a slightly wider rim
                    AppendCylinder(0f, height * 0.8f, radius * 0.92f, radius * 0.92f, vertices, triangles);
                    AppendCylinder(height * 0.8f, height, radius, radius, vertices, triangles);
                    break;
                case InstrumentType.HiHat: // stand with two thin cymbals
                    AppendCylinder(0f, height * 0.78f, radius * 0.12f, radius * 0.12f, vertices, triangles);
                    AppendCylinder(height * 0.66f, height * 0.78f, radius * 0.9f, radius, vertices, triangles);
                    AppendCylinder(height * 0.88f, height, radius, radius * 0.9f, vertices, triangles);
                    break;
            }

            Mesh mesh = new Mesh { name = type + "Block" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject element = new GameObject(type + "Block");
            element.transform.SetParent(block.transform, false);
            element.transform.localPosition = center;
            element.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer = element.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return InstrumentElement.Tinted(renderer);
        }

        private static bool HasModel(InstrumentVisual visual) => visual != null && visual.Prefab != null;

        private static Vector3 FallSide(Vector3 fallSideLocal)
        {
            Vector3 side = new Vector3(fallSideLocal.x, 0f, fallSideLocal.z);
            return side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.back; // no input direction (shouldn't happen on a Trigger block) - assume the usual straight-through entry side
        }

        /// The model's pivot in the block's local space: out toward the fall
        /// side by the Landing Offset, and sideways (across the fall
        /// direction) by the Lateral Offset. Seen along the marble's fall
        /// (from the fall side toward the block's center), positive Lateral
        /// Offset is to the right.
        private static Vector3 ModelPivotLocal(Vector3 side, float grooveRadius, float sideWidth, InstrumentVisual visual)
        {
            float halfCell = grooveRadius + sideWidth;
            Vector3 lateral = Vector3.Cross(Vector3.up, -side); // right-hand side seen along the fall
            return side * PadCenterOffset(InstrumentType.Xylophone, grooveRadius, sideWidth, visual)
                + lateral * (halfCell * visual.LateralOffsetFraction);
        }

        private static Quaternion ModelRotation(Vector3 side, InstrumentVisual visual) =>
            Quaternion.LookRotation(side, Vector3.up) * Quaternion.Euler(0f, visual.YawOffsetDegrees, 0f);

        /// Where the marble lands, in the block's local space - what
        /// TriggerFallMarbleTrace aims its fall at. For built-in geometry
        /// that's the top center of the element on the fall side; for an
        /// external model it's the LandingPoint after the model's pivot
        /// placement and Yaw Offset rotation, so it may lie off the fall axis.
        public static Vector3 PadLandingLocal(InstrumentType type, Vector3 fallSideLocal, float grooveRadius, float sideWidth, InstrumentVisual visual)
        {
            Vector3 side = FallSide(fallSideLocal);

            if (HasModel(visual))
            {
                Vector3 pivot = ModelPivotLocal(side, grooveRadius, sideWidth, visual);
                Vector3 landing = ModelRotation(side, visual) * visual.LandingLocal;
                return new Vector3(pivot.x + landing.x, landing.y, pivot.z + landing.z);
            }

            return side * PadCenterOffset(type, grooveRadius, sideWidth) + Vector3.up * PadTopY(type, grooveRadius);
        }

        /// Places an external model: pivot on the shoulder plane, front (+Z)
        /// toward the fall side, shifted so its LandingPoint sits exactly
        /// where PadTopY/PadCenterOffset tell TriggerFallMarbleTrace it is.
        private static InstrumentElement BuildModel(InstrumentVisual visual, TrackBlock block, Vector3 fallSideLocal, float grooveRadius, float sideWidth)
        {
            Vector3 side = FallSide(fallSideLocal);

            // The model's PIVOT sits on the fall side and it rotates around
            // that pivot (Yaw Offset); where the LandingPoint ends up
            // follows from that - see PadLandingLocal, which the marble's
            // trace reads, so both always agree.
            GameObject instance = Object.Instantiate(visual.Prefab, block.transform);
            instance.name = visual.Prefab.name;
            instance.transform.localRotation = ModelRotation(side, visual);
            instance.transform.localScale = Vector3.one * visual.Scale;
            instance.transform.localPosition = ModelPivotLocal(side, grooveRadius, sideWidth, visual);

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) Object.Destroy(collider); // the marble follows a trace, never physics

            AnimationClip clip = visual.HitClip;
            Animator animator = null;
            if (clip != null)
            {
                animator = instance.GetComponentInChildren<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = null; // the clip is driven directly by InstrumentPadFeedback
                animator.applyRootMotion = false;
            }

            bool flash = clip == null || visual.FlashAlongsideClip;
            return InstrumentElement.Model(instance.transform, instance.GetComponentsInChildren<Renderer>(), animator, clip, flash,
                visual.OverridePulseScale ? visual.PulseScaleAmount : -1f);
        }

        /// Truncated cone around the local Y axis (bottom/top caps included).
        /// Side vertices are shared around the ring (smooth shading); each cap has its own (flat).
        private static void AppendCylinder(float y0, float y1, float radiusBottom, float radiusTop, List<Vector3> vertices, List<int> triangles)
        {
            int ring = vertices.Count;
            for (int i = 0; i < Sides; i++)
            {
                float a = i * Mathf.PI * 2f / Sides;
                float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                vertices.Add(new Vector3(cos * radiusBottom, y0, sin * radiusBottom));
                vertices.Add(new Vector3(cos * radiusTop, y1, sin * radiusTop));
            }

            for (int i = 0; i < Sides; i++)
            {
                int next = (i + 1) % Sides;
                int b0 = ring + i * 2, t0 = b0 + 1, b1 = ring + next * 2, t1 = b1 + 1;
                // angle increases counter-clockwise seen from above (+X toward +Z), so this winding faces outward
                triangles.Add(b0); triangles.Add(t0); triangles.Add(t1);
                triangles.Add(b0); triangles.Add(t1); triangles.Add(b1);
            }

            AppendCap(y1, radiusTop, true, vertices, triangles);
            AppendCap(y0, radiusBottom, false, vertices, triangles);
        }

        private static void AppendCap(float y, float radius, bool up, List<Vector3> vertices, List<int> triangles)
        {
            int centerIndex = vertices.Count;
            vertices.Add(new Vector3(0f, y, 0f));
            for (int i = 0; i < Sides; i++)
            {
                float a = i * Mathf.PI * 2f / Sides;
                vertices.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
            }

            for (int i = 0; i < Sides; i++)
            {
                int a = centerIndex + 1 + i;
                int b = centerIndex + 1 + (i + 1) % Sides;
                if (up) { triangles.Add(centerIndex); triangles.Add(b); triangles.Add(a); }
                else { triangles.Add(centerIndex); triangles.Add(a); triangles.Add(b); }
            }
        }
    }
}
