// Made with love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine.Rendering.Universal;

namespace Anaglyph3D
{
    public class Anaglyph3DFeature : ScriptableRendererFeature
    {
        public Anaglyph3DSettings settings = new Anaglyph3DSettings();

        private Anaglyph3DPass anaglyphPass;

        public override void Create()
        {
            anaglyphPass = new Anaglyph3DPass(name, settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.anaglyphMaterial == null) { return; }

            renderer.EnqueuePass(anaglyphPass);
        }
    }
}