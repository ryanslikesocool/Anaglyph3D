// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

#if !(UNITY_IOS || UNITY_TVOS)
#define ANAGLYPH_INTERMEDIATE_TEXTURE
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;

namespace Anaglyph3D {
	internal sealed class AnaglyphPass : ScriptableRenderPass {
		private const string SHADER_NAME = "Render Feature/Anaglyph";

		private static readonly string[] RenderTargetColorNames = new string[2]  {
			"_AnaglyphLeft",
			"_AnaglyphRight"
		};
		private static readonly string[] RenderTargetDepthNames = new string[2] {
			"_AnaglyphLeftDepth",
			"_AnaglyphRightDepth"
		};
		private static readonly string IntermediateTargetColorName = "_AnaglyphIntermediateColor";
		private static readonly string IntermediateTargetDepthName = "_AnaglyphIntermediateDepth";

		private RTHandleGroup cameraTargetHandle = default;
		private RTHandleGroup intermediateTargetHandle = default;
		private RTHandleGroup[] renderTargetHandles = null;

		private List<ShaderTagId> shaderTagsList = new List<ShaderTagId>();

		private FilteringSettings filteringSettings;
		private RenderStateBlock renderStateBlock;

		private Matrix4x4[] offsetMatrices = null;

		private LocalKeyword singleChannelKeyword;

		internal Material material = null;
		private Settings settings;

		public AnaglyphPass(Settings settings, string tag) {
			profilingSampler = new ProfilingSampler(tag);
			material = CoreUtils.CreateEngineMaterial(SHADER_NAME);

			filteringSettings = new FilteringSettings(settings.QueueRange, settings.layerMask);
			renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

			shaderTagsList.Add(new ShaderTagId("SRPDefaultUnlit"));
			shaderTagsList.Add(new ShaderTagId("UniversalForward"));
			shaderTagsList.Add(new ShaderTagId("UniversalForwardOnly"));

			this.renderPassEvent = settings.renderPassEvent;
			this.settings = settings;

			offsetMatrices = new Matrix4x4[2];
			renderTargetHandles = new RTHandleGroup[2];

			singleChannelKeyword = new LocalKeyword(material.shader, "_ANAGLYPH_SINGLE_CHANNEL");
		}

		public void Setup(RTHandle color, RTHandle depth) {
			cameraTargetHandle.color = color;
			cameraTargetHandle.depth = depth;
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {

			RenderTextureDescriptor colorDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			RenderTextureDescriptor depthDescriptor = renderingData.cameraData.cameraTargetDescriptor;

			colorDescriptor.depthBufferBits = 0;
			depthDescriptor.depthBufferBits = 32;

			RenderingUtils.ReAllocateIfNeeded(ref intermediateTargetHandle.color, colorDescriptor, name: IntermediateTargetColorName);
			RenderingUtils.ReAllocateIfNeeded(ref intermediateTargetHandle.depth, depthDescriptor, name: IntermediateTargetDepthName);
			ConfigureTarget(intermediateTargetHandle.color, intermediateTargetHandle.depth);

			for (int i = 0; i < renderTargetHandles.Length; i++) {
				RenderingUtils.ReAllocateIfNeeded(ref renderTargetHandles[i].color, colorDescriptor, name: RenderTargetColorNames[i]);
				RenderingUtils.ReAllocateIfNeeded(ref renderTargetHandles[i].depth, depthDescriptor, name: RenderTargetDepthNames[i]);
				ConfigureTarget(renderTargetHandles[i].color, renderTargetHandles[i].depth);
			}

			ConfigureTarget(cameraTargetHandle.color, cameraTargetHandle.depth);

			for (int i = 0; i < 2; i++) {
				Extensions.CreateOffsetMatrix(settings.spacing, settings.focalPoint, i - 0.5f, ref offsetMatrices[i]);
			}
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
			CommandBuffer cmd = CommandBufferPool.Get();
			using (new ProfilingScope(cmd, profilingSampler)) {
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				ScriptableRenderer renderer = renderingData.cameraData.renderer;
				Camera camera = renderingData.cameraData.camera;

				SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
				DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagsList, ref renderingData, sortingCriteria);

				cmd.SetKeyword(material, singleChannelKeyword, settings.SingleChannel);

				if (settings.SingleChannel) { // render only left channel
					Draw(null, RenderTargetColorNames[0], RenderTargetDepthNames[0], renderTargetHandles[0], ref renderingData, ref drawingSettings);
				} else { // render both channels
					for (int i = 0; i < 2; i++) {
						Matrix4x4 viewMatrix = offsetMatrices[i] * camera.worldToCameraMatrix;

						Draw(viewMatrix, RenderTargetColorNames[i], RenderTargetDepthNames[i], renderTargetHandles[i], ref renderingData, ref drawingSettings);
					}
				}

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				Blitter.BlitCameraTexture(cmd, cameraTargetHandle.color, intermediateTargetHandle.color, material, 0);
				Blitter.BlitCameraTexture(cmd, intermediateTargetHandle.color, cameraTargetHandle.color);

				//context.ExecuteCommandBuffer(cmd);
				//cmd.Clear();
			}

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
			CommandBufferPool.Release(cmd);

			void Draw(in Matrix4x4? viewMatrix, in string colorTargetName, in string depthTargetName, RTHandleGroup targets, ref RenderingData renderingData, ref DrawingSettings drawingSettings) {
				if (viewMatrix.HasValue) {
					cmd.SetViewMatrix(viewMatrix.Value);
				}

				CoreUtils.SetRenderTarget(
					cmd,
					targets.color, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
					targets.depth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
					ClearFlag.Color | ClearFlag.Depth, Color.black
				);

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

				cmd.SetGlobalTexture(colorTargetName, targets.color);
				cmd.SetGlobalTexture(depthTargetName, targets.depth);
			}
		}

		public override void OnCameraCleanup(CommandBuffer cmd) {
			cameraTargetHandle = default;

			cmd.DisableKeyword(material, singleChannelKeyword);
		}

		public void Release() {
			intermediateTargetHandle.Release();
			for (int i = 0; i < renderTargetHandles.Length; i++) {
				renderTargetHandles[i].Release();
			}
		}
	}
}
