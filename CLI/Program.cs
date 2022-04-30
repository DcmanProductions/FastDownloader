using CLIArgumentBuilder;
using FastDownloader.FDLib;

namespace FastDownload.CLI;

public class Program
{
    static void Main(string[] args)
    {

        string url = string.Empty;
        int parts = Environment.ProcessorCount;
        string output = Path.GetFullPath(".");
        string fileName = "file";

        ArgumentBuilder argumentBuilder = new(createHelp: true);
        argumentBuilder.Add("i", "input", "the input url or url list file", true, true, arg => url = arg);
        argumentBuilder.Add("f", "fileName", "The output file name, ex: file.zip", true, true, arg => fileName = arg);
        argumentBuilder.Add("o", "output", "The output directory, ex: /path/to/download/", true, false, arg => output = arg);
        argumentBuilder.Add("p", "parts", "the number of parts to split the download", true, false, arg =>
        {
            if (int.TryParse(arg, out int p))
            {
                parts = p;
            }
            else
            {
                Console.Error.WriteLine($"Unable to parse parts amount {arg}");
                Environment.Exit(1);
            }
        });

        if (args.Length == 0)
        {
            Console.WriteLine(argumentBuilder.GetHelp());
            Environment.Exit(0);
        }
        else
        {
            argumentBuilder.Parse(args);
            DownloadManager.Init(url, output, false, parts);
        }

    }
}