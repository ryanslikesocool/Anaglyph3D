// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    public class Anaglyph3DFeature : ScriptableRendererFeature {
        public Anaglyph3DSettings settings = new Anaglyph3DSettings();

        private Anaglyph3DPass anaglyphPass;

        public override void Create() {
            anaglyphPass = new Anaglyph3DPass(name, settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (settings.anaglyphMaterial == null) {
                settings.anaglyphMaterial = new Material(Shader.Find("RenderFeature/Anaglyph3D"));
            }

            renderer.EnqueuePass(anaglyphPass);
        }
    }
}