using Newtonsoft.Json;
using System;
using System.IO;

namespace SubBox.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AppSettings
    {
        [Flags]
        public enum DownloadQuality { H2160F60, H2160F30, H1440F60, H1440F30, H1080F60, H1080F30, H720F60, H720F30 }

        [JsonProperty]
        public static int RetrievalTimeFrame { get; set; }
        [JsonProperty]
        public static int NewChannelTimeFrame { get; set; }
        [JsonProperty]
        public static int DeletionTimeFrame { get; set; }
        [JsonProperty]
        public static int PlaylistPlaybackSize { get; set; }
        [JsonProperty]
        public static string Color { get; set; }
        [JsonProperty]
        public static bool NightMode { get; set; }
        [JsonProperty]
        public static bool ConfirmWindow { get; set; }
        [JsonProperty]
        public static bool NewVersionNotification { get; set; }
        [JsonProperty]
        public static bool DownloadFinishedNotification { get; set; }
        [JsonProperty]
        public static bool VideosDeletedNotification { get; set; }
        [JsonProperty]
        public static bool ChannelAddedNotification { get; set; }
        [JsonProperty]
        public static int ChannelsPerPage { get; set; }
        [JsonProperty]
        public static DateTime LastRefresh { get; set; }
        [JsonProperty]
        public static bool FirstStart { get; set; }
        [JsonProperty]
        public static bool AutoStart { get; set; }
        [JsonProperty]
        public static bool DevMode { get; set; }
        [JsonProperty]
        public static DownloadQuality PreferredQuality { get; set; }

        //Runtime vars
        public static bool GCMode { get; set; }
        public static bool DownloadReady { get; set; }

        public static void Save()
        {
            using (StreamWriter file = File.CreateText(@"settings.json"))
            {
                JsonSerializer serializer = new JsonSerializer();

                serializer.Serialize(file, new AppSettings());
            }
        }
    }
}
