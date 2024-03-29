﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public static string Theme { get; set; } = "ThemeSubBoxLight";
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
        public static bool SmartListLoading { get; set; }
        [JsonProperty]
        public static bool HighlightNewVideos { get; set; }
        [JsonProperty]
        public static DateTime LastRefresh { get; set; }
        [JsonProperty]
        public static string FirstUse { get; set; }
        [JsonProperty]
        public static string PicOfTheDayUrl { get; set; } = "";
        [JsonProperty]
        public static string PicOfTheDayLink { get; set; } = "";
        [JsonProperty]
        public static string PicOfTheDayUser { get; set; }
        [JsonProperty]
        public static string Username { get; set; } = "Enter Username here...";
        [JsonProperty]
        public static string Notes { get; set; } = "Enter your notes here...";
        [JsonProperty]
        public static bool FirstStart { get; set; } = true;
        [JsonProperty]
        public static bool AutoStart { get; set; } = true;
        [JsonProperty]
        public static bool DevMode { get; set; } = false;
        [JsonProperty]
        public static bool UpdateYDL { get; set; } = true;
        [JsonProperty]
        public static DownloadQuality PreferredQuality { get; set; }
        [JsonProperty]
        public static List<bool> DisplayPlaylists { get; set; }
        [JsonProperty]
        public static bool AutoDeleteShorts { get; set; } = false;
        [JsonProperty]
        public static bool UseEmbedLinks { get; set; } = false;

        //Runtime vars
        public static bool GCMode { get; set; }
        public static bool DownloadReady { get; set; }

        public static void Save()
        {
            using (StreamWriter file = File.CreateText(@"settings.json.temp"))
            {
                JsonSerializer serializer = new JsonSerializer();

                serializer.Serialize(file, new AppSettings());
            }

            File.Replace("settings.json.temp", "settings.json", "settings.json.backup");
        }

        public static void EnsureDisplayPlaylists()
        {
            if (DisplayPlaylists == null)
            {
                DisplayPlaylists = new List<bool>(10);

                IncreaseDisplayPlaylists();
            }
        }

        public static void IncreaseDisplayPlaylists()
        {
            for (int i = 0; i < 10; i++)
            {
                DisplayPlaylists.Add(true);
            }
        }

        public static void SetFirstUse()
        {
            if (FirstUse == null || FirstUse == "")
            {
                DateTime now = DateTime.Now;

                FirstUse = now.Day + "." + now.Month + "." + now.Year;
            }
        }
    }
}
