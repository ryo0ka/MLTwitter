using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

namespace MLTwitter
{
	// Core client (don't implement end-user APIs here)
	public class TWClient
	{
		const string ApiEndpoint = "https://api.twitter.com/1.1";
		const string AuthTokenEndpoint = "https://api.twitter.com/oauth2/token";
		const string UserAuthRequestEndpoint = "https://api.twitter.com/oauth/request_token";
		const string UserAuthenticationEndpoint = "https://api.twitter.com/oauth/authorize";
		const string UserAuthorizationEndPoint = "https://api.twitter.com/oauth/access_token";
		const string UploadEndpoint = "https://upload.twitter.com/1.1/media/upload.json";

		readonly ITWCredentialRepository _app;
		readonly ITWCredentialStorage _storage;

		// Used during 3-legged OAuth
		string _userRequestToken;
		string _userRequestTokenSecret;

		public TWClient(ITWCredentialRepository app, ITWCredentialStorage storage)
		{
			_app = app;
			_storage = storage;
		}

		// Perform Application-only authorization as in:
		// https://developer.twitter.com/en/docs/basics/authentication/overview/application-only
		public async UniTask AuthorizeApp()
		{
			var form = new Dictionary<string, string>
			{
				{"grant_type", "client_credentials"},
			};

			string credential = Convert.ToBase64String(Encoding.UTF8.GetBytes(
				$"{_app.ConsumerKey}:{_app.ConsumerSecret}"));

			using (var req = UnityWebRequest.Post(AuthTokenEndpoint, form))
			{
				req.SetRequestHeader("Authorization", $"Basic {credential}");

				await req.SendWebRequest();

				TWHttpException.ThrowIfError(req);

				var res = JsonConvert.DeserializeObject<TWBearerToken>(req.downloadHandler.text);
				var token = res.AccessToken;
				Debug.Assert(token != null);

				_storage.AppAccessToken = token;
			}
		}

		// Step 1 in https://developer.twitter.com/en/docs/basics/authentication/overview/3-legged-oauth
		// For callback URL syntax, see https://developer.twitter.com/en/docs/basics/apps/guides/callback-urls
		// To use PIN-based OAuth, pass "oob" to `callbackUrl` parameter.
		// Navigate user to Twitter on a web browser using the URL returned from this method.
		public async UniTask<string> GetUserAuthenticationUrl(string callbackUrl)
		{
			using (var req = UnityWebRequest.Post(UserAuthRequestEndpoint, ""))
			{
				var oauthHeader = TWOAuth.MakeHeader(
					oauthToken: _app.AccessToken,
					oauthTokenSecret: _app.AccessTokenSecret,
					consumerKey: _app.ConsumerKey,
					consumerSecret: _app.ConsumerSecret,
					httpMethod: req.method,
					endpointUrl: req.url,
					extraParams: new Dictionary<string, string>
					{
						{"oauth_callback", callbackUrl}
					});

				req.SetRequestHeader("Authorization", oauthHeader);

				await req.SendWebRequest();

				TWHttpException.ThrowIfError(req);

				var response = TWOAuth.ParseParameters(req.downloadHandler.text);
				if (response["oauth_callback_confirmed"] != "true")
				{
					throw new Exception("OAuth failed: oauth_callback_confirmed was not true");
				}

				_userRequestToken = response["oauth_token"];
				_userRequestTokenSecret = response["oauth_token_secret"];
				Debug.Assert(_userRequestToken != null);
				Debug.Assert(_userRequestTokenSecret != null);

				return $"{UserAuthenticationEndpoint}?oauth_token={_userRequestToken}";
			}
		}

		// Step 2 in https://developer.twitter.com/en/docs/basics/authentication/overview/3-legged-oauth
		// Use this method when you passed a callback URL in `GetUserAuthenticationUrl()`.
		// Pass the callback that your app received from the earlier request's callback.
		public async UniTask AuthorizeUser(string callback)
		{
			// Check URL format
			if (!Uri.IsWellFormedUriString(callback, UriKind.Absolute))
			{
				throw new Exception($"Malformed callback: {callback}");
			}

			// Check presence of callback parameters
			var components = callback.Split('?');
			if (components.Length < 2)
			{
				throw new Exception($"Invalid callback: {callback}");
			}

			// Parse callback parameters
			var response = TWOAuth.ParseParameters(components[1]);
			var verifier = response["oauth_verifier"];
			var token = response["oauth_token"];

			// `oauth_token` must be the same as the request token
			// (otherwise this callback is coming back from a different request)
			if (token != _userRequestToken)
			{
				throw new Exception("Mismatched request token");
			}

			// Try authorizing with the verifier
			if (!await AuthorizeUserWithVerifier(verifier))
			{
				throw new Exception("Invalid verifier");
			}
		}

