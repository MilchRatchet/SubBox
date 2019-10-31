using System;
using System.ComponentModel.DataAnnotations;

namespace SubBox.Models
{
    public class Video
    {
        [Key]
        public string Id { get; set; }

        public DateTime PublishedAt { get; set; }

        public string PublishedAtString { get; set; }

        public string ChannelId { get; set; }

        public string ChannelName { get; set; }

        public string ChannelTitle { get; set; }

        public string ChannelPicUrl { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string ThumbnailUrl { get; set; }

        public string Duration { get; set; }

        public bool New { get; set; }

        public int List { get; set; }

        public int Index { get; set; }
    }
}
