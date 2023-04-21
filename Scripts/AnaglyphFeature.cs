// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    public sealed class AnaglyphFeature : ScriptableRendererFeature {
        public Settings settings = new Settings();

        private AnaglyphPass pass;

        public override void Create() {
            pass = new AnaglyphPass(settings, "Anaglyph");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (pass.Material == null || settings.layerMask == 0) {
                return;
            }

            renderer.EnqueuePass(pass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) {
            pass.Setup(renderer.cameraColorTargetHandle);
        }

        protected override void Dispose(bool disposing) {
            CoreUtils.Destroy(settings.Material);
            pass.Dispose();
        }
    }
}