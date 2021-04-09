// Made with <3 by Ryan Boyer http://ryanjboyer.com

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D
{
    [System.Serializable]
    public class Anaglyph3DSettings
    {
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Material anaglyphMaterial = null;

        [Space] public Vector2 channelSeparation = new Vector2(0.025f, 0);
        public Vector2 distanceRange = new Vector2(3, 15);
    }
}