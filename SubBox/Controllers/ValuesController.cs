using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubBox.Data;
using SubBox.Models;

namespace SubBox.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly AppDbContext context;

        public ValuesController(AppDbContext context)
        {
            this.context = context;   
        }

        // GET: api/values/videos
        [HttpGet("videos")]
        public async Task<ActionResult<IEnumerable<Video>>> GetVideos()
        {
            var result = await context.Videos.Where(v => v.New && v.List == 0).OrderByDescending(v => v.PublishedAt.Ticks).ToListAsync();

            return result;
        }

        // GET: api/values/localvideos
        [HttpGet("localvideos")]
        public IEnumerable<KeyValuePair<string,LocalVideo>> GetLocalVideos()
        {
            var result = LocalCollection.DownloadedVideos.ToList();

            return result;
        }

        // GET: api/values/videos/old
        [HttpGet("videos/old")]
        public async Task<ActionResult<IEnumerable<Video>>> GetOldVideos()
        {
            var result = await context.Videos.Where(v => !v.New && v.List == 0).OrderByDescending(v => v.PublishedAt.Ticks).ToListAsync();

            return result;
        }

        // GET: api/values/channels
        [HttpGet("channels")]
        public async Task<ActionResult<IEnumerable<Channel>>> GetChannels()
        {
            var result = await context.Channels.ToListAsync();

            return result;
        }

        // GET: api/values/lists
        [HttpGet("lists")]
        public async Task<ActionResult<IEnumerable<Video>>> GetLists()
        {
            var result = await context.Videos.Where(v => v.List != 0 && v.Index == 0).OrderBy(v => v.List).ToListAsync();

            return result;
        }

        // GET: api/values/list/all/number
        [HttpGet("list/all/{number}")]
        public async Task<ActionResult<IEnumerable<Video>>> GetAllVideosInList(int number)
        {
            var result = await context.Videos.Where(v => v.List == number).OrderBy(v => v.Index).ToListAsync();

            return result;
        }

        // GET: api/values/settings
        [HttpGet("settings")]
        public string[] GetSettings()
        {
            return new string[] { AppSettings.RetrievalTimeFrame.ToString(), AppSettings.NewChannelTimeFrame.ToString(), AppSettings.DeletionTimeFrame.ToString(), AppSettings.PlaylistPlaybackSize.ToString(), AppSettings.Color, AppSettings.NightMode.ToString(), ((int) AppSettings.PreferredQuality).ToString() };
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
            var result = await context.Tags.ToListAsync();

            return result;
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void PostChannel([FromForm]Channel ch)
        {
            string name = ch.Username;

            Logger.Info("Adding "+name+" to Database...");

            DataRetriever fetcher = new DataRetriever();

            fetcher.AddChannel(name);
        }

        // POST: api/values/channel
        [HttpPost("channel/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
        public void SaveSettings()
        {
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

        // DELETE: api/values/video/id
        [HttpDelete("video/{id}")]
        public void DeleteVideo(string id)
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

        // DELETE: api/values/localvideo/dir
        [HttpDelete("localvideo/{dir}")]
        public void DeleteLocalVideo(string dir)
        {
            dir = dir.Replace('*', '\\');

            try
            {
                string path = @"wwwroot" + dir;

                System.IO.File.Delete(path);

                Logger.Info("Deleted local video at " + path);
            }
            catch (Exception e)
            {
                Logger.Info("Could not delete Video");

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
