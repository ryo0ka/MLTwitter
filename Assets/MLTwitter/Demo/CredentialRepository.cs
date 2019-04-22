using UnityEngine;

namespace MLTwitter.Demo
{
	internal class CredentialRepository : ScriptableObject, ITWCredentialRepository, ITWCredentialStorage
	{
		[Header("Twitter app credentials")]
		[SerializeField]
		string _consumerKey;

		[SerializeField]
		string _consumerSecret;

		[SerializeField]
		string _accessToken;

		[SerializeField]
		string _accessTokenSecret;

		[SerializeField, Header("Remember to update manifest.json as well")]
		string _callbackUrl;

		[Header("Runtime token storage. Leave these empty")]
		[SerializeField]
		string _appAccessToken;

		[SerializeField]
		string _userAccessToken;

		[SerializeField]
		string _userAccessTokenSecret;

		string ITWCredentialRepository.ConsumerKey => _consumerKey;
		string ITWCredentialRepository.ConsumerSecret => _consumerSecret;
		string ITWCredentialRepository.AccessToken => _accessToken;
		string ITWCredentialRepository.AccessTokenSecret => _accessTokenSecret;

		string ITWCredentialStorage.AppAccessToken
		{
			get => _appAccessToken;
			set => _appAccessToken = value;
		}

		string ITWCredentialStorage.UserAccessToken
		{
			get => _userAccessToken;
			set => _userAccessToken = value;
		}

		string ITWCredentialStorage.UserAccessTokenSecret
		{
			get => _userAccessTokenSecret;
			set => _userAccessTokenSecret = value;
		}

		bool ITWCredentialStorage.AppTokenExists => !string.IsNullOrEmpty(_appAccessToken);
		bool ITWCredentialStorage.UserTokenExists => !string.IsNullOrEmpty(_userAccessToken);

		public void ClearStorage()
		{
			_appAccessToken = null;
			_userAccessToken = null;
			_userAccessTokenSecret = null;
		}

		public string CallbackUrl => _callbackUrl;
	}
}