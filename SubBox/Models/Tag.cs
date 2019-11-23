using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SubBox.Models
{
    public class Tag
    {
        [Key]
        public string Name { get; set; }
        public string Filter { get; set; }
    }


}
