// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anaglyph3D {
	[Serializable]
	public sealed class Settings {
		[Tooltip("When should the effect render?")] public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
		[Tooltip("What kinds of objects should the effect render?")] public Queue queue = Queue.Opaque;
		[Tooltip("Which layers should be rendered?")] public LayerMask layerMask = -1;

		[Header("Camera")]
		[Tooltip("The spacing between the red and cyan channels.\nA value of '0' will ignore the focal point.  This is useful for orthographic cameras.\nA negative value will swap red and cyan.")] public float spacing = 0.2f;
		[Tooltip("The point 'x' units in front of the camera where the red and cyan channels meet.")] public float focalPoint = 10f;

		internal bool SingleChannel => spacing == 0;

		internal RenderQueueRange QueueRange => queue switch {
			Queue.Opaque => RenderQueueRange.opaque,
			Queue.Transparent => RenderQueueRange.transparent,
			Queue.All => RenderQueueRange.all,
			_ => new RenderQueueRange()
		};

		public enum Queue : byte {
			Opaque = 1 << 0,
			Transparent = 1 << 1,
			All = Opaque | Transparent
		}
	}
}