		// Step 3 in https://developer.twitter.com/en/docs/basics/authentication/overview/3-legged-oauth
		// Also use this method when you're using PIN-based OAuth (passed "oob" to `GetUserAuthenticationUrl()`).
		// Pass the user's PIN code to `oauthVerifier` parameter.
		public async UniTask<bool> AuthorizeUserWithVerifier(string verifier)
		{
			// Check if this client has actually requested a token earlier
			if (_userRequestToken == null || _userRequestTokenSecret == null)
			{
				throw new Exception("Request token/secret has not been granted");
			}

			var query = new Dictionary<string, string>
			{
				{"oauth_verifier", verifier},
			};

			using (var req = UnityWebRequest.Post(UserAuthorizationEndPoint, query))
			{
				var oauthHeader = TWOAuth.MakeHeader(
					oauthToken: _userRequestToken,
					oauthTokenSecret: _userRequestTokenSecret,
					consumerKey: _app.ConsumerKey,
					consumerSecret: _app.ConsumerSecret,
					httpMethod: req.method,
					endpointUrl: req.url,
					extraParams: query);

				req.SetRequestHeader("Authorization", oauthHeader);

				await req.SendWebRequest();

				if (req.responseCode == 401)
				{
					return false;
				}

				TWHttpException.ThrowIfError(req);

				// Parse OAuth parameters
				var response = TWOAuth.ParseParameters(req.downloadHandler.text);
				var token = response["oauth_token"];
				var secret = response["oauth_token_secret"];
				Debug.Assert(token != null);
				Debug.Assert(secret != null);

				// Save the granted access token to storage
				_storage.UserAccessToken = token;
				_storage.UserAccessTokenSecret = secret;

				return true;
			}
		}

		// GET with OAuth header prepared. You should be able to implement majority of GET APIs with this method
		// You MUST have authorized as an app or user earlier or you'll receive 401 error 
		public async UniTask<T> Get<T>(string path, IDictionary<string, string> form)
		{
			var endpoint = $"{ApiEndpoint}/{path}.json";

			using (var req = UnityWebRequest.Get($"{endpoint}?{FormatUtils.UrlEncode(form)}"))
			{
				string authHeader = await MakeAuthHeader("GET", endpoint, form);
				req.SetRequestHeader("Authorization", authHeader);

				await req.SendWebRequest();

				TWHttpException.ThrowIfError(req);

				string res = req.downloadHandler.text;
				return JsonConvert.DeserializeObject<T>(res);
			}
		}

		// POST with OAuth header prepared. You should be able to implement majority of POST APIs with this method
		// You MUST have authorized as an app or user earlier or you'll receive 401 error
		public async UniTask<T> Post<T>(string path, IDictionary<string, string> form)
		{
			var endpoint = $"{ApiEndpoint}/{path}.json";

			using (var req = FormatUtils.RequestPost(endpoint, form))
			{
				string authHeader = await MakeAuthHeaderAsUser("POST", endpoint, form);
				req.SetRequestHeader("Authorization", authHeader);

				await req.SendWebRequest();

				TWHttpException.ThrowIfError(req);

				string res = req.downloadHandler.text;
				return JsonConvert.DeserializeObject<T>(res);
			}
		}

		// Step 1 in https://developer.twitter.com/en/docs/media/upload-media/overview
		public async UniTask<TWMediaUploadInitResponse> InitMediaUpload(int byteSize, string mediaType, string mediaCategory)
		{
			var endpoint = UploadEndpoint;

			var form = new Dictionary<string, string>
			{
				{"command", "INIT"},
				{"total_bytes", $"{byteSize}"},
				{"media_type", mediaType},
				{"media_category", mediaCategory},
			};

			using (var req = FormatUtils.RequestPost(endpoint, form))
			{
				string authHeader = await MakeAuthHeaderAsUser("POST", endpoint, form);
				req.SetRequestHeader("Authorization", authHeader);

				await req.SendWebRequest();

				TWHttpException.ThrowIfError(req);

				string res = req.downloadHandler.text;
				return JsonConvert.DeserializeObject<TWMediaUploadInitResponse>(res);
			}
		}

