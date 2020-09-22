using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bug_Tracker.Models
{
    public class User
    {
        public Object _id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<Privilege> AdminStatus { get; set; }
    }

    public class Privilege
    {
        public Object _id { get; set; }
        public string Status { get; set; }
        public string ProjectID { get; set; }
    }
}