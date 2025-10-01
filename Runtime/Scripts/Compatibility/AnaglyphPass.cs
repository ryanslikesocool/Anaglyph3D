// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
	internal sealed partial class AnaglyphPass : ScriptableRenderPass {
		// MARK: - Constants

		private const string SHADER_NAME = "Hidden/RenderFeature/Anaglyph/Main";

		private static readonly string[] RenderTargetColorNames = new string[2]  {
			"_AnaglyphLeft",
			"_AnaglyphRight",
		};
		private static readonly string[] RenderTargetDepthNames = new string[2] {
			"_AnaglyphLeftDepth",
			"_AnaglyphRightDepth",
		};
		private static readonly int[] RenderTargetColorIDs = new int[2] {
			Shader.PropertyToID(RenderTargetColorNames[0]),
			Shader.PropertyToID(RenderTargetColorNames[1]),
		};
		private static readonly int[] RenderTargetDepthIDs = new int[2] {
			Shader.PropertyToID(RenderTargetDepthNames[0]),
			Shader.PropertyToID(RenderTargetDepthNames[1]),
		};

		private const string RenderPassNameFormat = "Anaglyph Render Eye {0}";
		private const string CombinePassName = "Anaglyph Combine Eyes";

		// MARK: - Properties

		private RTHandleGroup cameraTargetHandle = default;
		private RTHandleGroup[] renderTargetHandles = null;
		private TextureHandleGroup[] textureHandles = null;

		private List<ShaderTagId> shaderTagsList = new List<ShaderTagId>();

		private FilteringSettings filteringSettings;
		private RenderStateBlock renderStateBlock;
		private Matrix4x4[] offsetViewMatrices = null;
		private readonly LocalKeyword singleChannelKeyword;

		internal readonly Material material;
		private readonly Settings settings;

		// MARK: - Setup

		public AnaglyphPass(in Settings settings, in string tag) {
			profilingSampler = new ProfilingSampler(tag);
			material = CoreUtils.CreateEngineMaterial(SHADER_NAME);

			filteringSettings = new FilteringSettings(settings.QueueRange, settings.layerMask);
			renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

			shaderTagsList.Add(new ShaderTagId("SRPDefaultUnlit"));
			shaderTagsList.Add(new ShaderTagId("UniversalForward"));
			shaderTagsList.Add(new ShaderTagId("UniversalForwardOnly"));

			this.renderPassEvent = settings.renderPassEvent;
			this.settings = settings;

			offsetViewMatrices = new Matrix4x4[2];
			renderTargetHandles = new RTHandleGroup[2];
			textureHandles = new TextureHandleGroup[2];

			singleChannelKeyword = new LocalKeyword(material.shader, "_ANAGLYPH_SINGLE_CHANNEL");
		}

#if UNITY_2023_3_OR_NEWER
		[Obsolete]
#endif
		public void Setup(RTHandle color, RTHandle depth) {
			cameraTargetHandle.color = color;
			cameraTargetHandle.depth = depth;
		}

		// MARK: - Cleanup

		public override void OnCameraCleanup(CommandBuffer cmd) {
			cameraTargetHandle = default;

			cmd.DisableKeyword(material, singleChannelKeyword);
		}

		public void Release() {
			for (int i = 0; i < renderTargetHandles.Length; i++) {
				renderTargetHandles[i].Release();
			}
		}

		// MARK: - Compatibility

#if UNITY_2023_3_OR_NEWER
		[Obsolete]
#endif
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
			RenderTextureDescriptor colorDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			RenderTextureDescriptor depthDescriptor = renderingData.cameraData.cameraTargetDescriptor;

			// depth doesn't write if MSAA is on for some reason?
			colorDescriptor.depthBufferBits = 0;
			colorDescriptor.msaaSamples = 1;

			depthDescriptor.depthBufferBits = 32;
			depthDescriptor.msaaSamples = 1;

			for (int i = 0; i < renderTargetHandles.Length; i++) {
				RenderingUtils.ReAllocateIfNeeded(ref renderTargetHandles[i].color, colorDescriptor, name: RenderTargetColorNames[i]);
				RenderingUtils.ReAllocateIfNeeded(ref renderTargetHandles[i].depth, depthDescriptor, name: RenderTargetDepthNames[i]);
				ConfigureTarget(renderTargetHandles[i].color, renderTargetHandles[i].depth);

				offsetViewMatrices[i] = Extensions.CreateOffsetMatrix(settings.spacing, settings.focalPoint, i - 0.5f);
			}

			ConfigureTarget(cameraTargetHandle.color, cameraTargetHandle.depth);
		}

