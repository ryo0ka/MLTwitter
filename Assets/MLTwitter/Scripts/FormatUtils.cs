using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace MLTwitter
{
	internal static class FormatUtils
	{
		// Create a POST request with a custom encoding applied to the form data
		public static UnityWebRequest RequestPost(string endpoint, IDictionary<string, string> form)
		{
			// Start as "PUT" so Unity's default form encoding will not happen.
			// Pass in a custom-encoded form data (which will not be re-encoded)
			var req = UnityWebRequest.Put(endpoint, UrlEncode(form));
			
			// Set it back to POST so it's a post request now
			req.method = "POST";
			
			// Manually set content type so that it's even more post a request
			req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
			
			// Use it
			return req;
		}

		// Custom HTTP form encoding
		public static string UrlEncode(IDictionary<string, string> form)
		{
			return string.Join("&", form.Select(p => $"{p.Key.Escape()}={p.Value.Escape()}"));
		}

		// For tweeting with special characters, you need to apply this non-standard encoding
		// to the HTTP request's form data and your OAuth signature, otherwise authentication will fail
		public static string Escape(this string data)
		{
			return Uri.EscapeDataString(data)
			          .Replace("!", "%21")
			          .Replace("'", "%27")
			          .Replace("(", "%28")
			          .Replace(")", "%29")
			          .Replace("*", "%2A");
		}
		
		public static UnityWebRequest RequestPostNoEscape(string endpoint, IDictionary<string, string> form)
		{
			var req = UnityWebRequest.Put(endpoint, UrlEncodeNoEscape(form));	
			req.method = "POST";
			return req;
		}

		// Custom HTTP form encoding
		static string UrlEncodeNoEscape(IDictionary<string, string> form)
		{
			return string.Join("&", form.Select(p => $"{p.Key}={p.Value}"));
		}
	}
}