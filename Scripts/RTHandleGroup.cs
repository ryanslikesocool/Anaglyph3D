// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
    public struct RTHandleGroup {
        public RTHandle color;
        public RTHandle depth;

        public RTHandleGroup(RTHandle color, RTHandle depth) {
            this.color = color;
            this.depth = depth;
        }

        public void Release() {
            color?.Release();
            depth?.Release();
        }
    }
}