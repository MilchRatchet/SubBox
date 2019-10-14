using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;
using SubBox.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

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

                    RequestVideosFromChannel(NewChannel.Id);
                }
            }
            catch (Exception)
            {

            }
        }

        private void RequestVideosFromChannel(string Id)
        {
            var Request = service.Search.List("snippet");

            Request.ChannelId = Id;

            Request.Order = SearchResource.ListRequest.OrderEnum.Date;

            Request.MaxResults = 50;

            Request.PublishedAfter = DateTime.Now.AddDays(-LifeTime);

            var Response = Request.Execute();

            string VideoIds = "";

            foreach (var item in Response.Items)
            {
                if (item.Id.Kind == "youtube#video")
                {
                    VideoIds += item.Id.VideoId + ",";
                }
            }

            var VideoRequest = service.Videos.List("snippet,contentDetails");

            VideoRequest.Id = VideoIds;

            var VideoResponse = VideoRequest.Execute();

            using (AppDbContext context = new AppDbContext())
            {
                foreach (var item in VideoResponse.Items)
                {
                    try
                    {
                        Video NewVideo = new Video()
                        {
                            Id = item.Id,

                            PublishedAt = item.Snippet.PublishedAt.GetValueOrDefault(),

                            PublishedAtString = item.Snippet.PublishedAt.GetValueOrDefault().Day + "." + item.Snippet.PublishedAt.GetValueOrDefault().Month + "." + item.Snippet.PublishedAt.GetValueOrDefault().Year + " " + item.Snippet.PublishedAt.GetValueOrDefault().Hour + ":" + ((item.Snippet.PublishedAt.GetValueOrDefault().Minute < 10) ? ("0" + item.Snippet.PublishedAt.GetValueOrDefault().Minute) : (item.Snippet.PublishedAt.GetValueOrDefault().Minute + "")),

                            ChannelId = item.Snippet.ChannelId,

                            ChannelName = context.Channels.Where(c => c.Id == item.Snippet.ChannelId).First().Username,

                            ChannelTitle = context.Channels.Where(c => c.Id == item.Snippet.ChannelId).First().Displayname,

                            ChannelPicUrl = context.Channels.Where(c => c.Id == item.Snippet.ChannelId).First().ThumbnailUrl,

                            Title = item.Snippet.Title,

                            ThumbnailUrl = item.Snippet.Thumbnails.Medium.Url,

                            New = true,

                            List = 0,

                            Index = 0
                        };

                        string desc = item.Snippet.Description;

                        if (desc.Length < DescLength)
                        {
                            NewVideo.Description1 = desc;

                            NewVideo.Description2 = "";
                        }
                        else if (desc.Length < 2 * DescLength)
                        {
                            NewVideo.Description1 = desc.Substring(0,DescLength);

                            int index = NewVideo.Description1.LastIndexOf(" ");

                            NewVideo.Description1 = NewVideo.Description1.Substring(0, index);

                            NewVideo.Description2 = desc.Substring(index + 1,desc.Length - index - 1);
                        }
                        else
                        {
                            NewVideo.Description1 = desc.Substring(0, DescLength);

                            int index = NewVideo.Description1.LastIndexOf(" ");

                            NewVideo.Description1 = NewVideo.Description1.Substring(0, index);

                            NewVideo.Description2 = desc.Substring(index + 1, DescLength) + "...";
                        }

                        string min = item.ContentDetails.Duration.Split('M')[0].Substring(2);

                        string sec = item.ContentDetails.Duration.Split('M')[1].Split('S')[0];

                        if (sec.Length == 1)
                        {
                            sec = "0" + sec;
                        }

                        NewVideo.Duration = min + ":" + sec;

                        context.Videos.Add(NewVideo);

                        context.SaveChanges();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Couldn't add: " + item.Snippet.Title + " by " + item.Snippet.ChannelTitle);
                    }
                }
            }
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

            foreach (Channel ch in Channels)
            {
                RequestVideosFromChannel(ch.Id);
            }
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

                                string min = item.ContentDetails.Duration.Split('M')[0].Substring(2);

                                string sec = item.ContentDetails.Duration.Split('M')[1].Split('S')[0];

                                if (sec.Length == 1)
                                {
                                    sec = "0" + sec;
                                }

                                NewVideo.Duration = min + ":" + sec;

                                context.Videos.Add(NewVideo);

                                context.SaveChanges();

                                count++;
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Couldn't add: " + item.Snippet.Title + " by " + item.Snippet.ChannelTitle);
                            }
                        }
                    }

                    if (response.NextPageToken == null)
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine(response.NextPageToken);

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

        public void GarbageCollector()
        {
            LifeTime = AppSettings.DeletionTimeFrame;

            using (AppDbContext context = new AppDbContext())
            {
                var list = context.Videos;

                foreach(Video v in list)
                {
                    if (!v.New)
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
