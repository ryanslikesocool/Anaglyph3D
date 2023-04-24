// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

#if !(UNITY_IOS || UNITY_TVOS)
#define ANAGLYPH_INTERMEDIATE_TEXTURE
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    internal sealed class AnaglyphPass : ScriptableRenderPass {
        private static readonly string[] RenderTargetColorNames = new string[2]  {
            "_AnaglyphLeft",
            "_AnaglyphRight"
        };
        private static readonly string[] RenderTargetDepthNames = new string[2] {
            "_AnaglyphLeftDepth",
            "_AnaglyphRightDepth"
        };

        private RTHandle cameraTargetHandle = null;
        private RTHandleGroup[] renderTargetHandles = null;

        //#if ANAGLYPH_INTERMEDIATE_TEXTURE
        private static readonly string IntermediateTargetName = "_AnaglyphIntermediate";
        private RTHandle intermediateTargetHandle = null;
        //#endif

        private List<ShaderTagId> shaderTagsList = new List<ShaderTagId>();

        private FilteringSettings filteringSettings;
        private RenderStateBlock renderStateBlock;

        private Settings settings;
        private Matrix4x4[] offsetMatrices = null;

        internal Material Material => settings.material;

        private LocalKeyword singleChannelKeyword;
        private LocalKeyword overlayModeOpacityKeyword;
        private LocalKeyword overlayModeDepthKeyword;
        private LocalKeyword blendModeAdditiveKeyword;
        private LocalKeyword blendModeChannelKeyword;

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

            singleChannelKeyword = new LocalKeyword(Material.shader, "_SINGLE_CHANNEL");
            overlayModeOpacityKeyword = new LocalKeyword(Material.shader, "_OVERLAY_MODE_OPACITY");
            overlayModeDepthKeyword = new LocalKeyword(Material.shader, "_OVERLAY_MODE_DEPTH");
            blendModeAdditiveKeyword = new LocalKeyword(Material.shader, "_BLEND_MODE_ADDITIVE");
            blendModeChannelKeyword = new LocalKeyword(Material.shader, "_BLEND_MODE_CHANNEL");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            RenderTextureDescriptor colorDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            RenderTextureDescriptor depthDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            colorDescriptor.depthBufferBits = 0;  // Color and depth cannot be combined in RTHandles

            cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

            //#if ANAGLYPH_INTERMEDIATE_TEXTURE
            RenderingUtils.ReAllocateIfNeeded(ref intermediateTargetHandle, colorDescriptor, name: AnaglyphPass.IntermediateTargetName);
            ConfigureTarget(intermediateTargetHandle);
            //#endif

            for (int i = 0; i < renderTargetHandles.Length; i++) {
                RenderingUtils.ReAllocateIfNeeded(ref renderTargetHandles[i].color, colorDescriptor, name: AnaglyphPass.RenderTargetColorNames[i]);
                RenderingUtils.ReAllocateIfNeeded(ref renderTargetHandles[i].depth, depthDescriptor, name: AnaglyphPass.RenderTargetDepthNames[i]);

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

                cmd.SetKeyword(Material, singleChannelKeyword, settings.SingleChannel);
                cmd.SetKeyword(Material, overlayModeOpacityKeyword, settings.overlayMode == Settings.OverlayMode.Opacity);
                cmd.SetKeyword(Material, overlayModeDepthKeyword, settings.overlayMode == Settings.OverlayMode.Depth);
                cmd.SetKeyword(Material, blendModeAdditiveKeyword, settings.blendMode == Settings.BlendMode.Additive);
                cmd.SetKeyword(Material, blendModeChannelKeyword, settings.blendMode == Settings.BlendMode.Channel);

                if (settings.SingleChannel) { // render only left channel
                    Draw(camera.worldToCameraMatrix, RenderTargetColorNames[0], RenderTargetDepthNames[0], renderTargetHandles[0], ref renderingData, ref drawingSettings);
                } else { // render both channels
                    for (int i = 0; i < renderTargetHandles.Length; i++) {
                        Matrix4x4 viewMatrix = offsetMatrices[i] * camera.worldToCameraMatrix;
                        Draw(viewMatrix, RenderTargetColorNames[i], RenderTargetDepthNames[i], renderTargetHandles[i], ref renderingData, ref drawingSettings);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                //#if ANAGLYPH_INTERMEDIATE_TEXTURE
                Blitter.BlitCameraTexture(cmd, cameraTargetHandle, intermediateTargetHandle, Material, 0);
                Blitter.BlitCameraTexture(cmd, intermediateTargetHandle, cameraTargetHandle);
                //#else
                //Blitter.BlitCameraTexture(cmd, cameraTargetHandle, cameraTargetHandle, Material, 0);
                //#endif
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);

            void Draw(in Matrix4x4 matrix, in string colorTarget, in string depthTarget, in RTHandleGroup handle, ref RenderingData renderingData, ref DrawingSettings drawingSettings) {
                cmd.SetViewMatrix(matrix);

                CoreUtils.SetRenderTarget(
                    cmd: cmd,
                    colorBuffer: handle.color,
                    colorLoadAction: RenderBufferLoadAction.DontCare,
                    colorStoreAction: RenderBufferStoreAction.Store,
                    depthBuffer: handle.depth,
                    depthLoadAction: RenderBufferLoadAction.DontCare,
                    depthStoreAction: settings.NeedsDepth ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare,
                    ClearFlag.Color | ClearFlag.Depth,
                    Color.clear
                );

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

                cmd.SetGlobalTexture(colorTarget, handle.color);
                if (settings.NeedsDepth) {
                    cmd.SetGlobalTexture(depthTarget, handle.depth);
                }
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            cameraTargetHandle = null;

            //cmd.DisableKeyword(Material, singleChannelKeyword);
            //cmd.DisableKeyword(Material, overlayModeOpacityKeyword);
            //cmd.DisableKeyword(Material, overlayModeDepthKeyword);
            //cmd.DisableKeyword(Material, blendModeAdditiveKeyword);
            //cmd.DisableKeyword(Material, blendModeChannelKeyword);
        }

        public void Release() {
            //#if ANAGLYPH_INTERMEDIATE_TEXTURE
            intermediateTargetHandle?.Release();
            //#endif
            foreach (RTHandleGroup group in renderTargetHandles) {
                group.Release();
            }
        }
    }
}
