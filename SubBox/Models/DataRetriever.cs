﻿using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;
using SubBox.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubBox.Models
{
    public class DataRetriever
    {
        private static readonly string APIKey = Config.APIKey;

        private readonly YouTubeService service;

        private static readonly int DescLength = 120;

        private static int LifeTime = 7;

        public DataRetriever()
        {
            service = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = APIKey,

                ApplicationName = this.GetType().ToString()
            });
        }

        public void AddChannel(string name)
        {
            LifeTime = AppSettings.NewChannelTimeFrame;

            var Request = service.Channels.List("snippet");

            Request.ForUsername = name;

            var Response = Request.Execute();

            try
            {
                Channel NewChannel = new Channel()
                {
                    Id = Response.Items.First().Id,

                    Username = name,

                    Displayname = Response.Items.First().Snippet.Title,

                    ThumbnailUrl = Response.Items.First().Snippet.Thumbnails.Medium.Url
                };

                using (AppDbContext context = new AppDbContext())
                {
                    context.Channels.Add(NewChannel);

                    context.SaveChanges();

                    RequestVideosFromIds(RequestVideoIdsFromChannel(NewChannel.Id));
                }
            }
            catch (Exception)
            {

            }
        }

        private void RequestVideosFromIds(string id)
        {
            var VideoRequest = service.Videos.List("snippet,contentDetails");

            VideoRequest.Id = id;

            var VideoResponse = VideoRequest.Execute();

            using (AppDbContext context = new AppDbContext())
            {
                object LockObject = new object();

                Parallel.ForEach(VideoResponse.Items, (item) =>
                {
                    try
                    {
                        Video v = ParseVideo(item, 0, 0);

                        lock (LockObject)
                        {
                            if (!context.Videos.Any(i => i.Id == v.Id))
                            {
                                context.Videos.Add(v);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        try
                        {
                            Console.WriteLine("Couldn't parse: " + item.Snippet.Title + " by " + item.Snippet.ChannelTitle);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Couldn't parse video information");
                        }

                        Console.WriteLine(e.Message);

                        Console.WriteLine(e.InnerException);

                        Console.WriteLine(e.StackTrace);

                        Console.WriteLine(e.Source);
                    }
                });
                try
                {
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Couldn't add new videos");

                    Console.WriteLine(e.Message);

                    Console.WriteLine(e.InnerException);

                    Console.WriteLine(e.StackTrace);

                    Console.WriteLine(e.Source);
                }
            }
        }

        private string RequestVideoIdsFromChannel(string id)
        {
            var Request = service.Activities.List("snippet");

            Request.ChannelId = id;

            Request.MaxResults = 50;

            Request.PublishedAfter = DateTime.Now.AddDays(-LifeTime);

            var Response = Request.Execute();

            string VideoIds = "";

            foreach (var item in Response.Items)
            {
                if (item.Snippet.Type == "upload")
                {
                    VideoIds += item.Snippet.Thumbnails.Default__.Url.Split('/')[4] + ",";
                }
            }

            return VideoIds;
        }

        public void UpdateVideoList()
        {
            GarbageCollector();

            LifeTime = AppSettings.RetrievalTimeFrame;

            List<Channel> Channels;

            using (AppDbContext context = new AppDbContext())
            {
                Channels = context.Channels.ToList();
            }

            object LockObject = new object();

            string videoIds = "";

            Parallel.ForEach(Channels, (ch) =>
            {
                string list = RequestVideoIdsFromChannel(ch.Id);

                lock(LockObject)
                {
                    videoIds += list;
                }
            });

            /*
             * Here I wanted to split the list into lists of like 50 ids but it seems like youtube allows people to just request any amount of ids at one time
             */

            RequestVideosFromIds(videoIds);
        }

        public void AddPlaylist(int number, string listId)
        {
            GarbageCollector();

            int count = 0;

            using (AppDbContext context = new AppDbContext())
            {
                var listOfVideos = context.Videos;

                foreach (Video v in listOfVideos)
                {
                    if ((v.List == number)&&(v.Index>=count))
                    {
                        count = v.Index + 1;
                    }
                }
            }

            var request = service.PlaylistItems.List("contentDetails");

            request.MaxResults = 50;

            request.PlaylistId = listId;

            while (true)
            {
                try
                {
                    var response = request.Execute();

                    string videoId = "";

                    foreach (var item in response.Items)
                    {
                        videoId += item.ContentDetails.VideoId + ",";
                    }

                    videoId = videoId.Remove(videoId.Length - 1);

                    var videoRequest = service.Videos.List("snippet,contentDetails");

                    videoRequest.Id = videoId;

                    var videoResponse = videoRequest.Execute();

                    using (AppDbContext context = new AppDbContext())
                    {
                        foreach (var item in videoResponse.Items)
                        {
                            try
                            {
                                Video v = ParseVideo(item, number, count);

                                if (!context.Videos.Any(i => i.Id == v.Id))
                                {
                                    context.Videos.Add(v);

                                    count++;
                                }
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    Console.WriteLine("Couldn't parse: " + item.Snippet.Title + " by " + item.Snippet.ChannelTitle);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("Couldn't parse video information");
                                }

                                Console.WriteLine(e.Message);

                                Console.WriteLine(e.InnerException);

                                Console.WriteLine(e.StackTrace);

                                Console.WriteLine(e.Source);
                            }
                        }

                        try
                        {
                            context.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Couldn't add playlist");

                            Console.WriteLine(e.Message);

                            Console.WriteLine(e.InnerException);

                            Console.WriteLine(e.StackTrace);

                            Console.WriteLine(e.Source);
                        }

                    }

                    if (response.NextPageToken == null)
                    {
                        break;
                    }
                    else
                    { 
                        request.PageToken = response.NextPageToken;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Error retrieving the playlist of id: " + listId);

                    return;
                }
            }
        }

        private Video ParseVideo(Google.Apis.YouTube.v3.Data.Video item, int number, int count)
        {
            Video NewVideo = new Video()
            {
                Id = item.Id,

                PublishedAt = item.Snippet.PublishedAt.GetValueOrDefault(),

                PublishedAtString = item.Snippet.PublishedAt.GetValueOrDefault().Day + "." + item.Snippet.PublishedAt.GetValueOrDefault().Month + "." + item.Snippet.PublishedAt.GetValueOrDefault().Year + " " + item.Snippet.PublishedAt.GetValueOrDefault().Hour + ":" + ((item.Snippet.PublishedAt.GetValueOrDefault().Minute < 10) ? ("0" + item.Snippet.PublishedAt.GetValueOrDefault().Minute) : (item.Snippet.PublishedAt.GetValueOrDefault().Minute + "")),

                ChannelId = item.Snippet.ChannelId,

                ChannelName = "",

                ChannelTitle = item.Snippet.ChannelTitle,

                ChannelPicUrl = "",

                Title = item.Snippet.Title,

                ThumbnailUrl = item.Snippet.Thumbnails.Medium.Url,

                New = true,

                List = number,

                Index = count
            };

            if (number == 0)
            {
                using (AppDbContext context = new AppDbContext())
                {
                    Channel ch = context.Channels.Where(c => c.Id == item.Snippet.ChannelId).FirstOrDefault();

                    if (ch != null)
                    {
                        NewVideo.ChannelName = ch.Username;

                        NewVideo.ChannelPicUrl = ch.ThumbnailUrl;
                    }
                }
            }

            string desc = item.Snippet.Description;

            if (desc.Length < DescLength)
            {
                NewVideo.Description1 = desc;

                NewVideo.Description2 = "";
            }
            else if (desc.Length < 2 * DescLength)
            {
                NewVideo.Description1 = desc.Substring(0, DescLength);

                int index = NewVideo.Description1.LastIndexOf(" ");

                NewVideo.Description1 = NewVideo.Description1.Substring(0, index);

                NewVideo.Description2 = desc.Substring(index + 1, desc.Length - index - 1);
            }
            else
            {
                NewVideo.Description1 = desc.Substring(0, DescLength);

                int index = NewVideo.Description1.LastIndexOf(" ");

                NewVideo.Description1 = NewVideo.Description1.Substring(0, index);

                NewVideo.Description2 = desc.Substring(index + 1, DescLength) + "...";
            }

            string hour, min;

            if (item.ContentDetails.Duration.Contains('H'))
            {
                hour = Regex.Match(item.ContentDetails.Duration.Split('H')[0], @"(.{2})\s*$").Value;
            }
            else
            {
                hour = "";
            }

            if (item.ContentDetails.Duration.Contains('M'))
            {
                min = Regex.Match(item.ContentDetails.Duration.Split('M')[0], @"(.{2})\s*$").Value;
            }
            else
            {
                min = "";
            }

            string sec = Regex.Match(item.ContentDetails.Duration.Split('S')[0], @"(.{2})\s*$").Value;

            hour = new string(hour.Where(c => Enumerable.Range('0', 10).Contains(c)).ToArray());

            min = new string(min.Where(c => Enumerable.Range('0', 10).Contains(c)).ToArray());

            sec = new string(sec.Where(c => Enumerable.Range('0', 10).Contains(c)).ToArray());

            if ((hour != "") && (min.Length == 1))
            {
                min = "0" + min;
            }
            else if ((hour != "") && (min == ""))
            {
                min = "00";
            }
            else if (min == "")
            {
                min = "0";
            }

            if (sec.Length == 1)
            {
                sec = "0" + sec;
            }
            else if (sec == "")
            {
                sec = "00";
            }

            if (hour != "")
            {
                hour += ":";
            }

            min += ":";

            NewVideo.Duration = hour + min + sec;

            return NewVideo;
        }

        public void GarbageCollector()
        {
            LifeTime = AppSettings.DeletionTimeFrame;

            using (AppDbContext context = new AppDbContext())
            {
                var list = context.Videos;

                foreach(Video v in list)
                {
                    if ((!v.New)&&(v.List==0))
                    {
                        if (v.PublishedAt.AddDays(LifeTime) < DateTime.Now)
                        {
                            context.Videos.Remove(v);
                        }
                    }
                }

                context.Videos.RemoveRange(context.Videos.Where(v => v.Index < -AppSettings.PlaylistPlaybackSize));

                context.SaveChanges();
            }
        }

        public void PrintAllVideos()
        {
            List<Video> Videos;

            using (AppDbContext context = new AppDbContext())
            {
                Videos = context.Videos.ToList();
            }

            foreach (Video v in Videos)
            {
                Console.WriteLine(v.Title + " by " + v.ChannelName);
            }
        }
    }

}
