using System.ComponentModel.DataAnnotations;

namespace SubBox.Models
{
    public class Channel
    {
        [Key]
        public string Id { get; set; }

        public string Username { get; set; }

        public string Displayname { get; set; }

        public string ThumbnailUrl { get; set; }
    }
}
