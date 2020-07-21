using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SubBox.Data;
using SubBox.Models;

namespace SubBox.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly AppDbContext context;

        private static readonly object LockObject = new object();

        public ValuesController(AppDbContext context)
        {
            this.context = context;   
        }

        // GET: api/values/videos
        [HttpGet("videos")]
        public async Task<ActionResult<IEnumerable<Video>>> GetVideos()
        {
            return await context.Videos.Where(v => v.New && v.List == 0).OrderByDescending(v => v.PublishedAt.Ticks).ToListAsync();
        }

        // GET: api/values/localvideos
        [HttpGet("localvideos")]
        public IEnumerable<KeyValuePair<string,LocalVideo>> GetLocalVideos()
        {
            return LocalCollection.DownloadedVideos.ToList();
        }

        // GET: api/values/videos/old
        [HttpGet("videos/old")]
        public async Task<ActionResult<IEnumerable<Video>>> GetOldVideos()
        {
            return await context.Videos.Where(v => !v.New && v.List == 0).OrderByDescending(v => v.PublishedAt.Ticks).ToListAsync();
        }

        // GET: api/values/channels
        [HttpGet("channels")]
        public IEnumerable<Channel> GetChannels()
        {
            return context.Channels.ToList().OrderBy(ch => ch.Displayname);
        }

        // GET: api/values/lists
        [HttpGet("lists")]
        public async Task<ActionResult<IEnumerable<Video>>> GetLists()
        {
            return await context.Videos.Where(v => v.List != 0 && v.Index == 0).OrderBy(v => v.List).ToListAsync();
        }

        // GET: api/values/list/all/number
        [HttpGet("list/all/{number}")]
        public async Task<ActionResult<IEnumerable<Video>>> GetAllVideosInList(int number)
        {
            return await context.Videos.Where(v => v.List == number).OrderBy(v => v.Index).ToListAsync(); ;
        }

        // GET: api/values/settings
        [HttpGet("settings")]
        public string GetSettings()
        {
            return JsonConvert.SerializeObject(new AppSettings());
        }

        // GET: api/values/information
        [HttpGet("information")]
        public string[] GetInformation()
        {

            long size = 0;

            foreach (KeyValuePair<string, LocalVideo> lv in LocalCollection.DownloadedVideos)
            {
                size += lv.Value.Size;
            }

            size /= 1024;

            size /= 1024;

            string disk = Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory);

            DriveInfo drive = DriveInfo.GetDrives().First(x => x.Name == disk);

            long availableSpace = 0;

            if (drive != null) availableSpace = drive.AvailableFreeSpace / (1024 * 1024);

            return new string[]
            {
                Startup.BuildVersion,
                "MilchRatchet",
                "" + context.Videos.LongCount(),
                "" + context.Videos.Where(v => v.List != 0).LongCount(),
                "" + context.Videos.Where(v => v.New == false).LongCount(),
                "" + context.Channels.LongCount(),
                "" + LocalCollection.DownloadedVideos.Count,
                size + " MiB",
                availableSpace + " Mib",
                AppSettings.LastRefresh.ToString("G"),
                AppSettings.DevMode ? "On" : "Off",
                AppSettings.AutoStart ? "Enabled" : "Disabled"
            };
        }

        // GET: api/values/first
        [HttpGet("first")]
        public bool GetFirstStart()
        {
            return AppSettings.FirstStart;
        }

        // GET: api/values/tags
        [HttpGet("tags")]
        public async Task<ActionResult<IEnumerable<Tag>>> GetTags()
        {
            return await context.Tags.ToListAsync();
        }

        // GET: api/values/status/{kind}/{key}
        [HttpGet("status/{kind}/{key}")]
        public async Task<ActionResult<StatusUpdate>> GetStatusUpdate(string kind, string key)
        {
            return await StatusBoard.GetStatus(kind, key);
        }

        // POST: api/values/firstdone
        [HttpPost("firstdone")]
        public void SetFirstStart()
        {
            if (!AppSettings.FirstStart) return;

            AppSettings.FirstStart = false;

            Logger.Info("Tutorial finished. You can always revisit it through the the link at the bottom of the site!");

            AppSettings.Save();
        }

        // POST: api/values/refresh
        [HttpPost("refresh")]
        public void Refresh()
        {
            DataRetriever fetcher = new DataRetriever();

            fetcher.UpdateVideoList();
        }

        // POST: api/values/channel
        [HttpPost("channel")]
        public void PostChannel([FromForm]Channel ch)
        {
            string name = ch.Username;

            Logger.Info("Adding "+name+" to Database...");

            DataRetriever fetcher = new DataRetriever();

            fetcher.AddChannel(name);

            Downloader.SyncChannelPictures();
        }

        // POST: api/values/channel
        [HttpPost("channel/{name}")]
        public void PostChannel(string name)
        { 
            Logger.Info("Adding " + name + " to Database...");

            DataRetriever fetcher = new DataRetriever();

            fetcher.AddChannel(name);
        }

        // POST: api/values/list/add/id
        [HttpPost("list/add/{id}/{number}")]
        public void AddList(string id, int number)
        {
            int count = number;

            if (number == 0)
            {
                count = 1;

                var videos = context.Videos;

                foreach (Video v in videos)
                {
                    if (v.List >= count)
                    {
                        count = v.List + 1;
                    }
                }
            }

            Logger.Info("Adding Playlist");

            DataRetriever fetcher = new DataRetriever();

            fetcher.AddPlaylist(count, id);
        }

        // POST: api/values/list/next/number
        [HttpPost("list/next/{number}")]
        public void NextEntry(int number)
        {
            context.Videos.Where(v => v.List == number).ToList().ForEach(v => v.Index -= 1);

            Video video = context.Videos.Where(v => v.List == number && v.Index == 1).FirstOrDefault();

            if (video == null)
            {
                Logger.Warn("End of Playlist found");

                context.Videos.RemoveRange(context.Videos.Where(v => v.List == number));
            }

            context.SaveChanges();
        }

        // POST: api/values/list/previous/number
        [HttpPost("list/previous/{number}")]
        public void PrevEntry(int number)
        {
            Video video = context.Videos.Where(v => v.List == number && v.Index == -1).FirstOrDefault();

            if (video == null)
            {
                Logger.Warn("No previous video found for playlist: " + number);

                return;
            }

            context.Videos.Where(v => v.List == number).ToList().ForEach(v => v.Index += 1);

            context.SaveChanges();
        }

        // POST: api/values/list/jump/{number}/{target}
        [HttpPost("list/jump/{number}/{target}")]
        public void JumpToEntry(int number, int target)
        {
            Video video = context.Videos.Where(v => v.List == number && v.Index == target).FirstOrDefault();

            if (video == null)
            {
                Logger.Warn("List " + number + " does not contain video at index " + target);

                return;
            }

            context.Videos.Where(v => v.List == number).ToList().ForEach(v => v.Index -= target);

            context.SaveChanges();
        }

        // POST: api/values/list/invert/{number}
        [HttpPost("list/invert/{number}")]
        public void InvertList(int number)
        {
            context.Videos.Where(v => v.List == number).ToList().ForEach(v => v.Index *= -1);

            context.SaveChanges();
        }

        // POST: api/values/video/id
        [HttpPost("video/{id}")]
        public void ReactivateVideo(string id)
        {
            try
            {
                Video video = context.Videos.Find(id);

                if (video == null)
                {
                    Logger.Warn("video: " + id + " was requested but is not in db");

                    return;
                }

                Logger.Info("Reativating Video: " + video.Title + " by " + video.ChannelTitle);

                video.New = true;

                context.SaveChanges();
            }
            catch(Exception e)
            {
                Logger.Warn("video: " + id + "could not be reactivated");

                Logger.Error(e.Message);

                return;
            }
        }

        //Post: api/values/download/id
        [HttpPost("download/{id}")]
        public void DownloadVideo(string id)
        {
            try
            {
                Downloader.DownloadVideo(id);
            }
            catch(Exception e)
            {
                Logger.Warn("video: " + id + "could not be downloaded");

                Logger.Error(e.Message);

                return;
            }
        }

        //POST: api/values/settings/syncChannelPictures
        [HttpPost("settings/syncChannelPictures")]
        public void SyncChannelPictures()
        {
            Logger.Info("Syncing channel pictures...");

            Downloader.SyncChannelPictures();

            Logger.Info("Done");
        }

        //POST: api/values/settings/type/value
        [HttpPost("settings/{type}/{value}")]
        public void SetSetting(string type, string value)
        {
            switch (type.ToUpper())
            {
                case "NCT":
                    AppSettings.NewChannelTimeFrame = int.Parse(value);

                    break;

                case "RT":
                    AppSettings.RetrievalTimeFrame = int.Parse(value);

                    break;

                case "DT":
                    AppSettings.DeletionTimeFrame = int.Parse(value);

                    break;

                case "PPS":
                    AppSettings.PlaylistPlaybackSize = int.Parse(value);

                    break;

                case "PQ":
                    AppSettings.PreferredQuality = (AppSettings.DownloadQuality) int.Parse(value);

                    break;

                case "NIGHT":
                    AppSettings.NightMode = !AppSettings.NightMode;

                    break;

                case "COLOR":
                    AppSettings.Color = value;

                    break;

                default:
                    Logger.Warn("Unknown setting was tried to change");

                    break;
            }
        }


        //POST: api/values/settings/save
        [HttpPost("settings/save")]
        public async void SaveSettings()
        {
            string raw = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            JsonConvert.DeserializeObject(raw, typeof(AppSettings));

            AppSettings.Save();
        }

        // POST: api/values/tags/add/{name}
        [HttpPost("tags/add/{name}")]
        public void AddTag(string name)
        {
            try
            {
                context.Tags.Add(new Tag{ Name = name, Filter = string.Empty});

                context.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.Warn("Couldn't add Tag " + name);

                Logger.Error(e.Message);
            }
        }

        // POST: api/values/tags/add/{name}
        [HttpPost("tags/set/{name}/{filter}")]
        public void SetTag(string name, string filter)
        {
            try
            {
                filter = filter.Substring(1, filter.Length - 1);

                Tag tag = context.Tags.Find(name);

                tag.Filter = filter;

                context.Tags.Update(tag);

                context.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.Warn("Couldn't update Tag " + name);

                Logger.Error(e.Message);
            }
        }

        //POST: api/values/close
        [HttpPost("close")]
        public void Close()
        {
            Logger.Warn("Shutdown was forced through API");

            Environment.Exit(0);
        }

        // DELETE: api/values/video/id
        [HttpDelete("video/{id}")]
        public void DeleteVideo(string id)
        {
            lock (LockObject)
            {
                try
                {
                    Video video = context.Videos.Find(id);

                    if (video == null)
                    {
                        Logger.Warn("video:" + id + " was requested but is not present in db");

                        return;
                    }

                    Logger.Info("Deleting Video: " + video.Title + " by " + video.ChannelTitle);

                    video.New = false;

                    if (video.List != 0)
                    {
                        context.Videos.Remove(video);

                        if (video.Index >= 0)
                        {
                            context.Videos.Where(v => v.List == video.List && v.Index > video.Index).ToList().ForEach(v => v.Index -= 1);
                        }
                        else
                        {
                            context.Videos.Where(v => v.List == video.List && v.Index < video.Index).ToList().ForEach(v => v.Index += 1);
                        }
                    }

                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    Logger.Warn("video: " + id + " could not be deleted");

                    Logger.Error(e.Message);

                    return;
                }
            }
        }

        // DELETE: api/values/localvideo/dir
        [HttpDelete("localvideo/{dir}/{thumbdir}")]
        public void DeleteLocalVideo(string dir, string thumbdir)
        {
            dir = dir.Replace('*', '\\');

            thumbdir = thumbdir.Replace('*', '\\');

            try
            {
                string path = @"wwwroot" + '\\' + dir;

                System.IO.File.Delete(path);

                Logger.Info("Deleted local video at " + path);
            }
            catch (Exception e)
            {
                Logger.Info("Could not delete Video");

                Logger.Error(e.Message);

                return;
            }

            try
            {
                string path = @"wwwroot" + '\\' + thumbdir;

                System.IO.File.Delete(path);

                Logger.Info("Deleted thumbnail at " + path);
            }
            catch (Exception e)
            {
                Logger.Info("Could not delete thumbnail");

                Logger.Error(e.Message);

                return;
            }

            LocalCollection.CollectAllDownloadedVideos();
        }

        // DELETE: api/values/channel/id
        [HttpDelete("channel/{id}")]
        public void DeleteChannel(string id)
        {
            Channel channel = context.Channels.Find(id);

            if (channel == null)
            {
                Logger.Warn("channel:" + id + " was requested but is not present in db");

                return;
            }

            Logger.Info("Deleting Channel: " + channel.Displayname);

            context.Channels.Remove(channel);

            IEnumerable<Video> videos = context.Videos.Where(v => v.List == 0 && v.ChannelId == id).ToList();

            foreach (Video v in videos)
            {
                context.Videos.Remove(v);
            }

            context.SaveChanges();
        }

        // DELETE: api/values/tags/{name}
        [HttpDelete("tags/delete/{name}")]
        public void DeleteTag(string name)
        {
            try
            {
                context.Tags.Remove(context.Tags.Find(name));

                context.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.Warn("Couldn't delete Tag " + name);

                Logger.Error(e.Message);
            }
        }
    }
}
