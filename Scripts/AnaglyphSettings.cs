using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    [System.Serializable]
    public class Settings {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public LayerMask layerMask = -1;
        [Tooltip("The spacing between the red and cyan channels.  This value may need to be larger for orthographic cameras.")] public float spacing = 0.2f;
        [Tooltip("The focal point, represented as units in front of the camera.")] public float lookTarget = 10f;
        [Tooltip("The anaglpyh shader, located at the root directory of the package.")] public Shader shader = null;
    }
}