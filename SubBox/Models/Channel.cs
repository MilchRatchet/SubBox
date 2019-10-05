using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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
