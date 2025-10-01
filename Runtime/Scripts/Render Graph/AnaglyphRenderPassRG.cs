// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
	public sealed partial class AnaglyphRenderPassRG : ScriptableRenderPass {
		public Settings settings;

		[Obsolete]
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
			=> base.Execute(context, ref renderingData);

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
			const string passName = "Anaglyph Copy To Debug Texture";

			using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData)) {
				UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

				passData.inputTexture = resourceData.activeColorTexture;

				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
				desc.msaaSamples = 1;
				desc.depthBufferBits = 0;

				TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
					renderGraph: renderGraph,
					desc: desc,
					name: "CopyTexture",
					clear: false
				);

				builder.UseTexture(passData.inputTexture);
				builder.SetRenderAttachment(destination, 0);
				builder.AllowPassCulling(false); // temporary to make sure RenderGraph doesn't discard the unused result
				builder.SetRenderFunc<PassData>(ExecutePass);
			}
		}

		private static void ExecutePass(PassData passData, RasterGraphContext context) {
			Blitter.BlitTexture(
				cmd: context.cmd,
				source: passData.inputTexture,
				scaleBias: new Vector4(1, 1, 0, 0), mipLevel: 0, bilinear: false
			);
		}
	}
}