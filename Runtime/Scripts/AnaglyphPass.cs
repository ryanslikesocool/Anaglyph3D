// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

			// depth doesn't write if MSAA is on for some reason?
			colorDescriptor.depthBufferBits = 0;
			colorDescriptor.msaaSamples = 1;

			depthDescriptor.depthBufferBits = 32;
			depthDescriptor.msaaSamples = 1;

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
			Camera camera = renderingData.cameraData.camera;
			Matrix4x4 worldToCameraMatrix = camera.worldToCameraMatrix;

			CommandBuffer cmd = CommandBufferPool.Get();
			using (new ProfilingScope(cmd, profilingSampler)) {
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
				DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagsList, ref renderingData, sortingCriteria);

				cmd.SetKeyword(material, singleChannelKeyword, settings.SingleChannel);

				if (settings.SingleChannel) { // render only left channel using the current camera matrix
					Draw(null, RenderTargetColorNames[0], RenderTargetDepthNames[0], renderTargetHandles[0], ref renderingData, ref drawingSettings);
				} else { // render both channels
					for (int i = 0; i < 2; i++) {
						Matrix4x4 viewMatrix = offsetMatrices[i] * worldToCameraMatrix;
						Draw(viewMatrix, RenderTargetColorNames[i], RenderTargetDepthNames[i], renderTargetHandles[i], ref renderingData, ref drawingSettings);
					}
				}

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				cmd.SetRenderTarget(
					cameraTargetHandle.color, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
					cameraTargetHandle.depth, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
				);

				cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);

				// reset the camera matrix if it was changed
				if (!settings.SingleChannel) {
					cmd.SetViewMatrix(worldToCameraMatrix);
				}
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
					ClearFlag.Color | ClearFlag.Depth, Color.clear
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
