using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;

/*https://stackoverflow.com/questions/55721449/mongodb-insert-multiple-types-of-objects-in-single-a-collection*/
namespace Bug_Tracker.Models
{
    public class Ticket
    {
        public Object _id { get; set; }
        public Details details { get; set; }
        public List<Comments> comments { get; set; }
        public List<Histories> histories { get; set; }
        public List<Attachments> attachments { get; set; }
    }
    public class Details
    { 
        public string ticketTitle { get; set; }
        public string ticketDescription { get; set; }
        public string assignDeveloper { get; set; }
        public string devID { get; set; }
        public string submitter { get; set; }
        public string submitterID { get; set; }
        public string projectName { get; set; }
        public string project_id { get; set; }
        public string ProjectManagerID { get; set; }
        public string ticketPriority { get; set; }
        public string ticketStatus { get; set; }
        public string ticketType { get; set; }
        public string created { get; set; }
        public string updated { get; set; }
}

    public class Comments
    {
        public Object _id { get; set; }
        public string commenter { get; set; }
        public string commenterID { get; set; }
        public string comment { get; set; }
        public string created { get; set; }
}

    public class Histories
    {
        public Object _id { get; set; }
        public string Editor { get; set; }
        public Details oldValue { get; set; }
        public Details newValue { get; set; }
        public string dateChanged { get; set; }
    }

    /* 
        https://stackoverflow.com/questions/2188134/what-variable-type-should-i-use-to-save-an-image
        https://www.dotnetperls.com/image
    */
    public class Attachments
    {
        public Object _id { get; set; }
        public Object pictureID { get; set; }
        public string uploader { get; set; }
        public string uploaderID { get; set; }
        public string notes { get; set; }
        public string created { get; set; }
    }
}