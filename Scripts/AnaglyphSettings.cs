// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    [Serializable]
    public sealed class Settings {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [Tooltip("Which layers to include when rendering the effect.")] public LayerMask layerMask = -1;

        [Header("Transform")]
        [Tooltip("The spacing between the red and cyan channels.  This value may need to be larger for orthographic cameras.")] public float spacing = 0.2f;
        [Tooltip("The focal point, represented as units in front of the camera.")] public float lookTarget = 10f;

        [Header("Blending")]
        [Tooltip("Overlay the layers with the effect on top of other layers.  This is useful for when only some layers should be rendered with the effect, but is also more computationally expensive.")] public OverlayMode overlayMode = OverlayMode.Opacity;
        [Tooltip("How should anaglyph layers be rendered on top of normal layers?  This requires Overlay Mode to be active.")] public BlendMode blendMode = BlendMode.None;

        [Space, Tooltip("The anaglpyh shader, located at the root directory of the package.")] public Shader shader = null;

        internal bool SingleChannel => spacing == 0;

        internal bool NeedsDepth => overlayMode == OverlayMode.Depth;

        internal Material material = default;

        public enum OverlayMode : int {
            None = 0,
            Opacity = 1,
            Depth = 2
        }

        public enum BlendMode : int {
            None = 0,
            Additive = 1,
            Channel = 2
        }
    }
}