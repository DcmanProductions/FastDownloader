using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace FastDownloader.FDLib
{
    public static class YoutubeManager
    {
        public static string GetDownloadLink(string url)
        {
            YoutubeClient client = new();
            return client.Videos.Streams.GetManifestAsync(client.Videos.GetAsync(url).Result.Id).Result.GetMuxedStreams().GetWithHighestVideoQuality().Url;
        }
        public static string GetVideoTitle(string url)
        {
            YoutubeClient client = new();
            return client.Videos.GetAsync(url).Result.Title;
        }
    }
}
