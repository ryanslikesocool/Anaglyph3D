// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    public class AnaglyphFeature : ScriptableRendererFeature {
        public Settings settings = new Settings();

        private AnaglyphPass pass;

        public override void Create() {
            pass = new AnaglyphPass(settings, name);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            renderer.EnqueuePass(pass);
        }
    }
}