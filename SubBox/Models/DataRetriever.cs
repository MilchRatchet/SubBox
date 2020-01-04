using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using SubBox.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubBox.Models
{
    public class DataRetriever
    {
        private static readonly string APIKey = Config.APIKey;

        private readonly YouTubeService service;

        private static int LifeTime = 7;

        private readonly int RefreshLimit = 120;

        public DataRetriever()
        {
            service = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = APIKey,

                ApplicationName = GetType().ToString()
            });
        }

        public void AddChannel(string name)
        {
            LifeTime = AppSettings.NewChannelTimeFrame;

            var Request = service.Channels.List("snippet");

            if (name.Length == 24)
            {
                Request.Id = name;
            }
            else
            {
                Request.ForUsername = name;
            }

            var Response = Request.Execute();

            try
            {
                Channel NewChannel = new Channel()
                {
                    Id = Response.Items.First().Id,

                    Username = name,

                    Displayname = Response.Items.First().Snippet.Title,

                    ThumbnailUrl = Response.Items.First().Snippet.Thumbnails.Default__.Url
                };

                using (AppDbContext context = new AppDbContext())
                {
                    context.Channels.Add(NewChannel);

                    context.SaveChanges();

                    List<string> list = RequestVideoIdsFromChannel(NewChannel.Id).GetAwaiter().GetResult();

                    if (list.Count == 0) return;

                    List<string> requests = CreateRequestList(list);

                    Task[] waitTasks = new Task[requests.Count];

                    int index = 0;

                    foreach (string str in requests)
                    {
                        waitTasks[index] = RequestVideosFromIds(str);

                        index++;
                    }

                    Task.WaitAll(waitTasks);
                }
            }
            catch (Exception)
            {
                Logger.Info(name + " couldn't be added");
            }
        }

        private async Task RequestVideosFromIds(string id)
        {
            var VideoRequest = service.Videos.List("snippet,contentDetails");

            VideoRequest.Id = id;

            var VideoResponse = await VideoRequest.ExecuteAsync();

            using (AppDbContext context = new AppDbContext())
            {
                Video[] videoList = context.Videos.ToArray();

                foreach(var item in VideoResponse.Items)
                {
                    try
                    {
                        Video v = ParseVideo(item, 0, 0);

                        context.Videos.Add(v);
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            Logger.Warn("Couldn't parse: " + item.Snippet.Title + " by " + item.Snippet.ChannelTitle);
                        }
                        catch (Exception)
                        {
                            Logger.Warn("Couldn't parse video information");
                        }

                        Logger.Error(e.Message);

                        Logger.Error(e.InnerException.ToString());

                        Logger.Error(e.StackTrace);

                        Logger.Error(e.Source);
                    }
                }

                try
                {
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    Logger.Warn("Couldn't add new videos");

                    Logger.Error(e.Message);

                    Logger.Error(e.InnerException.ToString());

                    Logger.Error(e.StackTrace);

                    Logger.Error(e.Source);
                }
            }
        }

        private async Task<List<string>> RequestVideoIdsFromChannel(string id)
        {
            var Request = service.Activities.List("snippet");

            Request.ChannelId = id;

            Request.MaxResults = 50;

            Request.PublishedAfter = DateTime.Now.AddDays(-LifeTime);

            var Response = await Request.ExecuteAsync();

            List<string> VideoIds = new List<string>();

            foreach (var item in Response.Items)
            {
                if (item.Snippet.Type == "upload")
                {
                    VideoIds.Add(item.Snippet.Thumbnails.Default__.Url.Split('/')[4]);
                }
            }

            return VideoIds;
        }

        private List<string> CreateRequestList(List<string> videoIds)
        {
            List<string> requests = new List<string>(videoIds.Count / 45 + 1);

            using (AppDbContext context = new AppDbContext())
            {
                List<Video> Videos = context.Videos.ToList();

                for (int i = 0; i < videoIds.Count / 45 + 1; i++)
                {
                    string requestId = "";

                    for (int j = i * 45; j < (i + 1) * 45; j++)
                    {
                        if (j >= videoIds.Count) break;

                        if (Videos.Any(v => v.Id == videoIds[j])) continue;

                        requestId += videoIds[j] + ",";
                    }

                    if (requestId != "")
                    {
                        requests.Add(requestId);
                    } 
                }
            }

            return requests;
        }

        public void UpdateVideoList()
        {
            int diffSec = (int) (DateTime.Now - AppSettings.LastRefresh).TotalSeconds;

            if (diffSec < RefreshLimit)
            {
                Logger.Info("Can only refresh every " + RefreshLimit + " seconds! " + (RefreshLimit - diffSec) + " seconds remaining");

                return;
            }

            AppSettings.LastRefresh = DateTime.Now;

            AppSettings.Save();

            GarbageCollector();

            Logger.Info("Refreshing...");

            LifeTime = AppSettings.RetrievalTimeFrame;

            List<Channel> Channels;

            long count = 0;

            using (AppDbContext context = new AppDbContext())
            {
                Channels = context.Channels.ToList();

                count = context.Videos.LongCount();
            }

            List<string> videoIds = new List<string>();

            Task<List<string>>[] tasks = new Task<List<string>>[Channels.Count];

            int index = 0;

            foreach(Channel ch in Channels)
            {
                tasks[index] = RequestVideoIdsFromChannel(ch.Id);

                index++;
            }

            Task.WaitAll(tasks);

            foreach(Task<List<string>> list in tasks)
            {
                videoIds.AddRange(list.Result);
            }

            if (videoIds.Count == 0) {
                Logger.Info("Finished loading 0 new Videos");

                return;
            };

            List<string> requests = CreateRequestList(videoIds);

            Task[] waitTasks = new Task[requests.Count];

            index = 0;

            foreach (string str in requests)
            {
                waitTasks[index] = RequestVideosFromIds(str);

                index++;
            }

            Task.WaitAll(waitTasks);

            using (AppDbContext context = new AppDbContext())
            {
                Logger.Info("Finished loading " + (context.Videos.LongCount() - count) + " new Videos");
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
                                    Logger.Warn("Couldn't parse: " + item.Snippet.Title + " by " + item.Snippet.ChannelTitle);
                                }
                                catch (Exception)
                                {
                                    Logger.Warn("Couldn't parse video information");
                                }

                                Logger.Error(e.Message);

                                Logger.Error(e.InnerException.ToString());

                                Logger.Error(e.StackTrace);

                                Logger.Error(e.Source);
                            }
                        }

                        try
                        {
                            context.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            Logger.Info("Couldn't add playlist");

                            Logger.Error(e.Message);

                            Logger.Error(e.InnerException.ToString());

                            Logger.Error(e.StackTrace);

                            Logger.Error(e.Source);
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
                    Logger.Info("Error retrieving the playlist of id: " + listId);

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

                Index = count,

                Description = item.Snippet.Description
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
            if (!AppSettings.GCMode)
            {
                Logger.Warn("Suppressed GC Call");

                return;
            }

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
    }

}
