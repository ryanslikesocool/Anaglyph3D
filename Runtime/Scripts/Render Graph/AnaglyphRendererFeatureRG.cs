// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
	public sealed class AnaglyphRendererFeatureRG : ScriptableRendererFeature {
		public Settings settings = new Settings();

		private AnaglyphRenderPassRG renderPass;

		public override void Create() {
			renderPass = new() {
				settings = this.settings,
			};
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
			if (
				renderingData.cameraData.cameraType == CameraType.Preview
				|| renderPass == null
				|| settings.layerMask == 0
			) {
				return;
			}

			renderer.EnqueuePass(renderPass);
		}
	}
}