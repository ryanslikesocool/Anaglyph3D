// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    [System.Serializable]
    public class Anaglyph3DSettings {
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Vector2 channelSeparation = new Vector2(-0.0025f, 0);
        [Range(0, 1)] public float tintOpacity = 0.05f;

        [HideInInspector] public Material anaglyphMaterial = null;
    }
}