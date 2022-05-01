// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    [Serializable]
    public class Settings {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public LayerMask layerMask = -1;
        [Tooltip("The spacing between the red and cyan channels.  This value may need to be larger for orthographic cameras.")] public float spacing = 0.2f;
        [Tooltip("The focal point, represented as units in front of the camera.")] public float lookTarget = 10f;
        [Tooltip("The anaglpyh shader, located at the root directory of the package.")] public Shader shader = null;
    }
}