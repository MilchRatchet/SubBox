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
            var result = await context.Videos.Where(v => v.New).Where(v => v.List == 0).OrderByDescending(v => v.PublishedAt.Ticks).ToListAsync();

            return result;
        }

        // GET: api/values/videos/old
        [HttpGet("videos/old")]
        public async Task<ActionResult<IEnumerable<Video>>> GetOldVideos()
        {
            var result = await context.Videos.Where(v => !v.New).Where(v => v.List == 0).OrderByDescending(v => v.PublishedAt.Ticks).ToListAsync();

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
            var result = await context.Videos.Where(v => v.List != 0).Where(v => v.Index == 0).OrderBy(v => v.List).ToListAsync();

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
            return new string[] { AppSettings.RetrievalTimeFrame.ToString(), AppSettings.NewChannelTimeFrame.ToString(), AppSettings.DeletionTimeFrame.ToString(), AppSettings.PlaylistPlaybackSize.ToString(), AppSettings.Color, AppSettings.NightMode.ToString() };
        }

        // POST: api/values/refresh
        [HttpPost("refresh")]
        public void Refresh()
        {
            Console.WriteLine("Refreshing...");

            DataRetriever fetcher = new DataRetriever();

            fetcher.UpdateVideoList();
        }

        // POST: api/values/channel
        [HttpPost("channel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void PostChannel([FromForm]Channel ch)
        {
            string name = ch.Username;

            Console.WriteLine("Adding "+name+" to Database...");

            DataRetriever fetcher = new DataRetriever();

            fetcher.AddChannel(name);
        }

        // POST: api/values/channel
        [HttpPost("channel/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void PostChannel(string name)
        { 
            Console.WriteLine("Adding " + name + " to Database...");

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
                var videos = context.Videos;

                foreach (Video v in videos)
                {
                    if (v.List >= count)
                    {
                        count = v.List + 1;
                    }
                }
            }

            Console.WriteLine("Adding Playlist with number: " + count);

            DataRetriever fetcher = new DataRetriever();

            fetcher.AddPlaylist(count, id);
        }

        // POST: api/values/list/next/number
        [HttpPost("list/next/{number}")]
        public void NextEntry(int number)
        {
            Video video = context.Videos.Where(v => v.List == number).Where(v => v.Index == 0).FirstOrDefault();

            if (video == null)
            {
                return;
            }

            video.New = false;

            context.Videos.Where(v => v.List == number).ToList().ForEach(v => v.Index -= 1);

            context.SaveChanges();
        }

        // POST: api/values/list/previous/number
        [HttpPost("list/previous/{number}")]
        public void PrevEntry(int number)
        {
            Video video = context.Videos.Where(v => v.List == number).Where(v => v.Index == -1).FirstOrDefault();

            if (video == null)
            {
                return;
            }

            video.New = true;

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
                    return;
                }

                Console.WriteLine("Reativating Video: " + video.Title + " by " + video.ChannelTitle);

                video.New = true;

                context.SaveChanges();
            }
            catch(Exception)
            {
                return;
            }
        }

        // POST: api/values/settings/RT/number
        [HttpPost("settings/RT/{number}")]
        public void SetRT(int number)
        {
            AppSettings.RetrievalTimeFrame = number;
        }

        // POST: api/values/settings/NCT/number
        [HttpPost("settings/NCT/{number}")]
        public void SetNCT(int number)
        {
            AppSettings.NewChannelTimeFrame = number;
        }

        // POST: api/values/settings/DT/number
        [HttpPost("settings/DT/{number}")]
        public void SetDT(int number)
        {
            AppSettings.DeletionTimeFrame = number;
        }

        // POST: api/values/settings/PPS/number
        [HttpPost("settings/PPS/{number}")]
        public void SetPPS(int number)
        {
            AppSettings.PlaylistPlaybackSize = number;
        }

        // POST: api/values/settings/night
        [HttpPost("settings/night")]
        public void SwitchNight()
        {
            AppSettings.NightMode = !AppSettings.NightMode;
        }

        // POST: api/values/settings/Color/number
        [HttpPost("settings/Color/{number}")]
        public void SetColor(string number)
        {
            AppSettings.Color = number;
        }

        [HttpPost("settings/save")]
        public void saveSettings()
        {
            AppSettings.Save();
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
                    return;
                }

                Console.WriteLine("Deleting Video: " + video.Title + " by " + video.ChannelTitle);

                video.New = false;

                if (video.List != 0)
                {
                    context.Videos.Remove(video);

                    context.Videos.Where(v => v.List == video.List).Where(v => v.Index > video.Index).ToList().ForEach(v => v.Index -= 1);
                }

                context.SaveChanges();
            }
            catch (Exception)
            {
                return;
            }
        }

        // DELETE: api/values/channel/id
        [HttpDelete("channel/{id}")]
        public void DeleteChannel(string id)
        {
            Channel channel = context.Channels.Find(id);

            if (channel == null)
            {
                return;
            }

            Console.WriteLine("Deleting Channel: " + channel.Displayname);

            context.Channels.Remove(channel);

            IEnumerable<Video> videos = context.Videos.Where(v => v.List == 0).Where(v => v.ChannelId == id).ToList();

            foreach (Video v in videos)
            {
                context.Videos.Remove(v);
            }

            context.SaveChanges();
        }
    }
}
