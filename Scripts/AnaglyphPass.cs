// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    internal sealed class AnaglyphPass : ScriptableRenderPass {
        private static readonly string[] RenderTargetNames = new string[2]  {
            "_LeftTex",
            "_RightTex"
        };
        private static readonly string IntermediateTargetName = "_IntermediateTex";

        private RTHandle cameraColorTargetHandle;

        private RTHandle intermediateTargetHandle = null;
        private RTHandleGroup[] renderTargetHandles = null;

        private List<ShaderTagId> shaderTagIDs = new List<ShaderTagId>();

        private FilteringSettings filteringSettings;
        private RenderStateBlock renderStateBlock;

        private Settings settings;
        private Matrix4x4[] offsetMatrices = null;

        internal Material Material => settings.Material;

        private LocalKeyword opacityModeAdditiveKeyword;
        private LocalKeyword opacityModeChannelKeyword;
        private LocalKeyword singleChannelKeyword;
        private LocalKeyword overlayEffectKeyword;

        public AnaglyphPass(Settings settings, string tag) {
            profilingSampler = new ProfilingSampler(tag);
            filteringSettings = new FilteringSettings(RenderQueueRange.all, settings.layerMask);

            shaderTagIDs.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagIDs.Add(new ShaderTagId("UniversalForward"));
            shaderTagIDs.Add(new ShaderTagId("UniversalForwardOnly"));
            shaderTagIDs.Add(new ShaderTagId("LightweightForward"));

            renderStateBlock = new RenderStateBlock(RenderStateMask.Raster);

            this.renderPassEvent = settings.renderPassEvent;
            this.settings = settings;

            offsetMatrices = new Matrix4x4[2];
            renderTargetHandles = new RTHandleGroup[2];

            opacityModeAdditiveKeyword = new LocalKeyword(Material.shader, "_OPACITY_MODE_ADDITIVE");
            opacityModeChannelKeyword = new LocalKeyword(Material.shader, "_OPACITY_MODE_CHANNEL");
            singleChannelKeyword = new LocalKeyword(Material.shader, "_SINGLE_CHANNEL");
            overlayEffectKeyword = new LocalKeyword(Material.shader, "_OVERLAY_EFFECT");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0; // Color and depth cannot be combined in RTHandles

            for (int i = 0; i < renderTargetHandles.Length; i++) {
                RenderingUtils.ReAllocateIfNeeded(ref renderTargetHandles[i].color, Vector2.one, descriptor, name: AnaglyphPass.RenderTargetNames[i]);
                RenderingUtils.ReAllocateIfNeeded(ref renderTargetHandles[i].depth, Vector2.one, descriptor);

                ConfigureTarget(renderTargetHandles[i].color, renderTargetHandles[i].depth);
            }
            RenderingUtils.ReAllocateIfNeeded(ref intermediateTargetHandle, Vector2.one, descriptor, name: AnaglyphPass.IntermediateTargetName);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            Material.SetKeyword(opacityModeAdditiveKeyword, settings.opacityMode == Settings.OpacityMode.Additive);
            Material.SetKeyword(opacityModeChannelKeyword, settings.opacityMode == Settings.OpacityMode.Channel);
            Material.SetKeyword(singleChannelKeyword, settings.TextureCount == 1);
            Material.SetKeyword(overlayEffectKeyword, settings.overlayEffect);

            Extensions.CreateOffsetMatrix(settings.spacing, settings.lookTarget, -1, ref offsetMatrices[0]);
            Extensions.CreateOffsetMatrix(settings.spacing, settings.lookTarget, 1, ref offsetMatrices[1]);
        }

        public void Setup(RTHandle cameraColorTargetHandle) {
            this.cameraColorTargetHandle = cameraColorTargetHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            SortingCriteria sortingCriteria = SortingCriteria.RenderQueue;
            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIDs, ref renderingData, sortingCriteria);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler)) {
                ScriptableRenderer renderer = renderingData.cameraData.renderer;
                Camera camera = renderingData.cameraData.camera;

                if (settings.TextureCount == 1) { // render only left channel
                    cmd.SetViewMatrix(camera.worldToCameraMatrix);

                    CoreUtils.SetRenderTarget(cmd, renderTargetHandles[0].color, renderTargetHandles[0].depth, ClearFlag.Color | ClearFlag.Depth, Color.clear);

                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
                } else { // render both channels
                    for (int i = 0; i < 2; i++) {
                        Matrix4x4 viewMatrix = offsetMatrices[i] * camera.worldToCameraMatrix;
                        cmd.SetViewMatrix(viewMatrix);

                        CoreUtils.SetRenderTarget(cmd, renderTargetHandles[i].color, renderTargetHandles[i].depth, ClearFlag.Color | ClearFlag.Depth, Color.clear);

                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
                    }
                }

                Blitter.BlitCameraTexture(cmd, cameraColorTargetHandle, intermediateTargetHandle, Material, pass: 0);
                Blitter.BlitCameraTexture(cmd, intermediateTargetHandle, cameraColorTargetHandle, Vector2.one);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            cameraColorTargetHandle = null;
        }

        public void Dispose() {
            intermediateTargetHandle?.Release();
            for (int i = 0; i < renderTargetHandles.Length; i++) {
                renderTargetHandles[i].Release();
            }
        }
    }
}
