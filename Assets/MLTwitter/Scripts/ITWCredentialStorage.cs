namespace MLTwitter
{
	// Runtime credential storage (e.g. access tokens)
	public interface ITWCredentialStorage
	{
		string AppAccessToken { get; set; }
		string UserAccessToken { get; set; }
		string UserAccessTokenSecret { get; set; }

		bool AppTokenExists { get; }
		bool UserTokenExists { get; }
	}
}