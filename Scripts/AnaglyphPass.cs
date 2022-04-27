using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    public class AnaglyphPass : ScriptableRenderPass {
        private static readonly int[] RenderTargetIDs = new int[2] {
            Shader.PropertyToID("_LeftTex"),
            Shader.PropertyToID("_RightTex")
        };
        private static readonly int TemporaryRenderTargetID = Shader.PropertyToID("_TemporaryRenderTarget");
        private List<ShaderTagId> shaderTagIDs = new List<ShaderTagId>();

        private RenderTargetIdentifier[] renderTargetIdentifiers = null;
        private RenderTargetIdentifier tempRenderTargetIdentifier;
        private FilteringSettings filteringSettings;
        private RenderStateBlock renderStateBlock;

        private readonly string profilerTag;
        private Settings settings;
        private Matrix4x4[] offsetMatrices = null;

        private Material material;

        public AnaglyphPass(Settings settings, string tag) {
            profilingSampler = new ProfilingSampler(tag);
            filteringSettings = new FilteringSettings(null, settings.layerMask);

            shaderTagIDs.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagIDs.Add(new ShaderTagId("UniversalForward"));
            shaderTagIDs.Add(new ShaderTagId("UniversalForwardOnly"));
            shaderTagIDs.Add(new ShaderTagId("LightweightForward"));

            renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            this.renderPassEvent = settings.renderPassEvent;
            this.settings = settings;

            offsetMatrices = new Matrix4x4[2];
            renderTargetIdentifiers = new RenderTargetIdentifier[2];

            material = new Material(settings.shader);

#if UNITY_IOS && !UNITY_EDITOR
            //for (int i = 0; i < 28; i++) {
            //    Debug.Log($"{(RenderTextureFormat)i} supported? {SystemInfo.SupportsRenderTextureFormat((RenderTextureFormat)i)}");
            //}
#endif
        }

        private Matrix4x4 CreateOffsetMatrix(float spacing, float lookTarget, int side) {
            float xOffset = spacing * side * 0.5f;
            Vector3 offset = Vector3.right * xOffset;
            if (lookTarget != 0) {
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(xOffset, 0, lookTarget).normalized, Vector3.up);
                return Matrix4x4.TRS(offset, lookRotation, Vector3.one);
            } else {
                return Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);
            }
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            offsetMatrices[0] = CreateOffsetMatrix(settings.spacing, settings.lookTarget, -1);
            offsetMatrices[1] = CreateOffsetMatrix(settings.spacing, settings.lookTarget, 1);

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;

            descriptor.colorFormat = RenderTextureFormat.ARGB32;
            descriptor.useDynamicScale = true;
            //descriptor.autoGenerateMips = false;
            //descriptor.useMipMap = false;
            //descriptor.msaaSamples = 1;
            //descriptor.dimension = TextureDimension.Tex2D;
            //descriptor.stencilFormat = GraphicsFormat.None;

            { // temporary render target
                descriptor.depthBufferBits = 0;

                cmd.GetTemporaryRT(TemporaryRenderTargetID, descriptor);
                tempRenderTargetIdentifier = new RenderTargetIdentifier(TemporaryRenderTargetID);
                ConfigureTarget(tempRenderTargetIdentifier);
            }

            { // anaglyph targets
                descriptor.depthBufferBits = 8;

                for (int i = 0; i < 2; i++) {
                    cmd.GetTemporaryRT(RenderTargetIDs[i], descriptor);
                    renderTargetIdentifiers[i] = new RenderTargetIdentifier(RenderTargetIDs[i]);
                    ConfigureTarget(renderTargetIdentifiers[i], depthAttachment);
                    ConfigureClear(ClearFlag.Color, Color.clear);
                }
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIDs, ref renderingData, sortingCriteria);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler)) {
                ScriptableRenderer renderer = renderingData.cameraData.renderer;
                Camera camera = renderingData.cameraData.camera;

                for (int i = 0; i < 2; i++) {
                    Matrix4x4 viewMatrix = offsetMatrices[i] * camera.worldToCameraMatrix;
                    cmd.SetViewMatrix(viewMatrix);

                    cmd.ClearRenderTarget(false, true, Color.clear);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    cmd.SetRenderTarget(renderTargetIdentifiers[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
                }

                cmd.SetRenderTarget(renderer.cameraColorTarget);
                ConfigureClear(ClearFlag.None, Color.clear);

                cmd.Blit(renderer.cameraColorTarget, TemporaryRenderTargetID, null);
                cmd.Blit(TemporaryRenderTargetID, renderer.cameraColorTarget, material);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            if (cmd == null) {
                throw new System.ArgumentNullException("cmd");
            }

            cmd.ReleaseTemporaryRT(TemporaryRenderTargetID);
            for (int i = 0; i < 2; i++) {
                cmd.ReleaseTemporaryRT(RenderTargetIDs[i]);
            }
        }
    }
}