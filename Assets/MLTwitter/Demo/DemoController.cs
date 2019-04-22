using System;
using System.IO;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using Utils;

namespace MLTwitter.Demo
{
	internal class DemoController : MonoBehaviour
	{
		[SerializeField]
		CredentialRepository _credentials;

		[SerializeField]
		Text _promptText;

		[SerializeField]
		Text _errorText;

		[SerializeField]
		StatusView _statusView;

		TWClient _client;
		Subject<Unit> _onApplicationResume;

		void Awake()
		{
			// Initialize view on Awake so that user won't see the debug state
			_errorText.text = "";
			ClearLines();
			_statusView.Hide(true).Forget(Debug.LogException);
		}

		void Start()
		{
			DoStart().Forget(e =>
			{
				Debug.LogException(e);

				// Receive any errors during demo and present it on the ui
				_errorText.text = e.Message;
			});
		}

		void OnApplicationPause(bool pauseStatus)
		{
			if (!pauseStatus)
			{
				// Notify when this app is re-focused from Helio
				_onApplicationResume?.OnNext(Unit.Default);
			}
		}

		async UniTask DoStart()
		{
			// Get a WiFi privilege
			await MLUtils.RequestPrivilege(MLPrivilegeId.LocalAreaNetwork);

			_credentials.ClearStorage();
			_client = new TWClient(_credentials, _credentials);

			_onApplicationResume = new Subject<Unit>();

			ClearLines();
			AppendLine("You may use a dummy Twitter account for this demo.");
			AppendLine("Press Trigger button to start...");

			// Wait for a trigger by user
			await MLUtils.OnTriggerUpAsObservable().ToUniTask(useFirstValue: true);

			ClearLines();
			AppendLine("You'll be prompted to open Helio shortly...");

			// Initiate 3-legged authentication.
			// Callback URL must be your app's URI configured in the manifest,
			// also must be registered in your Twitter app's "Callback URLs" list.
			// See https://forum.magicleap.com/hc/en-us/community/posts/360042601671
			// and https://developer.twitter.com/en/docs/basics/apps/guides/callback-urls
			string authUrl = await _client.GetUserAuthenticationUrl(_credentials.CallbackUrl);

			// Open the URL on Helio and let user log in to Twitter
			MLDispatcher.TryOpenAppropriateApplication(authUrl).ThrowIfFail();

			ClearLines();
			AppendLine("Waiting for a redirect from Helio...");
			AppendLine("(if you'd like to retry, please exit all apps)");

			/* Magic Leap's dispatcher is generally immature and
			 * you'll find MANY causes of failure here
			 * but for this demo I just let user initialize everything in such cases
			 * for the simplicity of this code...
			 */

			// Wait for getting redirected from Helio
			await _onApplicationResume.ToUniTask(useFirstValue: true);

			// Authorize using the redirect URL sent from Helio
			var redirectUrl = Environment.GetCommandLineArgs()[0];
			await _client.AuthorizeUser(redirectUrl);

			ClearLines();
			AppendLine("Authorized with your Twitter account!");
			AppendLine("Press Trigger button to tweet on your account");
			AppendLine("Please end this demo if your account is public)");

			// Wait for a trigger by user
			await MLUtils.OnTriggerUpAsObservable().ToUniTask(useFirstValue: true);

			// Tweet a sample text (note that all characters survive URL encoding)
			await _client.UpdateStatus("Tweeting from #MagicLeap using MLTwitter https://github.com/ryo0ka/MLTwitter");

			// Get the authorized user's latest status (which is the tweet above)
			var user = await _client.VerifyCredentials();

			// Present the tweet to user
			await _statusView.Show(user);

			ClearLines();
			AppendLine("Check out your new tweet!");
			AppendLine("Press Trigger button to move on to the next demo...");

			// Wait for a trigger by user
			await MLUtils.OnTriggerUpAsObservable().ToUniTask(useFirstValue: true);

			// Hide the twitter ui
			await _statusView.Hide();

			ClearLines();
			AppendLine("Press Trigger button to start video capture & upload...");
			AppendLine("(Tweet will contain a video captured from now.");
			AppendLine("Please end this demo if your privacy is concerned)");

			// Wait for a trigger by user
			await MLUtils.OnTriggerUpAsObservable().ToUniTask(useFirstValue: true);

			// Make a file path for the video
			string videoFilePath = Path.Combine(Application.temporaryCachePath, "video.mp4");

			// Update privileges just in case
			await MLUtils.RequestPrivilege(MLPrivilegeId.LocalAreaNetwork);
			await MLUtils.RequestPrivilege(MLPrivilegeId.CameraCapture);
			await MLUtils.RequestPrivilege(MLPrivilegeId.AudioCaptureMic);

			// Start video recording
			MLCamera.Start().ThrowIfFail();
			MLCamera.Connect().ThrowIfFail();
			MLCamera.StartVideoCapture(videoFilePath);

			ClearLines();
			AppendLine("Press Trigger button to STOP video capture and upload it on Twitter...");

			// Wait for a trigger by user
			await MLUtils.OnTriggerUpAsObservable().ToUniTask(useFirstValue: true);

			// Stop video recording
			MLCamera.StopVideoCapture().ThrowIfFail();

			ClearLines();
			AppendLine("Stoped video capture. Encoding...");

			// Wait until encoding is over
			await MLUtils.OnCaptureCompletedAsObservable().ToUniTask(useFirstValue: true);

			// Stop capture service
			MLCamera.Disconnect().ThrowIfFail();
			MLCamera.Stop();

			ClearLines();
			AppendLine("Finished encoding. Uploading to Twitter...");

			// Read the video file
			byte[] video = File.ReadAllBytes(videoFilePath);

			// Upload the video to Twitter (this is just a media upload; not a tweet)
			string videoMediaId = await _client.UploadVideo(video, (upload, encode) =>
			{
				ClearLines();
				AppendLine($"Uploading: {upload * 100:0}% done, encoding: {encode * 100:0}% done...");
			});

			// Tweet the video
			await _client.UpdateStatus("Uploading a video capture from #MagicLeap using MLTwitter", videoMediaId);

			// Present the tweet to user
			user = await _client.VerifyCredentials();
			await _statusView.Show(user);

			ClearLines();
			AppendLine("Check out your new tweet!");
			AppendLine("");
			AppendLine("You've reached the end of this demo.");
			AppendLine("Press Trigger button to exit the app...");

			// Wait for a trigger by user
			await MLUtils.OnTriggerUpAsObservable().ToUniTask(useFirstValue: true);

			// Cool animation
			await _statusView.Hide();

			Application.Quit();
		}

		void ClearLines()
		{
			_promptText.text = "";
		}

		void AppendLine(string line)
		{
			_promptText.text += line + "\n";
		}
	}
}