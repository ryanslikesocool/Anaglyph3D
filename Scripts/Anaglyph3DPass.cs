// Made with love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D
{
    public class Anaglyph3DPass : ScriptableRenderPass
    {
        private const string CHANNEL_SEPARATION_PROP = "_ChannelSeparation";
        private const string TINT_OPACITY_PROP = "_TintOpacity";

        private string profilerTag;

        private Anaglyph3DSettings settings;

        public Anaglyph3DPass(string profilerTag, Anaglyph3DSettings settings)
        {
            this.profilerTag = profilerTag;
            this.settings = settings;
            renderPassEvent = settings.passEvent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            Camera camera = renderingData.cameraData.camera;
            if (camera == null) { return; }
            // start rendering

            Material mat = settings.anaglyphMaterial;
            mat.SetVector(CHANNEL_SEPARATION_PROP, new Vector2(settings.channelSeparation.x, settings.channelSeparation.y));
            mat.SetFloat(TINT_OPACITY_PROP, settings.tintOpacity);

            cmd.Blit(renderer.cameraColorTarget, renderer.cameraColorTarget, mat);

            // end rendering
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}