#if UNITY_2023_3_OR_NEWER
		[Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
			if (material == null) {
				Debug.LogError($"Cannot execute {this}.  Missing material with shader {SHADER_NAME}");
				return;
			}

			Camera camera = renderingData.cameraData.camera;
			Matrix4x4 cameraViewMatrix = camera.worldToCameraMatrix;

			CommandBuffer cmd = CommandBufferPool.Get();
			using (new ProfilingScope(cmd, profilingSampler)) {
				SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
				DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagsList, ref renderingData, sortingCriteria);

				cmd.SetKeyword(material, singleChannelKeyword, settings.SingleChannel);

				if (settings.SingleChannel) { // render only left channel using the current camera matrix
					Draw(null, 0, ref renderingData, ref drawingSettings);
				} else { // render both channels
					for (int i = 0; i < 2; i++) {
						Matrix4x4 viewMatrix = offsetViewMatrices[i] * cameraViewMatrix;
						Draw(viewMatrix, i, ref renderingData, ref drawingSettings);
					}
				}

				cmd.SetRenderTarget(
					cameraTargetHandle.color, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
					cameraTargetHandle.depth, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
				);

				cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);

				// reset the camera matrix if it was changed
				if (!settings.SingleChannel) {
					cmd.SetViewMatrix(cameraViewMatrix);
				}
			}

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
			CommandBufferPool.Release(cmd);

			void Draw(in Matrix4x4? viewMatrix, int eyeIndex, ref RenderingData renderingData, ref DrawingSettings drawingSettings) {
				if (viewMatrix.HasValue) {
					cmd.SetViewMatrix(viewMatrix.Value);
				}

				RTHandleGroup targets = renderTargetHandles[eyeIndex];

				CoreUtils.SetRenderTarget(
					cmd,
					targets.color, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
					targets.depth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
					ClearFlag.Color | ClearFlag.Depth, Color.clear
				);

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

				//cmd.SetGlobalTexture(RenderTargetColorIDs[eyeIndex], targets.color);
				//cmd.SetGlobalTexture(RenderTargetDepthIDs[eyeIndex], targets.depth);
				cmd.SetGlobalTexture(RenderTargetColorNames[eyeIndex], targets.color);
				cmd.SetGlobalTexture(RenderTargetDepthNames[eyeIndex], targets.depth);
			}
		}

		// MARK: - Render Graph

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
			Debug.Assert(offsetViewMatrices != null);
			Debug.Assert(textureHandles != null);

			UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
			UniversalLightData lightData = frameData.Get<UniversalLightData>();
			UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();

			// TODO: validate if this is still true
			// depth doesn't write if MSAA is on for some reason?

			var colorDescriptor = renderGraph.GetTextureDesc(resourceData.cameraColor);
			colorDescriptor.msaaSamples = MSAASamples.None;
			colorDescriptor.depthBufferBits = DepthBits.None;
			colorDescriptor.clearBuffer = false;

			var depthDescriptor = renderGraph.GetTextureDesc(resourceData.cameraDepth);
			depthDescriptor.msaaSamples = MSAASamples.None;
			depthDescriptor.depthBufferBits = DepthBits.Depth32;
			depthDescriptor.clearBuffer = false;

			Matrix4x4 cameraViewMatrix = cameraData.GetViewMatrix();
			Matrix4x4 cameraProjectionMatrix = cameraData.GetProjectionMatrix();

			for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++) {
				colorDescriptor.name = RenderTargetColorNames[eyeIndex];
				depthDescriptor.name = RenderTargetDepthNames[eyeIndex];

				textureHandles[eyeIndex] = new(
					renderGraph.CreateTexture(colorDescriptor),
					renderGraph.CreateTexture(depthDescriptor)
				);

				offsetViewMatrices[eyeIndex] = Extensions.CreateOffsetMatrix(settings.spacing, settings.focalPoint, eyeIndex - 0.5f);
			}

			SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;
			DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagsList, renderingData, cameraData, lightData, sortingCriteria);
			RendererListParams rendererListParams = new(renderingData.cullResults, drawingSettings, filteringSettings);

			if (settings.SingleChannel) { // render only left eyes using the current camera matrix
				EnqueueEyePass(null, 0);
			} else { // render both eyes
				for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++) {
					EnqueueEyePass(offsetViewMatrices[eyeIndex], eyeIndex);
				}
			}
			EnqueueCombinePass();

			void EnqueueEyePass(Matrix4x4? viewMatrixOffset, int eyeIndex) {
				string passName = string.Format(RenderPassNameFormat, eyeIndex);

				using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler)) {
					var rendererList = renderGraph.CreateRendererList(rendererListParams); // Renderer lists can't be reused.

					passData.rendererList = rendererList;

					if (viewMatrixOffset.HasValue) {
						passData.viewMatrix = viewMatrixOffset.Value * cameraViewMatrix;
					} else {
						passData.viewMatrix = cameraViewMatrix;
					}

					passData.projectionMatrix = cameraProjectionMatrix;

					builder.UseRendererList(rendererList);
					builder.AllowGlobalStateModification(true);
					builder.AllowPassCulling(false); // temporary for debugging

					builder.SetRenderAttachment(textureHandles[eyeIndex].color, 0, flags: AccessFlags.Write);
					builder.SetRenderAttachmentDepth(textureHandles[eyeIndex].depth, flags: AccessFlags.Write);

					builder.SetGlobalTextureAfterPass(textureHandles[eyeIndex].color, RenderTargetColorIDs[eyeIndex]);
					builder.SetGlobalTextureAfterPass(textureHandles[eyeIndex].depth, RenderTargetDepthIDs[eyeIndex]);

					builder.SetRenderFunc<PassData>(ExecuteRenderPass);
				}
			}

			void EnqueueCombinePass() {
				using (var builder = renderGraph.AddRasterRenderPass<CombinePassData>(CombinePassName, out var passData, profilingSampler)) {
					//var destinationColorDescriptor = renderGraph.GetTextureDesc(resourceData.cameraColor);
					//destinationColorDescriptor.msaaSamples = MSAASamples.None;
					//destinationColorDescriptor.depthBufferBits = DepthBits.None;
					//destinationColorDescriptor.clearBuffer = false;
					//TextureHandle destinationColorTexture = renderGraph.CreateTexture(destinationColorDescriptor);

					//var destinationDepthDescriptor = renderGraph.GetTextureDesc(resourceData.cameraDepth);
					//destinationDepthDescriptor.msaaSamples = MSAASamples.None;
					//destinationDepthDescriptor.depthBufferBits = DepthBits.Depth32;
					//destinationDepthDescriptor.clearBuffer = false;
					//TextureHandle destinationDepthTexture = renderGraph.CreateTexture(destinationDepthDescriptor);

					passData.material = material;
					passData.singleChannelKeyword = singleChannelKeyword;
					passData.isSingleChannel = settings.SingleChannel;

					passData.textureHandles = textureHandles;

					passData.viewMatrix = cameraViewMatrix;
					passData.projectionMatrix = cameraProjectionMatrix;

					//builder.UseTexture(resourceData.cameraColor, flags: AccessFlags.ReadWrite);
					//builder.UseTexture(resourceData.cameraDepth, flags: AccessFlags.ReadWrite);
					for (int i = 0; i < textureHandles.Length; i++) {
						builder.UseGlobalTexture(RenderTargetColorIDs[i]);
						builder.UseGlobalTexture(RenderTargetDepthIDs[i]);
					}

					builder.AllowGlobalStateModification(true);
					builder.AllowPassCulling(false); // temporary for debugging

					builder.SetRenderAttachment(resourceData.cameraColor, 0, flags: AccessFlags.ReadWrite);
					builder.SetRenderAttachmentDepth(resourceData.cameraDepth, flags: AccessFlags.ReadWrite);

					builder.SetRenderFunc<CombinePassData>(ExecuteCombinePass);

					//resourceData.cameraColor = destinationColorTexture;
					//resourceData.cameraDepth = destinationDepthTexture;
				}
			}

			static void ExecuteRenderPass(PassData passData, RasterGraphContext context) {
				context.cmd.SetViewProjectionMatrices(
					view: passData.viewMatrix,
					proj: passData.projectionMatrix
				);

				context.cmd.DrawRendererList(passData.rendererList);
			}

			static void ExecuteCombinePass(CombinePassData passData, RasterGraphContext context) {
				context.cmd.SetKeyword(passData.material, passData.singleChannelKeyword, passData.isSingleChannel);

				context.cmd.DrawProcedural(Matrix4x4.identity, passData.material, 0, MeshTopology.Triangles, 3);

				// reset the camera matrix if it was changed
				if (!passData.isSingleChannel) {
					context.cmd.SetViewProjectionMatrices(
						view: passData.viewMatrix,
						proj: passData.projectionMatrix
					);
				}
			}
		}
	}
}
