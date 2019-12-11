using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SubBox.Models
{
    public class LocalVideo
    {
        public string Dir { get; set; }

        public long Size { get; set; }

        public Video Data { get; set; }
    }
}