		// Step 2 in https://developer.twitter.com/en/docs/media/upload-media/overview
		public async UniTask AppendMediaUpload(string mediaId, int segmentIndex, byte[] mediaData)
		{
			var endpoint = UploadEndpoint;

			var form = new List<IMultipartFormSection>
			{
				new MultipartFormDataSection("command", "APPEND"),
				new MultipartFormDataSection("media_id", mediaId),
				new MultipartFormDataSection("segment_index", $"{segmentIndex}"),
				new MultipartFormFileSection("media", mediaData, "", ""),
			};

			using (var req = UnityWebRequest.Post(endpoint, form))
			{
				string authHeader = await MakeAuthHeaderAsUser("POST", endpoint);
				req.SetRequestHeader("Authorization", authHeader);

				await req.SendWebRequest();

				TWHttpException.ThrowIfError(req);
			}
		}

		// Step 3 in https://developer.twitter.com/en/docs/media/upload-media/overview
		public async UniTask<TWMediaUploadFinalizeResponse> FinalizeMediaUpload(string mediaId)
		{
			var endpoint = UploadEndpoint;

			var form = new Dictionary<string, string>
			{
				{"command", "FINALIZE"},
				{"media_id", mediaId},
			};

			using (var req = FormatUtils.RequestPost(endpoint, form))
			{
				string authHeader = await MakeAuthHeaderAsUser("POST", endpoint, form);
				req.SetRequestHeader("Authorization", authHeader);

				await req.SendWebRequest();

				TWHttpException.ThrowIfError(req);

				string res = req.downloadHandler.text;
				return JsonConvert.DeserializeObject<TWMediaUploadFinalizeResponse>(res);
			}
		}

		// https://developer.twitter.com/en/docs/media/upload-media/api-reference/get-media-upload-status
		public async UniTask<TWMediaUploadStatusResponse> GetMediaUploadStatus(string mediaId)
		{
			var endpoint = UploadEndpoint;

			var form = new Dictionary<string, string>
			{
				{"command", "STATUS"},
				{"media_id", mediaId},
			};

			using (var req = UnityWebRequest.Get($"{endpoint}?{FormatUtils.UrlEncode(form)}"))
			{
				string authHeader = await MakeAuthHeaderAsUser("GET", endpoint, form);
				req.SetRequestHeader("Authorization", authHeader);

				await req.SendWebRequest();

				TWHttpException.ThrowIfError(req);

				string res = req.downloadHandler.text;
				return JsonConvert.DeserializeObject<TWMediaUploadStatusResponse>(res);
			}
		}

		// Generate an OAuth header. If user token exists, user it. Otherwise use app token.
		async UniTask<string> MakeAuthHeader(string httpMethod, string targetEndpoint, IDictionary<string, string> oauthParams = null)
		{
			if (_storage.UserTokenExists)
			{
				return await MakeAuthHeaderAsUser(httpMethod, targetEndpoint, oauthParams);
			}

			if (_storage.AppTokenExists)
			{
				return $"Bearer {_storage.AppAccessToken}";
			}

			throw new Exception("Neither app nor user has been authenticated");
		}

		// Generate an OAuth header using the user token that must have been granted earlier
		async UniTask<string> MakeAuthHeaderAsUser(string httpMethod, string targetEndpoint, IDictionary<string, string> extraParams = null)
		{
			if (!_storage.UserTokenExists)
			{
				throw new Exception("Failed authorizing user request: User token not granted");
			}

			await UniTask.SwitchToThreadPool();

			var oauthHeader = TWOAuth.MakeHeader(
				oauthToken: _storage.UserAccessToken,
				oauthTokenSecret: _storage.UserAccessTokenSecret,
				consumerKey: _app.ConsumerKey,
				consumerSecret: _app.ConsumerSecret,
				httpMethod: httpMethod,
				endpointUrl: targetEndpoint,
				extraParams: extraParams);

			await UniTask.SwitchToMainThread();

			return oauthHeader;
		}
	}
}