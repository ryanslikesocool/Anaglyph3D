// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    internal sealed class AnaglyphPass : ScriptableRenderPass {
        //private static readonly int[] RenderTargetIDs = new int[2] {
        //    Shader.PropertyToID("_LeftTex"),
        //    Shader.PropertyToID("_RightTex")
        //};
        private static readonly string[] RenderTargetNames = new string[] {
            "_LeftTex",
            "_RightTex"
        };

#if !UNITY_IOS && !UNITY_TVOS
        //private static readonly int IntermediateRenderTargetID = Shader.PropertyToID("_IntermediateRenderTarget");
        private static readonly string IntermediateRenderTargetName = "_IntermediateRenderTarget";
        private RTHandle intermediate;
#endif

        private RTHandle source;
        private RTHandle destination;

        private List<ShaderTagId> shaderTagIDs = new List<ShaderTagId>();

        private RTHandle[] renderHandles = null;

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
            renderHandles = new RTHandle[2];

            opacityModeAdditiveKeyword = new LocalKeyword(Material.shader, "_OPACITY_MODE_ADDITIVE");
            opacityModeChannelKeyword = new LocalKeyword(Material.shader, "_OPACITY_MODE_CHANNEL");
            singleChannelKeyword = new LocalKeyword(Material.shader, "_SINGLE_CHANNEL");
            overlayEffectKeyword = new LocalKeyword(Material.shader, "_OVERLAY_EFFECT");
        }

        private void CreateOffsetMatrix(float spacing, float lookTarget, int side, ref Matrix4x4 matrix) {
            float xOffset = spacing * side * 0.5f;
            Vector3 offset = Vector3.right * xOffset;
            if (lookTarget != 0) {
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(xOffset, 0, lookTarget).normalized, Vector3.up);
                matrix = Matrix4x4.TRS(offset, lookRotation, Vector3.one);
            } else {
                matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);
            }
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            this.source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            this.destination = renderingData.cameraData.renderer.cameraColorTargetHandle;

            //            RenderTextureDescriptor descriptor = cameraTextureDescriptor;
            //            descriptor.colorFormat = RenderTextureFormat.ARGB32; // comment out this line to enable transparent recordings
            //            descriptor.useDynamicScale = true;
            //            descriptor.depthBufferBits = 16;
            //
            //            for (int i = 0; i < settings.TextureCount; i++) {
            //                RenderingUtils.ReAllocateIfNeeded(ref renderHandles[i], descriptor, name: AnaglyphPass.RenderTargetNames[i]);
            //            }
            //
            //#if !UNITY_IOS && !UNITY_TVOS
            //            RenderingUtils.ReAllocateIfNeeded(ref intermediate, descriptor, name: AnaglyphPass.IntermediateRenderTargetName);
            //#endif
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            base.Configure(cmd, cameraTextureDescriptor);

            Material.SetKeyword(opacityModeAdditiveKeyword, settings.opacityMode == Settings.OpacityMode.Additive);
            Material.SetKeyword(opacityModeChannelKeyword, settings.opacityMode == Settings.OpacityMode.Channel);
            Material.SetKeyword(singleChannelKeyword, settings.TextureCount == 1);
            Material.SetKeyword(overlayEffectKeyword, settings.overlayEffect);

            CreateOffsetMatrix(settings.spacing, settings.lookTarget, -1, ref offsetMatrices[0]);
            CreateOffsetMatrix(settings.spacing, settings.lookTarget, 1, ref offsetMatrices[1]);

            RenderTextureDescriptor descriptor = cameraTextureDescriptor;
            //descriptor.colorFormat = RenderTextureFormat.ARGB32; // comment out this line to enable transparent recordings
            //descriptor.useDynamicScale = true;
            descriptor.depthBufferBits = 16;

            for (int i = 0; i < renderHandles.Length; i++) {
                if (renderHandles[i] == null) {
                    renderHandles[i] = RTHandles.Alloc(descriptor, name: AnaglyphPass.RenderTargetNames[i]);
                    Debug.Log("alloc");
                }
            }
#if !UNITY_IOS && !UNITY_TVOS
            if (intermediate == null) {
                intermediate = RTHandles.Alloc(descriptor, name: AnaglyphPass.IntermediateRenderTargetName);
            }
#endif

            for (int i = 0; i < settings.TextureCount; i++) {
                RenderingUtils.ReAllocateIfNeeded(ref renderHandles[i], descriptor, name: AnaglyphPass.RenderTargetNames[i]);
                //ConfigureTarget(renderHandles[i]);
            }

#if !UNITY_IOS && !UNITY_TVOS
            RenderingUtils.ReAllocateIfNeeded(ref intermediate, descriptor, name: AnaglyphPass.IntermediateRenderTargetName);
#endif

            //ConfigureClear(ClearFlag.Color | ClearFlag.Depth, Color.clear);
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

                    cmd.ClearRenderTarget(true, true, Color.clear);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    cmd.SetRenderTarget(renderHandles[0], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
                } else { // render both channels
                    for (int i = 0; i < 2; i++) {
                        Matrix4x4 viewMatrix = offsetMatrices[i] * camera.worldToCameraMatrix;
                        cmd.SetViewMatrix(viewMatrix);

                        cmd.ClearRenderTarget(true, true, Color.clear);

                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();

                        cmd.SetRenderTarget(renderHandles[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
                    }
                }

#if !UNITY_IOS && !UNITY_TVOS
                Blitter.BlitCameraTexture(cmd, source, intermediate, Material, 0);
                Blitter.BlitCameraTexture(cmd, intermediate, destination);
#else
                Blitter.BlitCameraTexture(source, destination, Material);
#endif
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            if (cmd == null) {
                throw new System.ArgumentNullException("cmd");
            }

            //            for (int i = 0; i < settings.TextureCount; i++) {
            //                cmd.ReleaseTemporaryRT(RenderTargetIDs[i]);
            //            }
            //#if !UNITY_IOS && !UNITY_TVOS
            //            cmd.ReleaseTemporaryRT(IntermediateRenderTargetID);
            //#endif
        }

        public void Dispose() {
            for (int i = 0; i < renderHandles.Length; i++) {
                renderHandles[i]?.Release();
            }
#if !UNITY_IOS && !UNITY_TVOS
            intermediate?.Release();
#endif
        }
    }
}
