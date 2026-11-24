using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// The lit shader for every Material created at runtime (track blocks,
    /// terrain clumps, marble). Shader.Find alone only works in the Editor:
    /// a player build (e.g. WebGL) strips every shader no asset in the build
    /// references, so the runtime materials came out magenta / missing.
    /// Resources/RuntimeLit.mat references URP/Lit, which forces the shader
    /// (incl. its instancing variants) into the build.
    /// </summary>
    public static class RuntimeShaders
    {
        private static Shader lit;

        public static Shader Lit
        {
            get
            {
                if (lit != null) return lit;

                Material template = Resources.Load<Material>("RuntimeLit");
                if (template != null) lit = template.shader;
                if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) lit = Shader.Find("Standard");
                if (lit == null) Debug.LogError("RuntimeShaders: no lit shader found - is Resources/RuntimeLit.mat missing?");
                return lit;
            }
        }
    }
}
