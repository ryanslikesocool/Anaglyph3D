// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    [Serializable]
    public sealed class Settings {
        [Tooltip("Leave at 'Before Rendering Post Processing' for best results.")] public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [Tooltip("Which layers to include when rendering the effect.")] public LayerMask layerMask = -1;

        [Header("Transform")]
        [Tooltip("The spacing between the red and cyan channels.\nA value of '0' will ignore the focal point.  This is useful for orthographic cameras.\nA negative value will swap the red and cyan.")] public float spacing = 0.2f;
        [Tooltip("The point 'x' units in front of the camera where the red and cyan channels meet.")] public float focalPoint = 10f;

        [Header("Blending")]
        [Tooltip("'None' - Replace the background with the effect.  This is ideal for rendering the entire screen with the effect.\n'Opacity' - Overlay the effect based on its opacity.\n'Depth' - Overlay the effect based on its depth.")] public OverlayMode overlayMode = OverlayMode.Opacity;
        [Tooltip("'None' - Do not blend the effect onto the background.\n'Additive' - Perform stylistic blending by adding the effect to the background.\n'Channel' - Perform correct blending based on each eye's channels.")] public BlendMode blendMode = BlendMode.None;

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