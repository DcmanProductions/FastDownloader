using ChaseLabs.CLLogger;
using ChaseLabs.CLLogger.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using Timer = System.Timers.Timer;

namespace FastDownloader.FDLib;

public class DownloadManager
{
    #region Fields

    private static ILog log = LogManager.Init().SetDumpMethod(DumpType.NoDump).SetMinimumLogType(Lists.LogTypes.All).SetLogDirectory(Path.Combine(Path.GetTempPath(), "latest.log"));
    private string part_output;

    #endregion Fields

    #region Private Constructors

    private DownloadManager(string url, string outputDirectory, string filename, int chunks, bool print)
    {
        IsYoutube = url.Contains("youtube.com") || url.Contains("youtu.be");
        URL = IsYoutube ? YoutubeManager.GetDownloadLink(url) : url;
        OutputDirectory = outputDirectory;
        NumberOfChunks = chunks;
        PrintVarbosly = print;
        Name = filename;
        if (TryGetHeaders(URL, out var headers) && headers != null && headers.ContentLength != null)
        {
            Size = (long)headers.ContentLength;
            Name = IsYoutube ? YoutubeManager.GetVideoTitle(url) + ".mp4" : string.IsNullOrWhiteSpace(filename) ? GetFileName(URL) : filename;
        }
        else
        {
            log.Fatal($"Unable to connect to \"{url}\"");
        }
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            Name = Name.Replace(c.ToString(), "");
        }
    }

    #endregion Private Constructors

    #region Properties

    public long BytesDownloaded { get; private set; }
    public long BytesPerSecond { get; private set; }
    public string DownloadSpeed { get; private set; }
    public string Extension { get; private set; }
    public bool IsYoutube { get; }
    public string Name { get; private set; }
    public int NumberOfChunks { get; }
    public string OutputDirectory { get; }
    public float Percentage { get; private set; }
    public bool PrintVarbosly { get; private set; }
    public long Size { get; }
    public string URL { get; }

    #endregion Properties

    #region Public Methods

    public static void Init(string url, string outputDirectory, string filename, bool print, int chunks = -1)
    {
        DownloadManager manager = new(url, outputDirectory, filename, chunks == -1 ? Environment.ProcessorCount : chunks, print);
        if (!(string.IsNullOrWhiteSpace(manager.Name) || manager.Size == 0 || manager.NumberOfChunks <= 0))
        {
            manager.Run();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(manager.Name))
            {
                log.Error($"Name is EMPTY");
            }
            else if (manager.Size == 0)
            {
                log.Error("Size is 0b");
            }
            else if (manager.NumberOfChunks <= 0)
            {
                log.Error("Chucks is less than or equal to zero");
            }
        }
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Downloads the individual sections
    /// </summary>
    /// <param name="chunks"></param>
    /// <returns></returns>
    private string[] DownloadParts(Chunk[] chunks)
    {
        part_output = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "fdl", $"{Name}-{DateTime.Now.Ticks}")).FullName;
        string[] parts = new string[chunks.Length];
        Timer Update = new()
        {
            Enabled = true,
            AutoReset = true,
            Interval = 1000,
        };
        Update.Elapsed += (sender, args) =>
        {
            long speed = 0;
            float percentage = 0f;
            long downloaded = 0;
            foreach (Chunk chunk in chunks)
            {
                speed += chunk.BytesPerSecond;
                downloaded += chunk.BytesDownloaded;
                percentage += downloaded / chunk.Size;
            }
            percentage = downloaded / Size;
            percentage /= chunks.Length;
            DownloadSpeed = $"{MathUtils.AdjustedFileSize(speed)}/s";
            BytesPerSecond = speed;
            Percentage = percentage;
            BytesDownloaded = downloaded;

            var Top = Console.GetCursorPosition().Top;
            Console.SetCursorPosition(0, Top);
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Top);
            //log.Debug($"[{new string('|', (int)percentage)}{new string(' ', 100 - (int)percentage)}] (D:{MathUtils.AdjustedFileSize(speed)}/s | T:{MathUtils.AdjustedFileSize(downloaded)}/{MathUtils.AdjustedFileSize(Size)} | C:{NumberOfChunks} | P:{Math.Round(percentage, 2)}%)");
            log.Debug($"(D:{MathUtils.AdjustedFileSize(speed)}/s | T:{MathUtils.AdjustedFileSize(downloaded)}/{MathUtils.AdjustedFileSize(Size)} | C:{NumberOfChunks} | P:{Math.Round(percentage, 2)}%)");
            Console.SetCursorPosition(0, Top);
        };
        Update.Start();
        Parallel.For(0, chunks.Length, i =>
        {
            long lastRecordedBytes = 0;
            string file = Path.Combine(part_output, $"{Name}_{i}.part");
            Timer timer = new()
            {
                Enabled = true,
                AutoReset = true,
                Interval = 1000,
            };
            timer.Elapsed += (sender, args) =>
            {
                chunks[i].BytesDownloaded = new FileInfo(file).Length;
                chunks[i].BytesPerSecond = chunks[i].BytesDownloaded - lastRecordedBytes;
                lastRecordedBytes = chunks[i].BytesDownloaded;
            };
            HttpWebRequest request = WebRequest.CreateHttp(URL);
            request.AddRange(chunks[i].Start, chunks[i].End);
            WebResponse response = request.GetResponse();
            FileStream fs = new(file, FileMode.OpenOrCreate, FileAccess.Write);
            response.GetResponseStream().CopyToAsync(fs).Wait();
            fs.Flush();
            fs.Dispose();
            fs.Close();
            parts[i] = file;
            timer.Stop();
        });
        Update.Stop();
        return parts;
    }

    private Chunk[] GetChunks(int count)
    {
        Chunk[] chunks = new Chunk[count];
        long chunkSize = Size / count;
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i] = new Chunk()
            {
                Start = i == 0 ? 0 : chunkSize * i + 1,
                End = i == 0 ? chunkSize : i == chunks.Length - 1 ? Size : chunkSize * i + chunkSize,
            };
        }
        return chunks;
    }

    private string GetFileName(string url)
    {
        if (TryGetHeaders(url, out var headers) && headers != null && headers.ContentDisposition != null && headers.ContentDisposition.FileName != null)
        {
            return headers.ContentDisposition.FileName;
        }
        else
        {
            string tmp = url.Split('/').Last();
            tmp = tmp.Split('.').First() + "." + tmp.Split('.').Last().Split('?').First();

            return tmp;
        }
    }

    private void Run()
    {
        if (File.Exists(Path.Combine(OutputDirectory, Name)))
        {
            string fileExtension = new FileInfo(Path.Combine(OutputDirectory, Name)).Extension.Trim('.');
            string fileName = Name.Replace(fileExtension, "").Trim('.');
            Name = $"{fileName} ({Directory.GetFiles(OutputDirectory, $"{fileName}*.{fileExtension}").Length}).{fileExtension}";
        }
        long start = DateTime.Now.Ticks;
        log.Info($"Downloading \"{Name}\" ({MathUtils.AdjustedFileSize(Size)})");
        Stitch(DownloadParts(GetChunks(NumberOfChunks)));
        Directory.Delete(part_output, true);
        log.Info("Done Downloading...");
        TimeSpan span = TimeSpan.FromTicks(DateTime.Now.Ticks - start);
        log.Debug($"Download took {span.Hours}h {span.Minutes}m {span.Seconds}s {span.Milliseconds}ms");
    }

    /// <summary>
    /// Stitches each section together to create the final product
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    private bool Stitch(string[] parts)
    {
        FileStream fileStream = new(Path.Combine(OutputDirectory, Name), FileMode.Create, FileAccess.Write);
        for (int i = 0; i < parts.Length; i++)
        {
            string file = parts[i];
            FileStream fs = new(file, FileMode.Open, FileAccess.Read);
            fs.CopyToAsync(fileStream).Wait();
            fs.Flush();
            fs.Dispose();
            fs.Close();
        }
        fileStream.Flush();
        fileStream.Dispose();
        fileStream.Close();
        return false;
    }

    private bool TryGetHeaders(string url, out HttpContentHeaders? headers)
    {
        HttpClient client = new();
        HttpRequestMessage message = new(HttpMethod.Head, url);
        HttpResponseMessage response = client.Send(message);
        if (response.IsSuccessStatusCode)
        {
            headers = response.Content.Headers;
            return true;
        }
        headers = null;
        return false;
    }

    #endregion Private Methods

    #region Structs

    private struct Chunk
    {
        #region Fields

        public long BytesDownloaded;
        public long BytesPerSecond;
        public long End;
        public long Start;

        #endregion Fields

        #region Properties

        public long Size => End - Start;

        #endregion Properties
    }

    #endregion Structs
}