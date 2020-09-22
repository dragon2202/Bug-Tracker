using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bug_Tracker.Models
{
    public class Picture
    {
        public Object _id { get; set; }
        public string fileName { get; set; }
        public string PictureDataAsString { get; set; }
    }
}