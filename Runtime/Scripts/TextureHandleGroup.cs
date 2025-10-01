// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

using UnityEngine.Rendering.RenderGraphModule;

namespace Anaglyph3D {
	internal struct TextureHandleGroup {
		public TextureHandle color;
		public TextureHandle depth;

		public TextureHandleGroup(TextureHandle color, TextureHandle depth) {
			this.color = color;
			this.depth = depth;
		}
	}
}