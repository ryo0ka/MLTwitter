using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using Utils;

namespace MLTwitter.Demo
{
	public class StatusView : MonoBehaviour
	{
		[SerializeField]
		RawImage _profileImage;

		[SerializeField]
		Text _nameText;

		[SerializeField]
		Text _screenNameText;

		[SerializeField]
		Text _dateTimeText;

		[SerializeField]
		Text _statusText;

		[SerializeField]
		MLMediaPlayer _mediaPlayer;

		[SerializeField]
		RawImage _videoImage;

		[SerializeField]
		CanvasGroup _group;

		public async UniTask Show(TWUser user)
		{
			// Hide video
			_videoImage.gameObject.SetActive(false);

			// Show the entire view
			Fade(true).Forget(Debug.LogException);

			// Set up data
			var status = user.Status;
			_nameText.text = user.Name;
			_screenNameText.text = user.ScreenName;
			_dateTimeText.text = $"{status.DateTime:MMM dd}";
			_statusText.text = status.Text;
			_profileImage.texture = await DownloadImage(user.ProfileImageUrl);

			// Show & play video if included in the status
			if (status.ExtendedEntities?.Media is TWMediaObject[] mediaObjs &&
			    mediaObjs.TryGetFirstValue(out TWMediaObject media) &&
			    media.Type == "video" &&
			    media.VideoInfo.Variants.TryGetFirstValue(out var variant))
			{
				// Show video
				_videoImage.gameObject.SetActive(true);
				
				await PlayVideo(variant.Url);
			}
		}

		async UniTask PlayVideo(string url)
		{
			_mediaPlayer.VideoSource = url;

			// Start observing OnMediaError before PrepareVideo because 
			// PrepareVideo itself can trigger MediaError events
			var error = _mediaPlayer.OnMediaErrorAsObservable().ToUniTask(useFirstValue: true);
			var complete = _mediaPlayer.OnVideoPreparedAsObservable().ToUniTask(useFirstValue: true);

			_mediaPlayer.PrepareVideo().ThrowIfFail();

			// Throw exception in case an error happens,
			// otherwise wait until preparation is completed
			await UniTask.WhenAny(error, complete);

			// Play video
			_mediaPlayer.IsLooping = true;
			_mediaPlayer.Play().ThrowIfFail();
		}

		public async UniTask Hide(bool immediately = false)
		{
			_mediaPlayer.Stop();

			if (immediately)
			{
				gameObject.SetActive(false);
				return;
			}

			await Fade(false);
		}

		async UniTask Fade(bool show)
		{
			if (show)
			{
				gameObject.SetActive(true);
			}

			await UnityUtils.Animate(0.5f, AnimationCurve.EaseInOut(0, 0, 1, 1), t =>
			{
				_group.alpha = show ? t : 1 - t;
			});

			if (!show)
			{
				gameObject.SetActive(false);
				_mediaPlayer.Stop();
			}
		}

		async UniTask<Texture> DownloadImage(string url)
		{
			using (var req = UnityWebRequestTexture.GetTexture(url))
			{
				await req.SendWebRequest();
				return ((DownloadHandlerTexture) req.downloadHandler).texture;
			}
		}
	}
}