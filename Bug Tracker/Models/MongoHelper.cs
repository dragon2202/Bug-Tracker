using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Driver;

namespace Bug_Tracker.Models
{
    public class MongoHelper
    {
        public static IMongoClient client { get; set; }
        public static IMongoDatabase database { get; set; }
        public static string MongoConnection = "mongodb+srv://admin:admin@bug-tracker-ukvqd.mongodb.net/test?retryWrites=true&w=majority";
        public static string MongoDatabase = "bug_tracker";

        public static IMongoCollection<Models.User> users_collection { get; set; }
        public static IMongoCollection<Models.Project> project_collection { get; set; }
        public static IMongoCollection<Models.Message> message_collection { get; set; }
        public static IMongoCollection<Models.Ticket> ticket_collection { get; set; }
        public static IMongoCollection<Models.Picture> picture_collection { get; set; }
        public static IMongoCollection<Models.AccountRecovery> recovery_collection { get; set; }
        internal static void ConnectToMongo() {
            try
            {
                client = new MongoClient(MongoConnection);
                database = client.GetDatabase(MongoDatabase);
            }
            catch (Exception) {
              throw;
            }
        }

    }
}