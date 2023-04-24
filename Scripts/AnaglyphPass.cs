// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    internal sealed class AnaglyphPass : ScriptableRenderPass {
        private static readonly string[] RenderTargetNames = new string[2]  {
            "_AnaglyphLeftTex",
            "_AnaglyphRightTex"
        };
        private static readonly string IntermediateTargetName = "_AnaglyphIntermediateTex";

        private RTHandle intermediateTargetHandle = null;
        private RTHandleGroup[] renderTargetHandles = null;

        private List<ShaderTagId> shaderTagsList = new List<ShaderTagId>();

        private FilteringSettings filteringSettings;
        private RenderStateBlock renderStateBlock;

        private Settings settings;
        private Matrix4x4[] offsetMatrices = null;

        internal Material Material => settings.material;

        private LocalKeyword opacityModeAdditiveKeyword;
        private LocalKeyword opacityModeChannelKeyword;
        private LocalKeyword singleChannelKeyword;
        private LocalKeyword overlayEffectKeyword;

        public AnaglyphPass(Settings settings, string tag) {
            profilingSampler = new ProfilingSampler(tag);
            filteringSettings = new FilteringSettings(RenderQueueRange.all, settings.layerMask);

            shaderTagsList.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagsList.Add(new ShaderTagId("UniversalForward"));
            shaderTagsList.Add(new ShaderTagId("UniversalForwardOnly"));

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
            RenderTextureDescriptor colorDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            RenderTextureDescriptor depthDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            colorDescriptor.depthBufferBits = 0;  // Color and depth cannot be combined in RTHandles
            colorDescriptor.colorFormat = RenderTextureFormat.BGRA32;

            RenderingUtils.ReAllocateIfNeeded(ref intermediateTargetHandle, colorDescriptor, name: AnaglyphPass.IntermediateTargetName);
            ConfigureTarget(intermediateTargetHandle);

            for (int i = 0; i < renderTargetHandles.Length; i++) {
                RenderingUtils.ReAllocateIfNeeded(ref renderTargetHandles[i].color, colorDescriptor, name: AnaglyphPass.RenderTargetNames[i]);
                RenderingUtils.ReAllocateIfNeeded(ref renderTargetHandles[i].depth, depthDescriptor);

                ConfigureTarget(renderTargetHandles[i].color, renderTargetHandles[i].depth);
            }

            // ---

            Extensions.CreateOffsetMatrix(settings.spacing, settings.lookTarget, -1, ref offsetMatrices[0]);
            Extensions.CreateOffsetMatrix(settings.spacing, settings.lookTarget, 1, ref offsetMatrices[1]);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler)) {
                ScriptableRenderer renderer = renderingData.cameraData.renderer;
                Camera camera = renderingData.cameraData.camera;

                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagsList, ref renderingData, sortingCriteria);

                cmd.SetKeyword(Material, opacityModeAdditiveKeyword, settings.opacityMode == Settings.OpacityMode.Additive);
                cmd.SetKeyword(Material, opacityModeChannelKeyword, settings.opacityMode == Settings.OpacityMode.Channel);
                cmd.SetKeyword(Material, singleChannelKeyword, settings.SingleChannel);
                cmd.SetKeyword(Material, overlayEffectKeyword, settings.overlayEffect);

                if (settings.SingleChannel) { // render only left channel
                    Draw(camera.worldToCameraMatrix, RenderTargetNames[0], renderTargetHandles[0], ref renderingData, ref drawingSettings);
                } else { // render both channels
                    for (int i = 0; i < renderTargetHandles.Length; i++) {
                        Matrix4x4 viewMatrix = offsetMatrices[i] * camera.worldToCameraMatrix;
                        Draw(viewMatrix, RenderTargetNames[i], renderTargetHandles[i], ref renderingData, ref drawingSettings);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Blitter.BlitCameraTexture(cmd, renderer.cameraColorTargetHandle, intermediateTargetHandle, Material, 0);
                Blitter.BlitCameraTexture(cmd, intermediateTargetHandle, renderer.cameraColorTargetHandle);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);

            void Draw(in Matrix4x4 matrix, in string target, in RTHandleGroup handle, ref RenderingData renderingData, ref DrawingSettings drawingSettings) {
                cmd.SetViewMatrix(matrix);

                CoreUtils.SetRenderTarget(cmd, handle.color, handle.depth, ClearFlag.Color | ClearFlag.Depth, Color.clear);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

                cmd.SetGlobalTexture(target, handle.color);
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            cmd.DisableKeyword(Material, opacityModeAdditiveKeyword);
            cmd.DisableKeyword(Material, opacityModeChannelKeyword);
            cmd.DisableKeyword(Material, singleChannelKeyword);
            cmd.DisableKeyword(Material, overlayEffectKeyword);
        }

        public void Release() {
            intermediateTargetHandle?.Release();
            foreach (RTHandleGroup group in renderTargetHandles) {
                group.Release();
            }
        }
    }
}
