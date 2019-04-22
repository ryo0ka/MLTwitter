using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MLTwitter
{
	// OAuth-related functions
	public static class TWOAuth
	{
		// Empty parameters
		static readonly Dictionary<string, string> Empty = new Dictionary<string, string>();

		// Generate an OAuth header body
		public static string MakeHeader(
			string oauthToken,
			string oauthTokenSecret,
			string consumerKey,
			string consumerSecret,
			string httpMethod,
			string endpointUrl,
			IDictionary<string, string> extraParams = null)
		{
			var allParams = new SortedDictionary<string, string>
			{
				{"oauth_consumer_key", consumerKey},
				{"oauth_signature_method", "HMAC-SHA1"},
				{"oauth_timestamp", $"{CurrentTick():0}"},
				{"oauth_nonce", $"{Guid.NewGuid():N}"},
				{"oauth_version", "1.0"},
				{"oauth_token", oauthToken},
			};

			foreach (var pair in extraParams ?? Empty)
			{
				allParams.Add(pair.Key, pair.Value);
			}

			var signatureKey = string.Join("&", consumerSecret, oauthTokenSecret);
			var allParamsStr = string.Join("&", allParams.Select(p => $"{p.Key}={p.Value.Escape()}"));
			var signatureData = string.Join("&", httpMethod, endpointUrl.Escape(), allParamsStr.Escape());
			var signature = CalcSignature(signatureKey, signatureData);
			var requestSignature = string.Join(",", allParams.Select(p => $"{p.Key}=\"{p.Value.Escape()}\""));
			var header = $"OAuth {requestSignature}, oauth_signature=\"{signature.Escape()}\"";

			return header;
		}

		static string CalcSignature(string key, string data)
		{
			using (var enc = new HMACSHA1(Encoding.ASCII.GetBytes(key)))
			{
				return Convert.ToBase64String(enc.ComputeHash(Encoding.ASCII.GetBytes(data)));
			}
		}

		static double CurrentTick()
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			return (DateTime.UtcNow - epoch).TotalSeconds;
		}

		// Take in parameters (the part after "?" in URL) into key-value pairs.
		public static IDictionary<string, string> ParseParameters(string res)
		{
			var entries = new Dictionary<string, string>();
			foreach (var entry in res.Split('&'))
			{
				var pair = entry.Split('=');
				entries[pair[0]] = pair[1];
			}

			return entries;
		}
	}
}