using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Purely visual marker for the marble. Movement logic lives in MarbleController.
    /// </summary>
    public class Marble : MonoBehaviour
    {
        private static Sprite circleSprite;

        public static Marble Create(Transform parent, float radius, Color color)
        {
            GameObject go = new GameObject("Marble");
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 3;

            return go.AddComponent<Marble>();
        }

        /// 3D sphere marble for TrackBlockSpawner's blocks. With
        /// withPhysics=false the default primitive collider is removed
        /// (kinematic mode only ever sets transform.position directly).
        /// With withPhysics=true it keeps its SphereCollider and gets a
        /// gravity-driven Rigidbody so it can roll against the terrain's
        /// MeshCollider.
        public static Marble CreateSphere3D(Transform parent, float radius, bool withPhysics)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Marble3D";
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * (radius * 2f);

            if (withPhysics)
            {
                Rigidbody rb = go.AddComponent<Rigidbody>();
                rb.useGravity = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                // Blocks are separate, thinner mesh pieces with a real step
                // (and, deliberately, a short fall) at each boundary rather
                // than one continuous mesh - Continuous avoids tunneling
                // through a block's thin edge at those higher relative speeds.
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                go.GetComponent<Collider>().sharedMaterial = LowFrictionMaterial();
            }
            else
            {
                Object.Destroy(go.GetComponent<Collider>());
            }

            go.GetComponent<Renderer>().sharedMaterial = CreateSphereMaterial();

            return go.AddComponent<Marble>();
        }

        private static PhysicsMaterial lowFrictionMaterial;

        /// Matches TrackBlock's own surface PhysicsMaterial (low friction,
        /// no bounce) so the marble rolls smoothly instead of catching or
        /// bouncing at a block boundary.
        private static PhysicsMaterial LowFrictionMaterial()
        {
            if (lowFrictionMaterial == null)
            {
                lowFrictionMaterial = new PhysicsMaterial("MarbleSurface")
                {
                    dynamicFriction = 0.05f,
                    staticFriction = 0.05f,
                    bounciness = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
            }
            return lowFrictionMaterial;
        }

        private static readonly Color WoodColor = new Color(0.52f, 0.35f, 0.20f);

        /// Wood-look material for the 3D marble: a flat wood-brown base
        /// color with a satin (not glossy plastic, not metallic) finish.
        private static Material CreateSphereMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material material = new Material(shader);

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", WoodColor);
            else material.color = WoodColor;

            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.35f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.35f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);

            return material;
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null) return circleSprite;

            const int size = 32;
            Texture2D texture = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
            Vector2 center = new Vector2(size - 1, size - 1) * 0.5f;
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, dist <= radius ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }
            texture.Apply();

            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return circleSprite;
        }
    }
}
