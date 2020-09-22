using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bug_Tracker.Models
{
    public class Project
    {
        public Object _id { get; set; }
        public string ProjectName { get; set; }
        public string ProjectManagerID { get; set; }
        public string ProjectManagerName { get; set; }
        public string Description { get; set; }
        public string[] UserID { get; set; }//Users Assigned
        public string[] TicketID { get; set; }//Tickets Associated to Project
    }

}