namespace MLTwitter
{
	// Twitter App credentials
	public interface ITWCredentialRepository
	{
		string ConsumerKey { get; }
		string ConsumerSecret { get; }
		string AccessToken { get; }
		string AccessTokenSecret { get; }
	}
}