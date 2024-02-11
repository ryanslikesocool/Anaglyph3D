// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
	public sealed class AnaglyphFeature : ScriptableRendererFeature {
		public Settings settings = new Settings();

		private AnaglyphPass pass;

		public override void Create() {
			pass = new AnaglyphPass(settings, "Anaglyph");
		}

		public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) {
			pass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
			pass.Setup(
				renderer.cameraColorTargetHandle,
				renderer.cameraDepthTargetHandle
			);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
			if (renderingData.cameraData.isPreviewCamera) {
				return;
			}
			if (settings.layerMask == 0) {
				return;
			}

			renderer.EnqueuePass(pass);
		}

		protected override void Dispose(bool disposing) {
			CoreUtils.Destroy(pass.material);
			pass.Release();
		}
	}
}