// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    public sealed class AnaglyphFeature : ScriptableRendererFeature {
        public Settings settings = new Settings();

        private AnaglyphPass pass;

        public override void Create() {
            if (settings.material == null && settings.shader != null) {
                settings.material = CoreUtils.CreateEngineMaterial(settings.shader);
            }

            pass = new AnaglyphPass(settings, "Anaglyph");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (renderingData.cameraData.isPreviewCamera || renderingData.cameraData.isSceneViewCamera) {
                return;
            }

            if (pass.Material == null || settings.layerMask == 0) {
                return;
            }

            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing) {
            CoreUtils.Destroy(settings.material);
            pass.Release();
        }
    }
}