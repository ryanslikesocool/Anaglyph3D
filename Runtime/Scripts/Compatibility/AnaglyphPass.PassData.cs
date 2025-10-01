// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Anaglyph3D {
	internal sealed partial class AnaglyphPass {
		private sealed class PassData {
			public bool isSingleChannel;

			public RendererListHandle rendererList;

			public Matrix4x4 viewMatrix;
			public Matrix4x4 projectionMatrix;
		}

		private sealed class CombinePassData {
			public Material material;
			public LocalKeyword singleChannelKeyword;
			public bool isSingleChannel;

			public Matrix4x4 viewMatrix;
			public Matrix4x4 projectionMatrix;
		}
	}
}