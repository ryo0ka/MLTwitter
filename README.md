#MLTwitter

Twitter client primarily targeted to Magic Leap on Unity.

###Sample Code

    // Read video file from storage
    byte[] video = File.ReadAllBytes(videoFilePath);
    
    // Upload the video to Twitter (this is just a media upload; not a tweet)
    string videoMediaId = await _client.UploadVideo(video, (upload, encode) =>
    {
        Debug.Log($"Uploading: {upload * 100:0}% done, encoding: {encode * 100:0}% done...");
    });
    
    // Tweet the video
    await _client.UpdateStatus("Uploading a video capture", videoMediaId);


###Features 

- [3-legged OAuth](https://developer.twitter.com/en/docs/basics/authentication/overview/3-legged-oauth) (demo included)
- [PIN-based OAuth](https://developer.twitter.com/en/docs/basics/authentication/overview/pin-based-oauth)
- [App authentication](https://developer.twitter.com/en/docs/basics/authentication/overview/application-only)
- [Update status](https://developer.twitter.com/en/docs/tweets/post-and-engage/overview) aka "tweeting" (demo included)
- [Upload media](https://developer.twitter.com/en/docs/media/upload-media/overview) with image/video (demo included)
- General GET/POST methods with [OAuth header](https://developer.twitter.com/en/docs/basics/authentication/overview/using-oauth)

###Demo

[Demo recorded footage](https://twitter.com/ryoichirooka/status/1120167709470105601)

1. User authentication (3-legged OAuth) using Helio
1. Fetch and present a user profile on Unity UI
1. Capture and upload video to Twitter media server
1. Tweet with entities: URL links and media

To run the demo on your own:

- Instantiate `Demo/CredentialRepository.cs`
  - Right click → `Create` → `Scriptable Object`
- Fill in the repository's text fields with [your Twitter app credentials](https://developer.twitter.com/en/apps)
- Pass the repository to `DemoController` in `Demo/DemoScene.scene`
- Run that scene on Magic Leap

###Requirements

- Unity version 2019.1 and later
- C# 7 language features
- [UniRx](https://github.com/neuecc/UniRx)

###Disclaimer

- Primarily for myself and developing inconsistently (issues & PRs are appreciated)
- MIT license

###Motivation

- I couldn't find any loyalty-free cross-platform Twitter clients for Unity (except broken ones) so decided to make my own. The core is completely managed in .NET Standard 2.0 and survives IL2CPP so it should be portable to the majority of platforms including upcoming smartglasses; as PoC it works in Lumin runtime.