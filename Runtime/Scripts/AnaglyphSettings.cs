// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
	[Serializable]
	public sealed class Settings {
		[Tooltip("Leave at 'Before Rendering Post Processing' for best results.")] public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
		[Tooltip("Which layers to include when rendering the effect.")] public LayerMask layerMask = -1;
		[Tooltip("The anaglpyh shader, located at the root directory of the package.")] public Shader shader = null;

		[Header("Transform")]
		[Tooltip("The spacing between the red and cyan channels.\nA value of '0' will ignore the focal point.  This is useful for orthographic cameras.\nA negative value will swap red and cyan.")] public float spacing = 0.2f;
		[Tooltip("The point 'x' units in front of the camera where the red and cyan channels meet.")] public float focalPoint = 10f;

		[Header("Blending")]
		[Tooltip("'None' - Replace the background with the effect.  This is ideal for rendering the entire screen with the effect.\n\n'Opacity' - Overlay the effect based on its opacity.\n\n'Depth' - Overlay the effect based on its depth.")] public OverlayMode overlayMode = OverlayMode.Opacity;
		[Tooltip("'None' - Do not blend the effect onto the background.\n\n'Additive' - Perform stylistic blending by adding the effect to the background.\n\n'Channel' - Perform correct blending based on each eye's channels.")] public BlendMode blendMode = BlendMode.None;
		[Tooltip("'Luminance' - Use grayscale color based on luminance.\n\n'Color' - Use source color and split channels.")] public ColorMode colorMode = ColorMode.Luminance;

		[Header("Rendering")]
		[Tooltip("The render texture format to use when Overlay Mode is set to Opacity.\nThe texture format requires an alpha channel for blending to work.")] public RenderTextureFormat opacityOverlayRenderTextureFormat = RenderTextureFormat.ARGB32;
		//[Tooltip("The depth buffer bit count to use when Overlay Mode is set to Depth.")] public DepthBufferBitCount depthOverlayBufferBitCount = DepthBufferBitCount._24;

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

		public enum ColorMode : int {
			Luminance = 0,
			Color = 1
		}

		public enum DepthBufferBitCount : int {
			[InspectorName("0")] _0 = 0,
			[InspectorName("16")] _16 = 16,
			[InspectorName("24")] _24 = 24,
			[InspectorName("32")] _32 = 32,
		}
	}
}