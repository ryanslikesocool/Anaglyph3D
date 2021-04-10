// Made with <3 by Ryan Boyer http://ryanjboyer.com

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