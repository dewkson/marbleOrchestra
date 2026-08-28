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

        /// 3D sphere marble for TrackTerrainGenerator's ribbon. With
        /// withPhysics=false the default primitive collider is removed
        /// (kinematic mode only ever sets transform.position directly).
        /// With withPhysics=true it keeps its SphereCollider and gets a
        /// gravity-driven Rigidbody so it can roll against the terrain's
        /// MeshCollider.
        public static Marble CreateSphere3D(Transform parent, float radius, Color color, bool withPhysics)
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
            }
            else
            {
                Object.Destroy(go.GetComponent<Collider>());
            }

            go.GetComponent<Renderer>().sharedMaterial = CreateSphereMaterial(color);

            return go.AddComponent<Marble>();
        }

        private static Material CreateSphereMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
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
