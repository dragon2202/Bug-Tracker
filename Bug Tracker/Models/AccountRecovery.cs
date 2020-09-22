using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bug_Tracker.Models
{
    public class AccountRecovery
    { 
        public Object _id { get; set; }
        public string accountID { get; set; }
        public string token { get; set; }
        public DateTime expirationDateTime { get; set; }
    }
}