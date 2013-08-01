using System;
using System.IO;
using SevenZip;

namespace EmergencyBackup
{
    class Core
    {
        static void Main(string[] args)
        {
            if (args.Length >= 2 && args[0] != null && args[1] != null)
            {
                string _savePath = args[0];
                string _backupPath = args[1];

                if (Directory.Exists(_savePath))
                {
                    if (args.Length == 3)
                    {
                        int compression = 0;
                        if (args[2] != null)
                        {
                            try
                            {
                                compression = Convert.ToInt32(args[2]);
                                Compress(_savePath, _backupPath, (CompressionLevel)compression);
                            }
                            catch
                            {
                                Compress(_savePath, _backupPath);
                            }
                        }
                    }
                    else
                        Compress(_savePath, _backupPath);
                }
            }
        }

        private static void Compress(string savePath, string backupPath, CompressionLevel compressionLevel = CompressionLevel.None)
        {
            SevenZipCompressor compressor = new SevenZipCompressor();
            compressor.CustomParameters.Add("mt", "on");
            compressor.CompressionLevel = compressionLevel;
            compressor.ScanOnlyWritable = true;

            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);

            compressor.CompressDirectory(savePath, (Path.Combine(backupPath, GetTimeStamp()) + ".7z"));
        }

        private static string GetTimeStamp()
        {
            DateTime now = DateTime.Now;

            string dayNight;

            if (now.Hour < 12)
                dayNight = "Morning";
            else
                dayNight = "Night";

            return String.Format("{0}-{1}-{2}-{3}", now.Year, now.Month, now.Day, dayNight);
        }
    }
}