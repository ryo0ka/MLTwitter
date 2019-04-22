using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UniRx.Async;

namespace MLTwitter
{
	// Implementations of end-user APIs
	public static class TWClientExtension
	{
		public static async UniTask UpdateStatus(this TWClient client, string text, params string[] mediaIds)
		{
			await client.Post<JObject>("statuses/update", new Dictionary<string, string>
			{
				{"status", text},
				{"media_ids", string.Join(",", mediaIds)},
			});
		}

		public static async UniTask<TWUser> VerifyCredentials(this TWClient client)
		{
			return await client.Get<TWUser>("account/verify_credentials", new Dictionary<string, string>());
		}

		// Sample implementation of image upload
		public static async UniTask<string> UploadImage(this TWClient client, byte[] jpg)
		{
			var totalLength = jpg.Length;

			var init = await client.InitMediaUpload(totalLength, "image/jpeg", "tweet_image");
			var mediaId = init.MediaId;

			const int ChunkSize = 512 * 1000;
			var chunkCount = (int) ((float) totalLength / ChunkSize);
			var chunkBuffer = new byte[ChunkSize];

			for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
			{
				Array.Copy(jpg, chunkIndex * ChunkSize, chunkBuffer, 0, ChunkSize);
				await client.AppendMediaUpload(mediaId, chunkIndex, chunkBuffer);
			}

			int bytesLeft = totalLength - ChunkSize * chunkCount;
			if (bytesLeft > 0)
			{
				var flush = new byte[bytesLeft];
				var startIndex = totalLength - bytesLeft;
				Array.Copy(jpg, startIndex, flush, 0, bytesLeft);
				await client.AppendMediaUpload(mediaId, chunkCount, flush);
			}

			await client.FinalizeMediaUpload(mediaId);

			return mediaId;
		}

		// Sample implementation of video upload
		public static async UniTask<string> UploadVideo(this TWClient client, byte[] mp4, Action<float, float> onProgress)
		{
			var totalLength = mp4.Length;

			var init = await client.InitMediaUpload(totalLength, "video/mp4", "tweet_video");
			var mediaId = init.MediaId;

			const int ChunkSize = 1000 * 1024 * 3;
			var chunkCount = (int) ((float) totalLength / ChunkSize);
			var chunkBuffer = new byte[ChunkSize];

			for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
			{
				Array.Copy(mp4, chunkIndex * ChunkSize, chunkBuffer, 0, ChunkSize);
				await client.AppendMediaUpload(mediaId, chunkIndex, chunkBuffer);

				onProgress?.Invoke((float) (chunkIndex + 1) / chunkCount, 0);
			}

			int bytesLeft = totalLength - ChunkSize * chunkCount;
			if (bytesLeft > 0)
			{
				var flush = new byte[bytesLeft];
				var startIndex = totalLength - bytesLeft;
				Array.Copy(mp4, startIndex, flush, 0, bytesLeft);
				await client.AppendMediaUpload(mediaId, chunkCount, flush);
			}

			onProgress?.Invoke(1, 0);

			var fin = await client.FinalizeMediaUpload(mediaId);
			var progress = fin.ProcessingInfo;

			while (progress.State == "pending" || progress.State == "in_progress")
			{
				var waitSeconds = progress.CheckSeconds;
				await UniTask.Delay(TimeSpan.FromSeconds(waitSeconds));

				var status = await client.GetMediaUploadStatus(mediaId);
				progress = status.ProcessingInfo;

				onProgress?.Invoke(1, (float) progress.Percentage / 100);
			}

			onProgress?.Invoke(1, 1);

			return mediaId;
		}
	}
}