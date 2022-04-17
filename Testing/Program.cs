using FastDownloader.FDLib;

namespace FastDownload.IO;

public class Program
{
    static void Main(string[] args)
    {

        if (args.Length == 0)
        {
            Console.Write("Enter URL or URL list file: ");
            string path = Console.ReadLine() ?? "";
            Console.Write($"Enter Number of Chunks (Leave blank for default: {Environment.ProcessorCount}): ");
            string chunk = Console.ReadLine() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (path.StartsWith("http"))
                {
                    if (!string.IsNullOrWhiteSpace(chunk) && int.TryParse(chunk, out int c))
                        DownloadManager.Init(path, Environment.CurrentDirectory, true, c);
                    else
                        DownloadManager.Init(path, Environment.CurrentDirectory, true);
                }
                else
                {
                    try
                    {
                        Console.Write("How many should download at a time (default is 1): ");
                        string parallel = Console.ReadLine() ?? "1";
                        Parallel.ForEach(File.ReadAllLines(Path.GetFullPath(path)), new() { MaxDegreeOfParallelism = int.TryParse(parallel, out int p) ? p : 1 }, line =>
                        {
                            if (!string.IsNullOrWhiteSpace(chunk) && int.TryParse(chunk, out int c))
                                DownloadManager.Init(line, Environment.CurrentDirectory, true, c);
                            else
                                DownloadManager.Init(line, Environment.CurrentDirectory, true);
                        });
                    }
                    catch { }
                }
            }
        }
        else if (args.Length == 1)
        {
            if (args[0].StartsWith("http"))
            {
                DownloadManager.Init(args[0], Environment.CurrentDirectory, true);
            }
            else
            {
                try
                {
                    Console.Write("How many should download at a time (default is 1): ");
                    string parallel = Console.ReadLine() ?? "1";
                    Parallel.ForEach(File.ReadAllLines(Path.GetFullPath(args[0])), new() { MaxDegreeOfParallelism = int.TryParse(parallel, out int p) ? p : 1 }, line =>
                    {
                        DownloadManager.Init(line, Environment.CurrentDirectory, true);
                    });
                }
                catch { }
            }
        }
        else if (args.Length == 2)
        {

            if (int.TryParse(args[1], out int result))
            {
                if (args[0].StartsWith("http"))
                {
                    DownloadManager.Init(args[0], Environment.CurrentDirectory, true, result);
                }
                else
                {
                    try
                    {
                        Console.Write("How many should download at a time (default is 1): ");
                        string parallel = Console.ReadLine() ?? "1";
                        Parallel.ForEach(File.ReadAllLines(Path.GetFullPath(args[0])), new() { MaxDegreeOfParallelism = int.TryParse(parallel, out int p) ? p : 1 }, line =>
                        {
                            DownloadManager.Init(line, Environment.CurrentDirectory, true, result);
                        });
                    }
                    catch { }
                }

            }
        }
    }
}