using System;
using UnityEngine;
using UnityEngine.UI;

namespace Utils.Views
{
	public static class ViewUtils
	{
		public static void SetTexture(this RawImage image, Texture texture, AspectRatioFitter fitter = null)
		{
			if (fitter == null && (fitter = image.GetComponent<AspectRatioFitter>()) == null)
			{
				throw new Exception($"AspectRatioFitter not found with RawImage '{image.name}'");
			}

			image.texture = texture;
			fitter.aspectRatio = (float) texture.width / texture.height;
		}
	}
}