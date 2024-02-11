// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

using UnityEngine;

namespace Anaglyph3D {
	internal static class Extensions {
		public static void CreateOffsetMatrix(float spacing, float lookTarget, float scale, ref Matrix4x4 matrix) {
			float xOffset = spacing * scale;
			Vector3 offset = Vector3.right * xOffset;
			if (lookTarget != 0) {
				Quaternion lookRotation = Quaternion.LookRotation(new Vector3(xOffset, 0, lookTarget).normalized, Vector3.up);
				matrix = Matrix4x4.TRS(offset, lookRotation, Vector3.one);
			} else {
				matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);
			}
		}
	}
}