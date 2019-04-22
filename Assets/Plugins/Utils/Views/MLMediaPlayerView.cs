using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace Utils.Views
{
	// Transfers MLMediaPlayer's texture to RawImage
	// so that Unity UI can implement media player on Magic Leap
	public class MLMediaPlayerView : MonoBehaviour
	{
		[SerializeField]
		MLMediaPlayer _mediaPlayer;

		[SerializeField]
		RawImage _videoImage;

		readonly int _videoTexId = Shader.PropertyToID("_MainTex");
		Renderer _videoRenderer;

		void Reset()
		{
			_mediaPlayer = GetComponentInChildren<MLMediaPlayer>();
			_videoImage = GetComponentInChildren<RawImage>();
		}

		void Start()
		{
			// Cache the renderer's reference
			_videoRenderer = _mediaPlayer.GetComponent<Renderer>();
		}

		void Update()
		{
			// I couldn't find an event so let's do this every update
			_videoImage.texture = _videoRenderer.material?.GetTexture(_videoTexId);
		}
	}
}