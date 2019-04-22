using System;
using UnityEngine.Networking;

namespace MLTwitter
{
	// Exception for HTTP-related errors (since Unity doens't supply one)
	public class TWHttpException : Exception
	{
		public TWHttpException(UnityWebRequest req)
		{
			Url = req.url;
			Code = req.responseCode;
			Error = req.error;
			Text = req.downloadHandler.text;
		}

		public string Url { get; }
		public long Code { get; }
		public string Error { get; }
		public string Text { get; }

		public override string Message =>
			$"'{Url}', {Code}, '{Error}', '{Text}'";

		// Helper method to catch HTTP errors after `SendRequest()`
		public static void ThrowIfError(UnityWebRequest req)
		{
			if (!string.IsNullOrEmpty(req.error))
			{
				throw new TWHttpException(req);
			}
		}
	}
}