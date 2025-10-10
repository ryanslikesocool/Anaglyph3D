// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
	public sealed class AnaglyphFeature : ScriptableRendererFeature {
		// MARK: - Properties

		public Settings settings = new Settings();

		private AnaglyphPass pass;

		// MARK: - Setup

		public override void Create() {
			pass = new(settings, "Anaglyph");
		}

		// MARK: - Cleanup

		protected override void Dispose(bool disposing) {
			CoreUtils.Destroy(pass.material);
			pass.Release();
		}

		// MARK: -

#if UNITY_2023_3_OR_NEWER
		[Obsolete]
#endif
		public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) {
			pass.Setup(
				renderer.cameraColorTargetHandle,
				renderer.cameraDepthTargetHandle
			);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
			if (
				renderingData.cameraData.cameraType == CameraType.Preview
				|| pass == null
				|| settings.layerMask == 0
			) {
				return;
			}

			pass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);

			renderer.EnqueuePass(pass);
		}
	}
}