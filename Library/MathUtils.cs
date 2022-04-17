using System.Globalization;

namespace FastDownloader.FDLib
{
    internal class MathUtils
    {
        public static long FileSizeBytes(string file)
        {
            FileInfo info = new(file);
            long num = info.Length;
            return num;
        }
        public static double FileSizeKB(string file)
        {
            double num = FileSizeBytes(file) / 1024;
            double.TryParse("" + num, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
            num = Math.Round(num, 2);
            return num;
        }
        public static double FileSizeMB(string file)
        {
            double num = FileSizeKB(file) / 1024;
            double.TryParse("" + num, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
            num = Math.Round(num, 2);
            return num;
        }
        public static double FileSizeGB(string file)
        {
            double num = FileSizeMB(file) / 1024;
            double.TryParse("" + num, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
            num = Math.Round(num, 2);
            return num;
        }


        public static double FileSizeKB(double file)
        {
            double num = file / 1024;
            double.TryParse("" + num, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
            num = Math.Round(num, 2);
            return num;
        }
        public static double FileSizeMB(double file)
        {
            double num = FileSizeKB(file) / 1024;
            double.TryParse("" + num, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
            num = Math.Round(num, 2);
            return num;
        }
        public static double FileSizeGB(double file)
        {
            double num = FileSizeMB(file) / 1024;
            double.TryParse("" + num, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
            num = Math.Round(num, 2);
            return num;
        }

        public static string AdjustedFileSize(string file)
        {
            return (FileSizeBytes(file) < 1024) ? FileSizeBytes(file) + "B" : (FileSizeKB(file) < 1024) ? FileSizeKB(file) + "KB" : (FileSizeMB(file) < 1024) ? FileSizeMB(file) + "MB" : FileSizeGB(file) + "GB";
        }

        public static string AdjustedFileSize(double size)
        {
            return (Math.Round(double.Parse("" + size, NumberStyles.Any, CultureInfo.InvariantCulture), 2) < 1024) ? Math.Round(double.Parse("" + size, NumberStyles.Any, CultureInfo.InvariantCulture), 2) + "B" : (FileSizeKB(size) < 1024) ? FileSizeKB(size) + "KB" : (FileSizeMB(size) < 1024) ? FileSizeMB(size) + "MB" : FileSizeGB(size) + "GB";
        }

    }
}
