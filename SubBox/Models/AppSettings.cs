using System;
using System.IO;

namespace SubBox.Models
{
    public class AppSettings
    {
        public static int RetrievalTimeFrame { get; set; }
        public static int NewChannelTimeFrame { get; set; }
        public static int DeletionTimeFrame { get; set; }
        public static int PlaylistPlaybackSize { get; set; }   
        public static string Color { get; set; }
        public static bool NightMode { get; set; }
        public static DateTime LastRefresh { get; set; }
        public static bool FirstStart { get; set; }

        //Runtime vars
        public static bool GCMode { get; set; }

        public static async void Save()
        {
            string[] options = new string[] {RetrievalTimeFrame.ToString(), NewChannelTimeFrame.ToString(), DeletionTimeFrame.ToString(), PlaylistPlaybackSize.ToString(), Color, NightMode.ToString(), LastRefresh.ToString("O"), FirstStart.ToString() };
            await File.WriteAllLinesAsync("settings.txt", options);
        }
    }
}
