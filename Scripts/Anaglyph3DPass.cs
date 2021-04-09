// Made with <3 by Ryan Boyer http://ryanjboyer.com

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D
{
    public class Anaglyph3DPass : ScriptableRenderPass
    {
        private const string CAMERA_CLIP_PROP = "_CameraClip";
        private const string SEPARATION_DISTANCE_PROP = "_SeparationDistance";

        private string profilerTag;

        private Anaglyph3DSettings settings;
        private ScriptableRenderer renderer;

        public Anaglyph3DPass(string profilerTag, Anaglyph3DSettings settings)
        {
            this.profilerTag = profilerTag;
            this.settings = settings;
            renderPassEvent = settings.passEvent;
        }

        public void Setup(ScriptableRenderer renderer)
        {
            this.renderer = renderer;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            // start rendering

            Camera camera = Camera.main;
            if (camera == null) { return; }

            camera.depthTextureMode |= DepthTextureMode.Depth;

            Material mat = settings.anaglyphMaterial;
            mat.SetVector(CAMERA_CLIP_PROP, new Vector4(camera.nearClipPlane, camera.farClipPlane));
            mat.SetVector(SEPARATION_DISTANCE_PROP, new Vector4(settings.channelSeparation.x, settings.channelSeparation.y, settings.distanceRange.x, settings.distanceRange.y));

            cmd.Blit(renderer.cameraColorTarget, renderer.cameraColorTarget, mat);

            // end rendering
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}