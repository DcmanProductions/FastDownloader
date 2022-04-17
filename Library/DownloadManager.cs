using ChaseLabs.CLLogger;
using ChaseLabs.CLLogger.Interfaces;
using System.Net;
using Timer = System.Timers.Timer;

namespace FastDownloader.FDLib
{
    public class DownloadManager
    {
        private ILog log = LogManager.Init().SetDumpMethod(DumpType.NoDump).SetMinimumLogType(Lists.LogTypes.All).SetLogDirectory(Path.Combine(Path.GetTempPath(), "latest.log"));
        public string Name { get; private set; }
        public string URL { get; }
        public string OutputDirectory { get; }
        public long Size { get; }
        public int NumberOfChunks { get; }
        public float Percentage { get; private set; }
        public long BytesDownloaded { get; private set; }
        public string DownloadSpeed { get; private set; }
        public long BytesPerSecond { get; private set; }
        public string Extension { get; private set; }

        private string part_output;
        private DownloadManager(string url, string outputDirectory, int chunks, bool print)
        {
            URL = url;
            OutputDirectory = outputDirectory;
            NumberOfChunks = chunks;

            HttpClient client = new();
            HttpRequestMessage message = new(HttpMethod.Head, url);
            HttpResponseMessage response = client.Send(message);
            if (response.IsSuccessStatusCode)
            {
                if (long.TryParse(response.Content.Headers.First(n => n.Key.Equals("Content-Length")).Value.First(), out long _size))
                {
                    Size = _size;
                    Extension = response.Content.Headers.First(n => n.Key.Equals("Content-Type")).Value.First().Split("/").Last();
                    if (response.Content.Headers.Contains("Content-Disposition"))
                    {
                        try
                        {
                            Name = response.Content.Headers.First(n => n.Key.Equals("Content-Disposition")).Value.First();
                        }
                        catch (Exception)
                        {
                            Name = URL.Split("/").Last();
                        }
                    }
                    else
                    {
                        Name = URL.Split("/").Last();
                        foreach (char c in "#<>?/\\:'\"=|`+$&^%*!{} @".ToCharArray())
                        {
                            Name = Name.Replace("" + c, "");
                        }
                        Name = Name.EndsWith(Extension) ? Name : $"{Name}.{Extension}";
                    }
                }
                else
                {
                }
            }
            else
            {
                Console.WriteLine("No Success Code");
                Console.WriteLine(response.StatusCode);
                Name = "FAILED";
            }
        }

        public static void Init(string url, string outputDirectory, bool print, int chunks = -1)
        {
            DownloadManager manager = new(url, outputDirectory, chunks == -1 ? Environment.ProcessorCount : chunks, print);
            if (!(string.IsNullOrWhiteSpace(manager.Name) || manager.Size == 0 || manager.NumberOfChunks <= 0))
            {
                manager.Run();
            }
        }


        void Run()
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
        /// Downloads the individual sections
        /// </summary>
        /// <param name="chunks"></param>
        /// <returns></returns>
        string[] DownloadParts(Chunk[] chunks)
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
                percentage /= chunks.Length;
                DownloadSpeed = $"{MathUtils.AdjustedFileSize(speed)}/s";
                BytesPerSecond = speed;
                Percentage = percentage;
                BytesDownloaded = downloaded;


                var Top = Console.GetCursorPosition().Top;
                Console.SetCursorPosition(0, Top);
                Console.WriteLine(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, Top);
                log.Debug($"[{new string('|', (int)percentage)}{new string(' ', 100 - (int)percentage)}]   (D:{MathUtils.AdjustedFileSize(speed)}/s | T:{MathUtils.AdjustedFileSize(downloaded)}/{MathUtils.AdjustedFileSize(Size)} | C:{NumberOfChunks} | P:{Math.Round(percentage, 2)}%)");
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

        /// <summary>
        /// Stitches each section together to create the final product
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        bool Stitch(string[] parts)
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

        Chunk[] GetChunks(int count)
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
    }
    struct Chunk
    {
        public long Start;
        public long End;
        public long BytesPerSecond;
        public long BytesDownloaded;
        public long Size => End - Start;
    }
}
