using System;
using System.IO;

namespace SubBox.Models
{
    public class AppSettings
    {
        [Flags]
        public enum DownloadQuality { H2160F60, H2160F30, H1440F60, H1440F30, H1080F60, H1080F30, H720F60, H720F30 }

        public static int RetrievalTimeFrame { get; set; }
        public static int NewChannelTimeFrame { get; set; }
        public static int DeletionTimeFrame { get; set; }
        public static int PlaylistPlaybackSize { get; set; }
        public static string Color { get; set; }
        public static bool NightMode { get; set; }
        public static DateTime LastRefresh { get; set; }
        public static bool FirstStart { get; set; }
        public static bool AutoStart { get; set; }
        public static bool DevMode { get; set; }
        public static DownloadQuality PreferredQuality { get; set; }

        //Runtime vars
        public static bool GCMode { get; set; }
        public static bool DownloadReady { get; set; }

        public static async void Save()
        {
            string[] options = new string[] {RetrievalTimeFrame.ToString(), NewChannelTimeFrame.ToString(), DeletionTimeFrame.ToString(), PlaylistPlaybackSize.ToString(), Color, NightMode.ToString(), LastRefresh.ToString("O"), FirstStart.ToString(), AutoStart.ToString(), DevMode.ToString(), PreferredQuality.ToString() };
            await File.WriteAllLinesAsync("settings.txt", options);
        }
    }
}
