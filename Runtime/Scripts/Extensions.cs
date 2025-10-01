// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

using UnityEngine;

namespace Anaglyph3D {
	internal static class Extensions {
		public static Matrix4x4 CreateOffsetMatrix(float spacing, float lookTarget, float scale) {
			float xOffset = spacing * scale;
			Vector3 offset = Vector3.right * xOffset;

			if (lookTarget != 0) {
				Quaternion lookRotation = Quaternion.LookRotation(new Vector3(xOffset, 0, lookTarget).normalized, Vector3.up);
				return Matrix4x4.TRS(offset, lookRotation, Vector3.one);
			} else {
				return Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);
			}
		}
	}
}