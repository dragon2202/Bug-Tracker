using Bug_Tracker.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;

namespace Bug_Tracker.Controllers
{
    //https://stackoverflow.com/questions/20942042/customized-authorization-attribute-in-mvc-4-with-roles
    public class MyRoleAuthorization : AuthorizeAttribute
    {
        /// <summary>
        /// the allowed types
        /// </summary>
        readonly string[] allowedTypes;

        /// <summary>
        /// Default constructor with the allowed user types
        /// </summary>
        public MyRoleAuthorization(params string[] allowedTypes)
        {
            this.allowedTypes = allowedTypes;
        }

        /// <summary>
        /// Gets the allowed types
        /// </summary>
        public string[] AllowedTypes
        {
            get { return this.allowedTypes; }
        }

        /// <summary>
        /// Gets the authorize user
        /// </summary>
        private string AuthorizeUser(AuthorizationContext filterContext)
        {
            if (filterContext.RequestContext.HttpContext != null)
            {
                var context = filterContext.RequestContext.HttpContext;
                string roleName = Convert.ToString(context.Session["AdminStatus"]);
                switch (roleName)
                {
                    case "Admin":
                    case "Manager":
                    case "Submitter":
                    case "Developer":
                    case "IsUser":
                        return roleName;
                    default:
                        return "None";//Triggers unauthorized
                }
            }
            throw new ArgumentException("filterContext");
        }

        /// <summary>
        /// The authorization override
        /// </summary>
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentException("filterContext");
            string authUser = AuthorizeUser(filterContext);
            if (!this.AllowedTypes.Any(x => x.Equals(authUser, StringComparison.CurrentCultureIgnoreCase)))//Triggers unauthorized
            {
                filterContext.Result = new RedirectResult("~/Error/Unauthorized");
                return;
            }
        }


    }

    public class UserController : Controller
    {

        // GET: DashBoard
        [MyRoleAuthorization("Manager", "Admin", "Developer", "Submitter", "IsUser")]
        public ActionResult Dashboard()
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            List<Project> projectList = new List<Project>();
            List<Ticket> ticketList = new List<Ticket>();
            List<User> userList = new List<User>();
            //Grabs information different from a User as Guest doesn't have an ID
            if (Session["ID"].ToString() == "Guest")
            {
                var projectFilter = Builders<Project>.Filter.Eq("ProjectManagerID", Session["ID"]);
                var projectResult = MongoHelper.project_collection.Find(projectFilter).ToList();
                if (projectResult != null)
                {
                    for (int i = 0; i < projectResult.Count; i++)
                    {
                        for (int j = 0; j < projectResult[i].TicketID.Length; j++)
                        {
                            var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult[i].TicketID[j]);
                            var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
                            if (ticketResult != null)
                            {
                                ticketList.Add(ticketResult);
                            }
                        }
                    }
                }
                var tupleModel = new Tuple<List<Project>, List<Ticket>, List<User>>(projectResult, ticketList, userList);
                return View(tupleModel);
            }
            else //If User
            {
                var userFilter = Builders<User>.Filter.Eq("_id", Session["ID"]);
                var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
                if (userResult != null)
                {
                    userList.Add(userResult);
                    for (int i = 0; i < userResult.AdminStatus.Count(); i++)
                    {
                        var projectFilter = Builders<Project>.Filter.Eq("_id", userResult.AdminStatus[i].ProjectID);
                        var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
                        if (projectResult != null)
                        {
                            projectList.Add(projectResult);
                            for (int j = 0; j < projectResult.TicketID.Length; j++)
                            {
                                var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult.TicketID[j]);
                                var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
                                if (ticketResult != null)
                                {
                                    ticketList.Add(ticketResult);
                                }
                            }
                        }
                    }
                }
                var tupleModel = new Tuple<List<Project>, List<Ticket>, List<User>>(projectList, ticketList, userList);
                return View(tupleModel);
            }
        }

        //Admin Pages Start ------------------------------------------------------------------------------------------------------------------------->
        //GET: ManageRoleProject
        [MyRoleAuthorization("Manager", "Admin", "Developer", "Submitter", "IsUser")]
        public ActionResult ManageRoleProject()
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
            MongoHelper.database.GetCollection<Project>("Project");
            if (Session["ID"].ToString() == "Guest" && Session["AdminStatus"].ToString() != "Manager")
            {
                List<Project> projectList = new List<Project>();
                return View(projectList);
            }
            else
            {
                var filter = Builders<Project>.Filter.Eq("ProjectManagerID", Session["ID"]);
                var result = MongoHelper.project_collection.Find(filter).ToList();
                return View(result);
            }
        }
        // GET: ManageRole
        [MyRoleAuthorization("Manager")]
        public ActionResult ManageRole(string id)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            var projectFilter = Builders<Project>.Filter.Eq("_id", id);
            var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();

            List<User> userList = new List<User>();
            List<Project> projectList = new List<Project>();

            if (projectResult != null)
            {
                MongoHelper.users_collection =
                    MongoHelper.database.GetCollection<User>("Users");
                for (int i = 0; i < projectResult.UserID.Length; i++)
                {
                    var filter = Builders<User>.Filter.Eq("_id", projectResult.UserID[i]);
                    var result = MongoHelper.users_collection.Find(filter).FirstOrDefault();
                    if (result != null && result._id.ToString() != projectResult.ProjectManagerID)//If Not Project Manager
                    {
                        userList.Add(result);
                    }
                }
                projectList.Add(projectResult);
                var tupleModel = new Tuple<List<Project>, List<User>>(projectList, userList);
                return View(tupleModel);
            }
            else
            {
                var tupleModel = new Tuple<List<Project>, List<User>>(projectList, userList);//Return Empy Lists
                return View(tupleModel);
            }
        }
        //POST: ManageRole from ManageRole Page
        //Changes selected User(s) from ManageRole and changes their status for a specific project
        [HttpPost]
        public ActionResult ManageRole(FormCollection collection)
        {
            //https://stackoverflow.com/questions/5647873/split-string-after-comma-and-till-string-ends-asp-net-c-sharp
            if (collection["PrivilegeID"] != null)
            {
                MongoHelper.ConnectToMongo();
                MongoHelper.users_collection =
                    MongoHelper.database.GetCollection<User>("Users");
                string[] token = collection["PrivilegeID"].ToString().Split(',');

                for (int i = 0; i < token.Length; i++)
                {
                    var subFilter = Builders<Privilege>.Filter.Eq("_id", token[i]);
                    var filter = Builders<User>.Filter.ElemMatch("AdminStatus", subFilter);
                    var update = Builders<User>.Update.Set("AdminStatus.$.Status", collection["Status"]);
                    MongoHelper.users_collection.UpdateOne(filter, update);

                }
                return RedirectToAction("ManageRole", "User", new { id = collection["ProjectID"], message = "success" });
            }
            else
            {
                return RedirectToAction("ManageRole", "User", new { id = collection["ProjectID"], message = "NoUser" });
            }
        }

        [MyRoleAuthorization("Manager", "Admin", "Developer", "Submitter", "IsUser")]
        //GET: ManageUser
        public ActionResult ManageUser()
        {
            if ((Session["AdminStatus"].ToString() == "Manager" && Session["ID"].ToString() == "Guest") || (Session["AdminStatus"].ToString() == "Admin" && Session["ID"].ToString() == "Guest"))//Guest Manager or Guest Admin
            {
                MongoHelper.ConnectToMongo();
                MongoHelper.project_collection =
                    MongoHelper.database.GetCollection<Project>("Project");
                var filter = Builders<Project>.Filter.Eq("ProjectManagerID", Session["ID"]);//ProjectManagerID for Guest created Project is Guest
                var result = MongoHelper.project_collection.Find(filter).ToList();
                return View(result);
            }
            else//If User
            {
                MongoHelper.ConnectToMongo();
                MongoHelper.users_collection =
                    MongoHelper.database.GetCollection<User>("Users");
                MongoHelper.project_collection =
                    MongoHelper.database.GetCollection<Project>("Project");
                List<Project> projectList = new List<Project>();
                var filter = Builders<User>.Filter.Eq("_id", Session["ID"]);
                var result = MongoHelper.users_collection.Find(filter).FirstOrDefault();
                if (result != null)
                {
                    for (int i = 0; i < result.AdminStatus.Count; i++)
                    {
                        if (result.AdminStatus[i].Status == "Manager" || result.AdminStatus[i].Status == "Admin")//Pushes Project into List if user is either a Manager or Admin
                        {
                            var projectFilter = Builders<Project>.Filter.Eq("_id", result.AdminStatus[i].ProjectID);
                            var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
                            if (projectResult != null)
                            {
                                projectList.Add(projectResult);
                            }
                        }
                    }
                }
                return View(projectList);
            }
        }
        //Redirect and changes your admin status when passed ProjectID
        [HttpPost]
        public ActionResult Redirect(FormCollection collection)
        {
            if (Session["ID"].ToString() != "Guest")
            {
                MongoHelper.ConnectToMongo();
                MongoHelper.users_collection =
                    MongoHelper.database.GetCollection<User>("Users");
                var filter = Builders<User>.Filter.Eq("_id", Session["ID"].ToString());
                var result = MongoHelper.users_collection.Find(filter).FirstOrDefault();
                if (result != null)
                {
                    for (int i = 0; i < result.AdminStatus.Count; i++)
                    {
                        if (result.AdminStatus[i].ProjectID == collection["ProjectID"])
                        {
                            Session["AdminStatus"] = result.AdminStatus[i].Status;
                        }
                    }
                }
                return RedirectToAction(collection["Address"].ToString(), "User", new { id = collection["ProjectID"] });
            }
            return RedirectToAction(collection["Address"].ToString(), "User", new { id = collection["ProjectID"] });
        }
        //Redirect and changes your admin status when passed TicketID
        [HttpPost]
        public ActionResult Redirect2(FormCollection collection)
        {
            if (Session["ID"].ToString() != "Guest")
            {
                MongoHelper.ConnectToMongo();
                MongoHelper.users_collection =
                    MongoHelper.database.GetCollection<User>("Users");
                var filter = Builders<User>.Filter.Eq("_id", Session["ID"].ToString());
                var result = MongoHelper.users_collection.Find(filter).FirstOrDefault();
                if (result != null)
                {
                    for (int i = 0; i < result.AdminStatus.Count; i++)
                    {
                        if (result.AdminStatus[i].ProjectID == collection["ProjectID"])
                        {
                            Session["AdminStatus"] = result.AdminStatus[i].Status;
                        }
                    }
                }
                return RedirectToAction(collection["Address"].ToString(), "User", new { id = collection["TicketID"] });
            }
            return RedirectToAction(collection["Address"].ToString(), "User", new { id = collection["TicketID"] });
        }
        [MyRoleAuthorization("Manager", "Admin")]
        // GET: UserInvite
        public ActionResult UserInvite(string id)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            List<Project> projectList = new List<Project>();

            var userFilter = Builders<User>.Filter.Ne("_id", Session["ID"]);//Grabs a List of all users except User
            var userResult = MongoHelper.users_collection.Find(userFilter).ToList();

            var projectFilter = Builders<Project>.Filter.Eq("_id", id);
            var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
            projectList.Add(projectResult);

            var tupleModel = new Tuple<List<Project>, List<User>>(projectList, userResult);
            return View(tupleModel);
        }


        // POST: UserInvite from UserInvite Page
        //Sends a message to a user to invite him/her to join a project
        [HttpPost]
        public ActionResult UserInvite(FormCollection collection)
        {
            //https://www.tutorialsteacher.com/mvc/htmlhelper-hidden-hiddenfor
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            var filter = Builders<Project>.Filter.Eq("_id", collection["ProjectID"]);
            var result = MongoHelper.project_collection.Find(filter).FirstOrDefault();
            var projectname = result.ProjectName;

            MongoHelper.message_collection =
                MongoHelper.database.GetCollection<Message>("Message");

            var recipientFilter = Builders<Message>.Filter.Eq("Recipient", collection["item.Email"]);
            var recipientResult = MongoHelper.message_collection.Find(recipientFilter).FirstOrDefault();

            for (int i = 0; i < result.UserID.Length; i++)
            {
                if (result.UserID[i].ToString() == collection["item._id"].ToString())//Checks if User in Project with selected User
                {
                    return RedirectToAction("UserInvite", "User", new { message = "InProject" });
                }
            }

            if (recipientResult == null) //If there's no message sent to the user with the same email
            {
                MongoHelper.message_collection.InsertOneAsync(new Message
                {
                    _id = GenerateRandomId(24),
                    Recipient = collection["item.Email"],
                    Sender = Session["Username"].ToString(),
                    Subject = "Invite to a Project",
                    Content = "You have been invited to join " + projectname + " as a/an " + collection["Status"],
                    ProjectID_Invite = collection["ProjectID"].ToString(),
                    Project_Status = collection["Status"].ToString()
                }); ;
                return RedirectToAction("UserInvite", "User", new { message = "success" });
            }
            else if (recipientResult.ProjectID_Invite != collection["ProjectID"].ToString()) //if there's no message with the projectInvite
            {
                MongoHelper.message_collection.InsertOneAsync(new Message
                {
                    _id = GenerateRandomId(24),
                    Recipient = collection["item.Email"],
                    Sender = Session["Username"].ToString(),
                    Subject = "Invite to a Project",
                    Content = "You have been invited to join " + projectname,
                    ProjectID_Invite = collection["ProjectID"].ToString() + " as a/an " + collection["Status"],
                    Project_Status = collection["Status"].ToString()
                });
                return RedirectToAction("UserInvite", "User", new { message = "success" });
            }

            return RedirectToAction("UserInvite", "User", new { message = "failure" });
        }
        [MyRoleAuthorization("Manager", "Admin")]
        //GET:RemoveUser
        public ActionResult RemoveUser(string id)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            List<Project> projectList = new List<Project>();
            List<User> userList = new List<User>();

            var projectFilter = Builders<Project>.Filter.Eq("_id", id);
            var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
            if (projectResult != null)
            {
                projectList.Add(projectResult);
                for (int i = 0; i < projectResult.UserID.Length; i++)//Loop over the List of Project's List of Users
                {
                    var userFilter = Builders<User>.Filter.Eq("_id", projectResult.UserID[i]);
                    var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
                    if (userResult != null && userResult._id.ToString() != projectResult.ProjectManagerID)//UserResult Not Null and Not Project Manager
                    {
                        userList.Add(userResult);
                    }
                }
            }
            var tupleModel = new Tuple<List<Project>, List<User>>(projectList, userList);
            return View(tupleModel);
        }

        //POST: RemoveUser from RemoveUser Page
        //Removes Users from a Project
        [HttpPost]
        public ActionResult RemoveUser(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            //Removes AdminStatus from User related to the project------------------------------------------------------------------------------
            var userFilter = Builders<User>.Filter.Eq("_id", collection["UserID"]);
            var userUpdate = Builders<User>.Update.PullFilter("AdminStatus", Builders<Privilege>.Filter.Eq("_id", collection["AdminStatusID"]));
            MongoHelper.users_collection.FindOneAndUpdateAsync(userFilter, userUpdate);
            //------------------------------------------------------------------------------Removes AdminStatus from User related to the project

            //Removes User from the Project's List-----------------------------------------------------------------
            var projectFilter = Builders<Project>.Filter.Eq("_id", collection["ProjectID"]);
            var projectUpdate = Builders<Project>.Update.Pull("UserID", collection["UserID"]);
            MongoHelper.project_collection.FindOneAndUpdate(projectFilter, projectUpdate);
            //-----------------------------------------------------------------Removes User from the Project's List
            var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
            if (projectResult != null)
            {
                for (int i = 0; i < projectResult.TicketID.Length; i++)
                {
                    //Removes User from the Project's Tickets---------------------------------------------
                    var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult.TicketID[i]);
                    var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
                    if (ticketResult != null)
                    {
                        if (ticketResult.details.devID == collection["UserID"])
                        {
                            var ticketUpdate = Builders<Ticket>.Update
                                .Set("details.assignDeveloper", "None")
                                .Set("details.devID", "None");
                            MongoHelper.ticket_collection.FindOneAndUpdate(ticketFilter, ticketUpdate);
                        }
                    }
                    //---------------------------------------------Removes User from the Project's Tickets
                }
            }
            return RedirectToAction("RemoveUser", "User", new { message = "success" });
        }

        //Admin Pages End ------------------------------------------------------------------------------------------------------------------------->

        //Project Pages Start-------------------------------------------------------------------------------------------------------------------------->
        [MyRoleAuthorization("Manager", "Admin", "Developer", "Submitter", "IsUser")]
        // GET: Project
        public ActionResult Project()
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            if (Session["ID"].ToString() == "Guest")
            {
                List<User> userList = new List<User>();
                var filter = Builders<Project>.Filter.Eq("ProjectManagerID", Session["ID"]);
                var result = MongoHelper.project_collection.Find(filter).ToList();
                var tupleModel = new Tuple<List<User>, List<Project>>(userList, result);
                return View(tupleModel);
            }
            else //If Manager, Admin, Developer, Submitter, IsUser (User)
            {
                List<Project> projectList = new List<Project>();
                List<User> userList = new List<User>();
                var filter = Builders<User>.Filter.Eq("_id", Session["ID"]);
                var result = MongoHelper.users_collection.Find(filter).FirstOrDefault();
                if (result != null)
                {
                    for (int i = 0; i < result.AdminStatus.Count; i++)//Admin Status contains status and projectID that the user is in
                    {
                        var projectFilter = Builders<Project>.Filter.Eq("_id", result.AdminStatus[i].ProjectID);//Compares User's ProjectID with Project's _id
                        var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
                        if (projectResult != null)
                        {
                            projectList.Add(projectResult);
                        }
                    }
                    userList.Add(result);
                }
                var tupleModel = new Tuple<List<User>, List<Project>>(userList, projectList);
                return View(tupleModel);
            }
        }

        // POST: CreateProject from Project page
        //Creates a New Project
        [HttpPost]
        public ActionResult CreateProject(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            //Create Id
            Object id = GenerateRandomId(24);
            var ProjectManagerName = "Guest";
            if (Session["ID"].ToString() != "Guest")//If not Guest, assigns a Manager Role to the User who created the project
            {
                MongoHelper.users_collection =
                    MongoHelper.database.GetCollection<User>("Users");
                var filter = Builders<User>.Filter.Eq("_id", Session["ID"]);
                Privilege privilege = new Privilege()
                {
                    _id = GenerateRandomId(24),
                    Status = "Manager",
                    ProjectID = id.ToString()
                };
                var update = Builders<User>.Update.Push("AdminStatus", privilege);
                MongoHelper.users_collection.UpdateOneAsync(filter, update);
                ProjectManagerName = Session["UserName"].ToString();
            }
            //Creates the Project Object for DB------------------------------------
            MongoHelper.project_collection.InsertOne(new Project
            {
                _id = id,
                ProjectName = collection["Name"],
                Description = collection["Description"],
                ProjectManagerID = Session["ID"].ToString(),
                ProjectManagerName = ProjectManagerName,
                UserID = new string[] { },
                TicketID = new string[] { }
            });
            //------------------------------------Creates the Project Object for DB

            if (Session["ID"].ToString() != "Guest")//If not Guest, assigns a Manager Role to the User who created the project
            {
                //Pushes Manager's ID in UserID List--------------------------------
                var projectFilter = Builders<Project>.Filter.Eq("_id", id);
                var projectUpdate = Builders<Project>.Update
                    .Push("UserID", Session["ID"]);
                MongoHelper.project_collection.UpdateOne(projectFilter, projectUpdate);
                //--------------------------------Pushes Manager's ID in UserID List
            }
            return RedirectToAction("Project", "User", new { message = "Success" });
        }

        //POST: DeleteProject from Project Page
        //Deletes Projects and all it's Information including Users and Tickets, It also deletes the User's info about the project
        [HttpPost]
        public ActionResult DeleteProject(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            MongoHelper.picture_collection =
                MongoHelper.database.GetCollection<Picture>("Picture");

            var projectFilter = Builders<Project>.Filter.Eq("_id", collection["ProjectID"]);
            var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();

            if (projectResult != null)
            {
                //Deletes AdminStatus from all Users----------------------------------------------------------------------------------------------------------
                for (int i = 0; i < projectResult.UserID.Length; i++)
                {
                    var userFilter = Builders<User>.Filter.Eq("_id", projectResult.UserID[i]);
                    var userUpdate = Builders<User>.Update.PullFilter("AdminStatus", Builders<Privilege>.Filter.Eq("ProjectID", collection["ProjectID"]));
                    MongoHelper.users_collection.FindOneAndUpdate(userFilter, userUpdate);
                }
                //----------------------------------------------------------------------------------------------------------Deletes AdminStatus from all Users

                //Deletes Ticket Associated with Project                
                for (int i = 0; i < projectResult.TicketID.Length; i++)
                {
                    var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult.TicketID[i]);
                    var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
                    if (ticketResult != null)
                    {
                        for (int j = 0; j < ticketResult.attachments.Count; j++)//Loops over all the attachments in Ticket and delete them from Picture Collection
                        {
                            //Finding the attachment in Ticket---------------------------------------------------------------------------
                            var attachmentSubfilter = Builders<Attachments>.Filter.Eq("_id", ticketResult.attachments[j]._id);
                            var ticketattachmentFilter = Builders<Ticket>.Filter.ElemMatch("attachments", attachmentSubfilter);
                            var ticketattachmentResult = MongoHelper.ticket_collection.Find(ticketattachmentFilter)
                                .Project(Builders<Ticket>.Projection.Exclude("_id")
                                .Include("attachments.$")).FirstOrDefault();
                            //----------------------------------------------------------------------------Finding the attachment in Ticket
                            var allFields = ticketattachmentResult["attachments"].AsBsonArray;
                            foreach (var fields in allFields)
                            {
                                //Delete Attachment in Picture
                                var pictureFilter = Builders<Picture>.Filter.Eq("_id", fields["pictureID"].AsString);
                                MongoHelper.picture_collection.DeleteOneAsync(pictureFilter);
                            }
                            //Removes Attachment from Ticket------------------------------------------------------------------------------------------------------------
                            var update = Builders<Ticket>.Update.PullFilter("attachments", Builders<Attachments>.Filter.Eq("_id", ticketResult.attachments[j]._id));
                            MongoHelper.ticket_collection.FindOneAndUpdateAsync(ticketFilter, update);
                            //------------------------------------------------------------------------------------------------------------Removes Attachment from Ticket
                        }
                    }
                    MongoHelper.ticket_collection.DeleteOneAsync(ticketFilter);
                }
                //Deletes Ticket Associated with Project
                MongoHelper.project_collection.DeleteOne(projectFilter);//Deletes Project
            }
            return RedirectToAction("Project", "User", new { message = "Delete" });
        }

        //POST: QuitProject from Project
        //Deletes user from Project, and the project's info from the user
        [HttpPost]
        public ActionResult QuitProject(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            //Uses the ProjectID to identify the AdminStatus and removes it from the User------------------------------------------------------------
            var userFilter = Builders<User>.Filter.Eq("_id", Session["ID"]);
            var userUpdate = Builders<User>.Update.PullFilter("AdminStatus", Builders<Privilege>.Filter.Eq("ProjectID", collection["ProjectID"]));
            MongoHelper.users_collection.FindOneAndUpdate(userFilter, userUpdate);
            //------------------------------------------------------------Uses the ProjectID to identify the AdminStatus and removes it from the User

            //Removes the User from the project's list of users--------------------------------------------------------------------------------------
            var projectFilter = Builders<Project>.Filter.Eq("_id", collection["ProjectID"]);
            var projectUpdate = Builders<Project>.Update.Pull("UserID", Session["ID"]);
            MongoHelper.project_collection.FindOneAndUpdateAsync(projectFilter, projectUpdate);
            //--------------------------------------------------------------------------------------Removes the User from the project's list of users
            var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
            if (projectResult != null)
            {
                for (int i = 0; i < projectResult.TicketID.Count(); i++)//Loops through all the Project's Ticket
                {
                    var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult.TicketID[i]);
                    var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
                    if (ticketResult != null)
                    {
                        if (ticketResult.details.assignDeveloper.ToString() == Session["Username"].ToString())//Removes Assigned User from tickets
                        {
                            var ticketUpdate = Builders<Ticket>.Update
                                .Set("details.assignDeveloper", "None")
                                .Set("details.devID", "None");
                            MongoHelper.ticket_collection.UpdateOneAsync(ticketFilter, ticketUpdate);
                        }
                    }
                    else
                    {
                        return RedirectToAction("Project", "User", new { id = collection["ProjectID"].ToString(), message = "Error" });
                    }
                }
            }
            else
            {
                return RedirectToAction("Project", "User", new { id = collection["ProjectID"].ToString(), message = "Error" });
            }

            return RedirectToAction("Project", "User", new { id = collection["ProjectID"].ToString(), message = "UserQuit" });
        }

        // GET: ProjectDetails
        public ActionResult ProjectDetails(string id)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            List<User> userList = new List<User>();
            List<Ticket> ticketList = new List<Ticket>();
            List<Project> projectList = new List<Project>();
            var projectFilter = Builders<Project>.Filter.Eq("_id", id);//Query Project from id passed from Project
            var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
            if (projectResult != null)
            {
                projectList.Add(projectResult);
                for (int i = 0; i < projectResult.TicketID.Length; i++)//Query Ticket from Project's List of Ticket IDS
                {
                    var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult.TicketID[i]);
                    var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
                    if (ticketResult != null)
                    {
                        ticketList.Add(ticketResult);
                    }
                }
                for (int i = 0; i < projectResult.UserID.Length; i++)//Query Project from Project's List of Users IDS
                {
                    var userFilter = Builders<User>.Filter.Eq("_id", projectResult.UserID[i]);
                    var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
                    if (userResult != null)
                    {
                        userList.Add(userResult);
                    }
                }

            }
            var tupleModel = new Tuple<List<User>, List<Ticket>, List<Project>>(userList, ticketList, projectList);
            return View(tupleModel);
        }
        [MyRoleAuthorization("Admin", "Manager")]
        //GET: EditProject
        public ActionResult EditProject(string id)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            List<User> userList = new List<User>();
            List<Ticket> ticketList = new List<Ticket>();
            List<Project> projectList = new List<Project>();
            var projectFilter = Builders<Project>.Filter.Eq("_id", id);//Query Project from id passed from Project
            var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
            if (projectResult != null)
            {
                projectList.Add(projectResult);
                for (int i = 0; i < projectResult.TicketID.Length; i++)//Query Ticket from Project's List of Ticket IDS and add them to a List
                {
                    var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult.TicketID[i]);
                    var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
                    if (ticketResult != null)
                    {
                        ticketList.Add(ticketResult);
                    }
                }
                for (int i = 0; i < projectResult.UserID.Length; i++)//Query Project from Project's List of Users IDS and add them to a List
                {
                    var userFilter = Builders<User>.Filter.Eq("_id", projectResult.UserID[i]);
                    var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
                    if (userResult != null && userResult._id.ToString() != projectResult.ProjectManagerID)
                    {
                        userList.Add(userResult);
                    }
                }
            }
            var tupleModel = new Tuple<List<User>, List<Ticket>, List<Project>>(userList, ticketList, projectList);
            return View(tupleModel);
        }

        //Posts for DeleteTicket from Edit Project
        //Deletes Ticket
        [HttpPost]
        public ActionResult DeleteTicketEditProject(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.picture_collection =
                MongoHelper.database.GetCollection<Picture>("Picture");

            var ticketFilter = Builders<Ticket>.Filter.Eq("_id", collection["ticketID"]);
            var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
            if (ticketResult != null)
            {
                for (int j = 0; j < ticketResult.attachments.Count; j++)//Loops over all the attachments in Ticket and delete them from Picture Collection
                {
                    //Finding the attachment in Ticket---------------------------------------------------------------------------
                    var attachmentSubfilter = Builders<Attachments>.Filter.Eq("_id", ticketResult.attachments[j]._id);
                    var ticketattachmentFilter = Builders<Ticket>.Filter.ElemMatch("attachments", attachmentSubfilter);
                    var ticketattachmentResult = MongoHelper.ticket_collection.Find(ticketattachmentFilter)
                        .Project(Builders<Ticket>.Projection.Exclude("_id")
                        .Include("attachments.$")).FirstOrDefault();
                    //----------------------------------------------------------------------------Finding the attachment in Ticket
                    var allFields = ticketattachmentResult["attachments"].AsBsonArray;
                    foreach (var fields in allFields)
                    {
                        //Delete Attachment in Picture
                        var pictureFilter = Builders<Picture>.Filter.Eq("_id", fields["pictureID"].AsString);
                        MongoHelper.picture_collection.DeleteOneAsync(pictureFilter);
                    }
                    //Removes Attachment from Ticket------------------------------------------------------------------------------------------------------------
                    var ticketUpdate = Builders<Ticket>.Update.PullFilter("attachments", Builders<Attachments>.Filter.Eq("_id", ticketResult.attachments[j]._id));
                    MongoHelper.ticket_collection.FindOneAndUpdateAsync(ticketFilter, ticketUpdate);
                    //------------------------------------------------------------------------------------------------------------Removes Attachment from Ticket
                }
                MongoHelper.ticket_collection.DeleteOneAsync(ticketFilter);
            }
            else
            {
                return RedirectToAction("Tickets", "User", new { message = "Error" });
            }

            var projectFilter = Builders<Project>.Filter.Eq("_id", collection["projectID"]);
            var update = Builders<Project>.Update.Pull("TicketID", collection["ticketID"]);//Delete Ticket ID from Project's List
            MongoHelper.project_collection.FindOneAndUpdate(projectFilter, update);

            return RedirectToAction("EditProject", "User", new { id = collection["projectID"], message = "TicketDeleted" });
        }
        //POST: EditProjectDetails from EditProject
        //Updates Project Information, Name and Description
        [HttpPost]
        public ActionResult EditProjectDetails(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            var filter = Builders<Project>.Filter.Eq("_id", collection["projectid"]);
            var update = Builders<Project>.Update
                .Set("ProjectName", collection["item.ProjectName"])
                .Set("Description", collection["item.Description"]);
            MongoHelper.project_collection.UpdateOne(filter, update);
            return RedirectToAction("EditProject", "User", new { id = collection["projectid"], message = "DetailEdited" });
        }

        //POST: EditProjectDeleteUser from EditProject page
        //Deletes User from the EditProject Page
        [HttpPost]
        public ActionResult EditProjectDeleteUser(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            //Removes the user's project privileges-----------------------------------------------------------------------------------------------------
            var userFilter = Builders<User>.Filter.Eq("_id", collection["user._id"]);
            var userUpdate = Builders<User>.Update.PullFilter("AdminStatus", Builders<Privilege>.Filter.Eq("_id", collection["AdminStatusID"]));
            MongoHelper.users_collection.FindOneAndUpdateAsync(userFilter, userUpdate);
            //-----------------------------------------------------------------------------------------------------Removes the user's project privileges
            //Removes User ID from Project's List of User ID---------------------------------------------------------------------------------------------
            var projectFilter = Builders<Project>.Filter.Eq("_id", collection["project._id"]);
            var projectUpdate = Builders<Project>.Update.Pull("UserID", collection["user._id"]);
            MongoHelper.project_collection.FindOneAndUpdate(projectFilter, projectUpdate);
            //---------------------------------------------------------------------------------------------------- User ID from Project's List of User ID
            var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
            if (projectResult != null)
            {
                for (int i = 0; i < projectResult.TicketID.Length; i++)
                {
                    var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult.TicketID[i]);
                    var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
                    if (ticketResult != null)
                    {
                        if (ticketResult.details.devID == collection["user._id"])//Removes User's assignment on Tickets
                        {
                            var ticketUpdate = Builders<Ticket>.Update
                                .Set("details.assignDeveloper", "None")
                                .Set("details.devID", "None");
                            MongoHelper.ticket_collection.FindOneAndUpdate(ticketFilter, ticketUpdate);
                        }
                    }
                }
            }
            return RedirectToAction("EditProject", "User", new { id = collection["project._id"].ToString(), message = "UserDeleted" });
        }

        //POST: TrasferOwnership from EditProject
        //Assigns a new User to be a Project's Manager and Changes original project manager to a developer
        [HttpPost]
        public ActionResult TransferOwnership(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            //Uses Session ID to Find as only Non-Guest and Managers can access this function
            var PMFilter = Builders<User>.Filter.Eq("_id", Session["ID"]);
            var PMResult = MongoHelper.users_collection.Find(PMFilter).FirstOrDefault();
            if (PMResult != null)
            {
                for (int i = 0; i < PMResult.AdminStatus.Count; i++)
                {
                    if (PMResult.AdminStatus[i].ProjectID == collection["ProjectID"])
                    {
                        var subUserFilter = Builders<Privilege>.Filter.Eq("_id", PMResult.AdminStatus[i]._id);
                        var userFilter = Builders<User>.Filter.ElemMatch("AdminStatus", subUserFilter);
                        var userUpdate = Builders<User>.Update.Set("AdminStatus.$.Status", "Developer");
                        MongoHelper.users_collection.UpdateOneAsync(userFilter, userUpdate); //Manager will be a developer after transfering project rights
                        break;
                    }
                }
            }
            else
            {
                return RedirectToAction("Project", "User", new { id = collection["UserID"].ToString(), message = "Error" });
            }
            var userfilter = Builders<User>.Filter.Eq("_id", collection["UserID"]);
            var userResult = MongoHelper.users_collection.Find(userfilter).FirstOrDefault();
            if (userResult != null)
            {
                for (int i = 0; i < userResult.AdminStatus.Count; i++)
                {
                    //Changes selected User to Manager in the User's Array-------------------------------------------------------------------------
                    if (userResult.AdminStatus[i].ProjectID == collection["ProjectID"])
                    {
                        var subUserFilter = Builders<Privilege>.Filter.Eq("_id", userResult.AdminStatus[i]._id);
                        var userFilter = Builders<User>.Filter.ElemMatch("AdminStatus", subUserFilter);
                        var userUpdate = Builders<User>.Update.Set("AdminStatus.$.Status", "Manager");
                        MongoHelper.users_collection.UpdateOneAsync(userFilter, userUpdate);
                        break;
                    }
                    //-------------------------------------------------------------------------Changes selected User to Manager in the User's Array
                }
                //Changes Project Manager Designation on Project-----------------------------------------------------------------------------------
                var projectFilter = Builders<Project>.Filter.Eq("_id", collection["ProjectID"]);
                var projectUpdate = Builders<Project>.Update
                    .Set("ProjectManagerID", userResult._id)
                    .Set("ProjectManagerName", userResult.Username);
                MongoHelper.project_collection.UpdateOneAsync(projectFilter, projectUpdate);
                //-----------------------------------------------------------------------------------Changes Project Manager Designation on Project
                var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
                if (projectResult != null)
                {
                    //Changes Project Manager Designation on Ticket----------------------------------------------------------------------------------
                    for (int i = 0; i < projectResult.TicketID.Length; i++)
                    {
                        var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult.TicketID[i]);
                        var ticketUpdate = Builders<Ticket>.Update
                            .Set("details.ProjectManagerID", userResult._id);
                        MongoHelper.ticket_collection.UpdateOneAsync(ticketFilter, ticketUpdate);
                    }
                    //----------------------------------------------------------------------------------Changes Project Manager Designation on Ticket
                }
                else
                {
                    return RedirectToAction("Project", "User", new { id = collection["UserID"].ToString(), message = "Error" });
                }
            }
            else
            {
                return RedirectToAction("Project", "User", new { id = collection["UserID"].ToString(), message = "Error" });
            }
            return RedirectToAction("Project", "User", new { id = collection["UserID"].ToString(), message = "ProjectManagerChanged" });
        }
        //Project Pages End----------------------------------------------------------------------------------------------------------------------------->
        //Ticket Pages Start---------------------------------------------------------------------------------------------------------------------------->
        //Get: Ticket
        public ActionResult Tickets()
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                    MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            List<Project> projectList = new List<Project>();
            List<Ticket> ticketList = new List<Ticket>();
            if (Session["ID"].ToString() == "Guest")//If User is a Guest
            {
                var projectFilter = Builders<Project>.Filter.Eq("ProjectManagerID", Session["ID"]);
                var projectResult = MongoHelper.project_collection.Find(projectFilter).ToList();
                if (projectResult != null)
                {
                    for (int i = 0; i < projectResult.Count(); i++)
                    {
                        for (int j = 0; j < projectResult[i].TicketID.Length; j++)
                        {
                            var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult[i].TicketID[j]);
                            var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
                            if (ticketResult != null)
                            {
                                ticketList.Add(ticketResult);
                            }
                        }
                    }
                }
                var tupleModel = new Tuple<List<Ticket>, List<Project>>(ticketList, projectResult);
                return View(tupleModel);
            }
            else
            {
                var userFilter = Builders<User>.Filter.Eq("_id", Session["ID"]);
                var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
                if (userResult != null)
                {
                    for (int i = 0; i < userResult.AdminStatus.Count; i++)
                    {
                        var projectFilter = Builders<Project>.Filter.Eq("_id", userResult.AdminStatus[i].ProjectID);
                        var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
                        if (projectResult != null)
                        {
                            projectList.Add(projectResult);
                            for (int j = 0; j < projectResult.TicketID.Length; j++)
                            {
                                var ticketFilter = Builders<Ticket>.Filter.Eq("_id", projectResult.TicketID[j]);
                                var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
                                if (ticketResult != null)
                                {
                                    ticketList.Add(ticketResult);
                                }
                            }
                        }
                    }
                }
                var tupleModel = new Tuple<List<Ticket>, List<Project>>(ticketList, projectList);
                return View(tupleModel);
            }
        }
        //Get: Ticket Management
        [MyRoleAuthorization("Manager", "Admin", "Developer", "Submitter", "IsUser")]
        public ActionResult CreateTicket(string id)
        {
            //Tuple Two Models
            //https://www.c-sharpcorner.com/UploadFile/ff2f08/multiple-models-in-single-view-in-mvc/
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            var filter = Builders<Project>.Filter.Eq("_id", id);
            var result = MongoHelper.project_collection.Find(filter).FirstOrDefault();
            List<Project> projectList = new List<Project>();
            List<User> userList = new List<User>();
            if (result != null)
            {
                projectList.Add(result);
                for (int i = 0; i < result.UserID.Length; i++)
                {
                    var userfilter = Builders<User>.Filter.Eq("_id", result.UserID[i]);//Query Users only associated with Project
                    var userresult = MongoHelper.users_collection.Find(userfilter).FirstOrDefault();
                    if (userresult != null && userresult._id.ToString() != result.ProjectManagerID)
                    {
                        userList.Add(userresult);
                    }
                }
                var tupleModel = new Tuple<List<User>, List<Project>>(userList, projectList);
                return View(tupleModel);
            }
            else
            {
                var tupleModel = new Tuple<List<User>, List<Project>>(userList, projectList);
                return View(tupleModel);
            }
        }

        //POST: CreateTicket
        //Creates a Ticket for a project
        [HttpPost]
        public ActionResult CreateTicket(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            Object id = GenerateRandomId(24);
            var assignedDev = "";
            if (collection["assignedDeveloper"] == "None")
            {
                assignedDev = "None";
            }
            else
            {
                var userFilter = Builders<User>.Filter.Eq("_id", collection["assignedDeveloper"]);
                var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
                assignedDev = userResult.Username;
            }

            var filter = Builders<Project>.Filter.Eq("_id", collection["project._id"]);
            var result = MongoHelper.project_collection.Find(filter).FirstOrDefault();
            var update = Builders<Project>.Update
                .Push("TicketID", id);//Add TicketID to Project so it can be queried later in Project
            MongoHelper.project_collection.UpdateOneAsync(filter, update);

            //Create Ticket details as Comments,Histories, and Attachments can be created in Ticket Details
            MongoHelper.ticket_collection.InsertOne(new Ticket
            {
                _id = id,
                details = new Details
                {
                    ticketTitle = collection["ticketTitle"],
                    ticketDescription = collection["ticketDescription"],
                    assignDeveloper = assignedDev,
                    devID = collection["assignedDeveloper"],
                    submitter = Session["Username"].ToString(),
                    submitterID = Session["ID"].ToString(),
                    projectName = collection["project.ProjectName"],
                    project_id = collection["project._id"],
                    ProjectManagerID = result.ProjectManagerID,
                    ticketPriority = collection["ticketPriority"],
                    ticketStatus = collection["ticketStatus"],
                    ticketType = collection["ticketType"],
                    created = String.Format("{0:F}", DateTime.Now),
                    updated = String.Format("{0:F}", DateTime.Now)
                },
                comments = new List<Comments>(),
                histories = new List<Histories>(),
                attachments = new List<Attachments>()

            });

            return RedirectToAction("CreateTicket", "User", new { message = "success" });

        }
        //Get:EditTicket
        [MyRoleAuthorization("Manager", "Admin", "Developer", "Submitter", "IsUser")]
        public ActionResult EditTicket(string id)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            List<Ticket> ticketList = new List<Ticket>();
            List<User> userList = new List<User>();
            List<Project> projectList = new List<Project>();
            var ticketFilter = Builders<Ticket>.Filter.Eq("_id", id);//Find Ticket in DB with passed ticket id
            var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
            if (ticketResult != null)
            {
                ticketList.Add(ticketResult);
                var projectFilter = Builders<Project>.Filter.Eq("_id", ticketResult.details.project_id);//Find Project in DB with Ticket.Details.Project_id from TicketResult
                var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
                if (projectResult != null)
                {
                    projectList.Add(projectResult);
                    for (int i = 0; i < projectResult.UserID.Length; i++)
                    {
                        var userFilter = Builders<User>.Filter.Eq("_id", projectResult.UserID[i]);//Query Users only associated with Project
                        var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
                        if (userResult != null && userResult._id.ToString() != projectResult.ProjectManagerID)//Not Project Manager
                        {
                            userList.Add(userResult);
                        }
                    }
                }
            }
            var tupleModel = new Tuple<List<Ticket>, List<User>, List<Project>>(ticketList, userList, projectList);
            return View(tupleModel);
        }
        //Post: Edit Ticket
        //Updates Ticket Information
        [HttpPost]
        public ActionResult EditTicket(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            var assignedDev = "None";
            if (collection["assignedDeveloper"] != "None")
            {
                var userFilter = Builders<User>.Filter.Eq("_id", collection["assignedDeveloper"]);//Looks up username with ID
                var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
                assignedDev = userResult.Username;
            }

            var ticketFilter = Builders<Ticket>.Filter.Eq("_id", collection["TicketID"]);
            //Store Old Ticket Detail for History
            var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
            Details oldValue = ticketResult.details;
            //Store Old Ticket Detail for History

            //Updates Ticket
            var update = Builders<Ticket>.Update
                .Set("details.ticketTitle", collection["item.details.ticketTitle"])
                .Set("details.ticketDescription", collection["item.details.ticketDescription"])
                .Set("details.assignDeveloper", assignedDev)
                .Set("details.devID", collection["assignedDeveloper"])
                .Set("details.ticketPriority", collection["ticketPriority"])
                .Set("details.ticketStatus", collection["ticketStatus"])
                .Set("details.ticketType", collection["item.details.ticketType"])
                .Set("details.updated", String.Format("{0:F}", DateTime.Now));
            MongoHelper.ticket_collection.UpdateOneAsync(ticketFilter, update);
            //Updates Ticket

            //Store Updated Ticket Detail for History
            ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
            Details newValue = ticketResult.details;
            //Store Updated Ticket Detail for History

            //Create a History Object and Push it to the History List
            Histories histories = new Histories()
            {
                _id = GenerateRandomId(24),
                Editor = Session["Username"].ToString(),
                oldValue = oldValue,
                newValue = newValue,
                dateChanged = String.Format("{0:F}", DateTime.Now)
            };
            //Create a History Object and Push it to the History List
            var update2 = Builders<Ticket>.Update
                    .Push("histories", histories);
            MongoHelper.ticket_collection.UpdateOneAsync(ticketFilter, update2);
            if (collection["ticketStatus"] == "Closed")
            {
                return RedirectToAction("EditTicket", "User", new { id = collection["TicketID"], message = "TicketClosed" });
            }
            else 
            {
                return RedirectToAction("EditTicket", "User", new { id = collection["TicketID"], message = "TicketDetails" });
            }
        }


        //Post: Reopen from EditTicket
        //Reopens closed tickets
        [HttpPost]
        public ActionResult Reopen(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            var ticketFilter = Builders<Ticket>.Filter.Eq("_id", collection["TicketID"]);
            var update = Builders<Ticket>.Update
                .Set("details.ticketStatus", collection["ticketStatus"]);
            MongoHelper.ticket_collection.UpdateOne(ticketFilter, update);
            return RedirectToAction("EditTicket", "User", new { id = collection["TicketID"], message = "TicketReopen" });
        }


        //Get: Ticket Details
        public ActionResult TicketDetails(string id)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            var filter = Builders<Ticket>.Filter.Eq("_id", id);
            var result = MongoHelper.ticket_collection.Find(filter).FirstOrDefault();
            List<Ticket> ticketList = new List<Ticket>();
            if (result != null)
            {
                ticketList.Add(result);
                return View(ticketList);
            }
            else
            {
                return View(ticketList);
            }
        }

        // POST: DeleteTicket from EditTicket
        //Deletes Ticket
        [HttpPost]
        public ActionResult DeleteTicketTicketPage(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            MongoHelper.picture_collection =
                MongoHelper.database.GetCollection<Picture>("Picture");
            var ticketFilter = Builders<Ticket>.Filter.Eq("_id", collection["TicketID"]);
            var ticketResult = MongoHelper.ticket_collection.Find(ticketFilter).FirstOrDefault();
            if (ticketResult != null)
            {
                for (int j = 0; j < ticketResult.attachments.Count; j++)//Loops over all the attachments in Ticket and delete them from Picture Collection
                {
                    //Finding the attachment in Ticket---------------------------------------------------------------------------
                    var attachmentSubfilter = Builders<Attachments>.Filter.Eq("_id", ticketResult.attachments[j]._id);
                    var ticketattachmentFilter = Builders<Ticket>.Filter.ElemMatch("attachments", attachmentSubfilter);
                    var ticketattachmentResult = MongoHelper.ticket_collection.Find(ticketattachmentFilter)
                        .Project(Builders<Ticket>.Projection.Exclude("_id")
                        .Include("attachments.$")).FirstOrDefault();
                    //----------------------------------------------------------------------------Finding the attachment in Ticket
                    var allFields = ticketattachmentResult["attachments"].AsBsonArray;
                    foreach (var fields in allFields)
                    {
                        //Delete Attachment in Picture
                        var pictureFilter = Builders<Picture>.Filter.Eq("_id", fields["pictureID"].AsString);
                        MongoHelper.picture_collection.DeleteOneAsync(pictureFilter);
                    }
                    //Removes Attachment from Ticket------------------------------------------------------------------------------------------------------------
                    var ticketUpdate = Builders<Ticket>.Update.PullFilter("attachments", Builders<Attachments>.Filter.Eq("_id", ticketResult.attachments[j]._id));
                    MongoHelper.ticket_collection.FindOneAndUpdateAsync(ticketFilter, ticketUpdate);
                    //------------------------------------------------------------------------------------------------------------Removes Attachment from Ticket
                }
                MongoHelper.ticket_collection.DeleteOneAsync(ticketFilter);
            }
            else 
            {
               return RedirectToAction("Tickets", "User", new { message = "Error" });
            }

            var projectFilter = Builders<Project>.Filter.Eq("_id", collection["ProjectID"]);
            var update = Builders<Project>.Update.Pull("TicketID", collection["TicketID"]);
            MongoHelper.project_collection.FindOneAndUpdate(projectFilter, update);//Deletes Ticket ID from Project
            return RedirectToAction("Tickets", "User", new { message = "TicketDeleted" });
        }

        // POST: AddComment from EditTicket
        //Creates a Comment to push it into Ticket
        [HttpPost]
        public ActionResult AddComment(FormCollection collection)
        {
            if (collection["comment"] != "")
            {
                MongoHelper.ConnectToMongo();
                MongoHelper.ticket_collection =
                    MongoHelper.database.GetCollection<Ticket>("Ticket");
                var filter = Builders<Ticket>.Filter.Eq("_id", collection["TicketID"]);
                Comments comments = new Comments()
                {
                    _id = GenerateRandomId(24),
                    commenter = Session["Username"].ToString(),
                    commenterID = Session["ID"].ToString(),
                    comment = collection["comment"],
                    created = String.Format("{0:F}", DateTime.Now)
                };
                var result = Builders<Ticket>.Update.Push("comments", comments);
                MongoHelper.ticket_collection.UpdateOneAsync(filter, result);
                return RedirectToAction("TicketDetails", "User", new { id = collection["TicketID"], message = "AddComment" });
            }
            else
            {
                return RedirectToAction("TicketDetails", "User", new { id = collection["TicketID"], message = "NoComment" });
            }
        }

        // POST: EditComment from EditTicket
        //Updates Ticket Information
        [HttpPost]
        public ActionResult EditComment(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            //Filter to get Subdocument (Ticket.Comment)
            var subFilter = Builders<Comments>.Filter.Eq("_id", collection["commentID"]);
            var filter = Builders<Ticket>.Filter.ElemMatch("comments", subFilter);

            var update = Builders<Ticket>.Update.Set("comments.$.comment", collection["commentText"]);
            MongoHelper.ticket_collection.UpdateOne(filter, update);

            return RedirectToAction("EditTicket", "User", new { id = collection["TicketID"], message = "EditComment" });

        }


        // POST: DeleteComment from EditTicket
        //Deletes Comments In Ticket
        [HttpPost]
        public ActionResult DeleteComment(FormCollection collection)
        {
            //https://stackoverflow.com/questions/30141958/mongodb-net-driver-2-0-pull-remove-element
            MongoHelper.ConnectToMongo();
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");

            var filter = Builders<Ticket>.Filter.Eq("_id", collection["TicketID"]);
            var update = Builders<Ticket>.Update.PullFilter("comments", Builders<Comments>.Filter.Eq("_id", collection["commentID"]));
            MongoHelper.ticket_collection.FindOneAndUpdateAsync(filter, update);

            return RedirectToAction("EditTicket", "User", new { id = collection["TicketID"], message = "DeleteComment" });

        }

        // POST: UploadAttachment from TicketDetails
        //Uploads Images to Ticket
        [HttpPost]
        public ActionResult UploadAttachment(HttpPostedFileBase theFile, FormCollection collection)
        {
            //https://www.codeproject.com/articles/708140/uploading-and-viewing-images-with-asp-net-mvc-and
            if (theFile != null)
            {
                //Get File Name
                string FileName = Path.GetFileName(theFile.FileName);

                //Get bytes from the stream of file
                byte[] PictureAsBytes = new byte[theFile.ContentLength];
                using (BinaryReader reader = new BinaryReader(theFile.InputStream))
                {
                    PictureAsBytes = reader.ReadBytes(theFile.ContentLength);
                }

                //convert bytes of image data to string using Base64 encoding
                string PictureDataAsString = Convert.ToBase64String(PictureAsBytes);

                MongoHelper.ConnectToMongo();
                MongoHelper.picture_collection =
                    MongoHelper.database.GetCollection<Picture>("Picture");
                Object PictureID = GenerateRandomId(24);
                //Insert Picture to picture collection
                MongoHelper.picture_collection.InsertOne(new Picture
                {
                    _id = PictureID,
                    fileName = FileName,
                    PictureDataAsString = PictureDataAsString
                });

                MongoHelper.ticket_collection =
                    MongoHelper.database.GetCollection<Ticket>("Ticket");
                var ticketFilter = Builders<Ticket>.Filter.Eq("_id", collection["TicketID"]);

                Attachments attachments = new Attachments()
                {
                    _id = GenerateRandomId(24),
                    pictureID = PictureID,//ID to identify in Picture Collection
                    uploader = Session["Username"].ToString(),
                    uploaderID = Session["ID"].ToString(),
                    notes = collection["comment"].ToString(),
                    created = String.Format("{0:F}", DateTime.Now)
                };
                var ticketResult = Builders<Ticket>.Update
                        .Push("attachments", attachments);
                MongoHelper.ticket_collection.UpdateOne(ticketFilter, ticketResult);
                return RedirectToAction("TicketDetails", "User", new { id = collection["TicketID"], message = "UploadAttachment" });
            }
            return RedirectToAction("TicketDetails", "User", new { id = collection["TicketID"], message = "noImage" });
        }

        // POST: EditAttachment from EditTicket
        //Updates Attachment with a replacement image
        [HttpPost]
        public ActionResult EditAttachment(HttpPostedFileBase theFile, FormCollection collection)
        {
            //https://stackoverflow.com/questions/38258139/filter-elemmatch
            MongoHelper.ConnectToMongo();
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            MongoHelper.picture_collection =
                    MongoHelper.database.GetCollection<Picture>("Picture");
            if (theFile != null)
            {
                //Delete Existing Picture----------------------------------------------------------------------------------------------------------------------------
                var subFilter = Builders<Attachments>.Filter.Eq("_id", collection["attachmentID"]);
                var filter = Builders<Ticket>.Filter.ElemMatch("attachments", subFilter);
                var result = MongoHelper.ticket_collection.Find(filter)
                    .Project(Builders<Ticket>.Projection.Exclude("_id")
                    .Include("attachments.$")).FirstOrDefault();

                //https://stackoverflow.com/questions/18068772/mongodb-c-sharp-how-to-work-with-bson-document
                var allFields = result["attachments"].AsBsonArray;
                foreach (var fields in allFields)
                {
                    //Reads from Ticket and Uses Ticket.Attachment.TicketID to Delete From Picture
                    var pictureFilter = Builders<Picture>.Filter.Eq("_id", fields["pictureID"].AsString);
                    MongoHelper.picture_collection.DeleteOne(pictureFilter);
                }
                //----------------------------------------------------------------------------------------------------------------------------Delete Existing Picture

                //Uploaded picture To replace the Existing Picture -------------------------------------------------------------------------------------------------
                //Get File Name
                string FileName = Path.GetFileName(theFile.FileName);

                //Get bytes from the stream of file
                byte[] PictureAsBytes = new byte[theFile.ContentLength];
                using (BinaryReader reader = new BinaryReader(theFile.InputStream))
                {
                    PictureAsBytes = reader.ReadBytes(theFile.ContentLength);
                }

                //convert bytes of image data to string using Base64 encoding
                string PictureDataAsString = Convert.ToBase64String(PictureAsBytes);

                Object PictureID = GenerateRandomId(24);
                MongoHelper.picture_collection.InsertOneAsync(new Picture
                {
                    _id = PictureID,
                    fileName = FileName,
                    PictureDataAsString = PictureDataAsString
                });
                // -------------------------------------------------------------------------------------------------Uploaded picture To replace the Existing Picture
                var subFilterAttachment = Builders<Attachments>.Filter.Eq("_id", collection["attachmentID"]);
                var filterAttachment = Builders<Ticket>.Filter.ElemMatch("attachments", subFilterAttachment);

                var update = Builders<Ticket>.Update.Set("attachments.$.notes", collection["notesText"]);//Update For Attachment Note
                var update2 = Builders<Ticket>.Update.Set("attachments.$.pictureID", PictureID);//Update Picture's ID
                MongoHelper.ticket_collection.UpdateOne(filterAttachment, update);
                MongoHelper.ticket_collection.UpdateOne(filterAttachment, update2);
            }
            else
            {
                //Changes Note if no picture is uploaded
                var subFilterAttachment = Builders<Attachments>.Filter.Eq("_id", collection["attachmentID"]);
                var filterAttachment = Builders<Ticket>.Filter.ElemMatch("attachments", subFilterAttachment);

                var update = Builders<Ticket>.Update.Set("attachments.$.notes", collection["notesText"]);
                MongoHelper.ticket_collection.UpdateOne(filterAttachment, update);
            }

            return RedirectToAction("EditTicket", "User", new { id = collection["TicketID"], message = "EditAttachment" });
        }

        // POST: DeleteAttachment from EditTicket
        //Deletes Attachment in Ticket
        [HttpPost]
        public ActionResult DeleteAttachment(FormCollection collection)
        {
            //https://stackoverflow.com/questions/38258139/filter-elemmatch
            MongoHelper.ConnectToMongo();
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");
            MongoHelper.picture_collection =
                    MongoHelper.database.GetCollection<Picture>("Picture");

            var subFilter = Builders<Attachments>.Filter.Eq("_id", collection["attachmentID"]);
            var filter = Builders<Ticket>.Filter.ElemMatch("attachments", subFilter);
            var result = MongoHelper.ticket_collection.Find(filter)
                .Project(Builders<Ticket>.Projection.Exclude("_id")
                .Include("attachments.$")).FirstOrDefault();

            //https://stackoverflow.com/questions/18068772/mongodb-c-sharp-how-to-work-with-bson-document
            var allFields = result["attachments"].AsBsonArray;
            foreach (var fields in allFields)
            {
                //Reads from Ticket and Uses Ticket.Attachment.TicketID to Delete From Picture
                var pictureFilter = Builders<Picture>.Filter.Eq("_id", fields["pictureID"].AsString);
                MongoHelper.picture_collection.DeleteOne(pictureFilter);
            }
            //Removes Attachment from Ticket
            var ticketFilter = Builders<Ticket>.Filter.Eq("_id", collection["TicketID"]);
            var update = Builders<Ticket>.Update.PullFilter("attachments", Builders<Attachments>.Filter.Eq("_id", collection["attachmentID"]));
            MongoHelper.ticket_collection.FindOneAndUpdateAsync(ticketFilter, update);
            //Removes Attachment from Ticket
            return RedirectToAction("EditTicket", "User", new { id = collection["TicketID"], message = "DeleteAttachment" });
        }
        //Function to display image on our website
        public FileContentResult ShowPicture(string id)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.picture_collection =
                MongoHelper.database.GetCollection<Picture>("Picture");
            var filter = Builders<Picture>.Filter.Eq("_id", id);
            var result = MongoHelper.picture_collection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                var PictureDataAsBytes = Convert.FromBase64String(result.PictureDataAsString);
                return new FileContentResult(PictureDataAsBytes, "image/jpeg");
            }
            else
            {
                return null;
            }
        }

        //POST: DeleteHistory from EditTicketPage
        //Delete Ticket History
        [HttpPost]
        public ActionResult DeleteHistory(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.ticket_collection =
                MongoHelper.database.GetCollection<Ticket>("Ticket");

            var filter = Builders<Ticket>.Filter.Eq("_id", collection["TicketID"]);
            var update = Builders<Ticket>.Update.PullFilter("histories", Builders<Histories>.Filter.Eq("_id", collection["historyID"]));
            MongoHelper.ticket_collection.FindOneAndUpdateAsync(filter, update);

            return RedirectToAction("EditTicket", "User", new { id = collection["TicketID"], message = "DeleteHistory" });
        }

        //Ticket Pages End---------------------------------------------------------------------------------------------------------------------------->
        //GET: Message(Inbox)
        [MyRoleAuthorization("Manager", "Admin", "Developer", "Submitter", "IsUser")]
        public ActionResult Message()
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.message_collection =
                MongoHelper.database.GetCollection<Message>("Message");
            var filter = Builders<Message>.Filter.Eq("Recipient", Session["Email"]);
            var result = MongoHelper.message_collection.Find(filter).ToList();
            //Refreshes MessageList
            if (result != null)
            {
                Session["MessageList"] = result;
                return View(result);
            }
            else
            {
                List<Message> Message = new List<Message>();
                return View(Message);
            }
        }
        //GET: SendMessage
        [MyRoleAuthorization("Manager", "Admin", "Developer", "Submitter", "IsUser")]
        public ActionResult SendMessage()
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            List<Project> Projects = new List<Project>();
            var userFilter = Builders<User>.Filter.Ne("_id", "");//Query All Existing Users
            var userResult = MongoHelper.users_collection.Find(userFilter).ToList();
            var userFilter2 = Builders<User>.Filter.Eq("_id", Session["ID"]);//Query User in DB for User in Session
            var userResult2 = MongoHelper.users_collection.Find(userFilter2).FirstOrDefault();
            if (userResult2 != null)
            {
                for (int i = 0; i < userResult2.AdminStatus.Count(); i++)//Loop all Projects in User's List of Projects
                {
                    var projectFilter = Builders<Project>.Filter.Eq("_id", userResult2.AdminStatus[i].ProjectID);
                    var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
                    if (projectResult != null)
                    {
                        Projects.Add(projectResult);//Creates a List of Project that User is in
                    }
                }
            }
            var tupleModel = new Tuple<List<User>, List<Project>>(userResult, Projects);
            return View(tupleModel);

        }

        // POST: SendMessage
        //Sends Message between users on the Website
        [HttpPost]
        public ActionResult SendMessage(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.message_collection = MongoHelper.database.GetCollection<Message>("Message");
            if (collection["Subject"] == "" && collection["Message"] == "" && collection["Select"] == null)//Empty Subject, Message and No Recipient
            {
                return RedirectToAction("SendMessage", "User", new { message = "NoSubjectOrMessageOrSelect" });
            }
            else if (collection["Subject"] == "" && collection["Message"] == "")//Empty Subject and Message
            {
                return RedirectToAction("SendMessage", "User", new { message = "NoSubjectOrMessage" });
            }
            else if (collection["Subject"] == "") //Empty Subject
            {
                return RedirectToAction("SendMessage", "User", new { message = "NoSubject" });
            }
            else if (collection["Message"] == "") //Empty Message
            {
                return RedirectToAction("SendMessage", "User", new { message = "NoMessage" });
            }
            if (collection["Select"] != null) //If a Recipient from Select has been selected
            {
                string[] token = collection["Select"].ToString().Split(',');//List of User Selected
                List<string> unique = new List<string>();
                for (int i = 0; i < token.Length; i++)
                {
                    if (!unique.Contains(token[i]))//Creates a list of one instance of each user
                    {
                        unique.Add(token[i]);
                    }
                }
                for (int i = 0; i < unique.Count; i++)
                {
                    MongoHelper.message_collection.InsertOneAsync(new Message//New Message object for each User
                    {
                        _id = GenerateRandomId(24),
                        Recipient = unique[i],
                        Sender = Session["Username"].ToString(),
                        Subject = collection["Subject"],
                        Content = collection["Message"],
                        ProjectID_Invite = "N/A",
                        Project_Status = "N/A"
                    });
                }
                return RedirectToAction("SendMessage", "User", new { message = "Sent" });
            }
            return RedirectToAction("SendMessage", "User", new { message = "NoRecipient" });
        }

        // POST: AcceptInvite from Message
        //Accepts Message Invite and adds Recipient to Project
        [HttpPost]
        public ActionResult AcceptInvite(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.message_collection =
                MongoHelper.database.GetCollection<Message>("Message");
            MongoHelper.project_collection =
                MongoHelper.database.GetCollection<Project>("Project");
            var messageFilter = Builders<Message>.Filter.Eq("_id", collection["item._id"]);
            var messageResult = MongoHelper.message_collection.Find(messageFilter).FirstOrDefault();
            if (messageResult != null)
            {
                //Pushes ProjectID to User from Message
                MongoHelper.users_collection =
                    MongoHelper.database.GetCollection<User>("Users");
                var userFilter = Builders<User>.Filter.Eq("Email", messageResult.Recipient);
                var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
                if (userResult != null)
                {
                    //Pushes ProjectID and AdminStatus to User from Message----------------------------------
                    Privilege privilege = new Privilege()
                    {
                        _id = GenerateRandomId(24),
                        Status = messageResult.Project_Status,
                        ProjectID = messageResult.ProjectID_Invite,
                    };
                    var userUpdate = Builders<User>.Update
                        .Push("AdminStatus", privilege);
                    MongoHelper.users_collection.UpdateOneAsync(userFilter, userUpdate);
                    //----------------------------------Pushes ProjectID and AdminStatus to User from Message

                    //Pushes UserID to Project from user_collection-------------------------------------------
                    var projectFilter = Builders<Project>.Filter.Eq("_id", messageResult.ProjectID_Invite);
                    var projectResult = MongoHelper.project_collection.Find(projectFilter).FirstOrDefault();
                    if (projectResult == null) 
                    {
                        return RedirectToAction("Message", "User", new { message = "NoProject" });
                    }
                    var projectUpdate = Builders<Project>.Update.Push("UserID", userResult._id.ToString());
                    MongoHelper.project_collection.UpdateOneAsync(projectFilter, projectUpdate);
                    //-------------------------------------------Pushes UserID to Project from user_collection

                    //Deletes Message
                    MongoHelper.message_collection.DeleteOne(messageFilter);
                    //Deletes Message
                    return RedirectToAction("Message", "User", new { message = "accept" });
                }
                else
                {
                    return RedirectToAction("Message", "User", new { message = "error" });
                }
            }
            else
            {
                return RedirectToAction("Message", "User", new { message = "error" });
            }
        }

        // POST: Delete Message from Message
        //Deletes Message from Account
        [HttpPost]
        public ActionResult DeleteMessage(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.message_collection =
                MongoHelper.database.GetCollection<Message>("Message");
            var filter = Builders<Message>.Filter.Eq("_id", collection["item._id"]);
            MongoHelper.message_collection.DeleteOne(filter);

            return RedirectToAction("Message", "User", new { message = "delete" });
        }


        //User Pages ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------>
        // GET: UserProfile
        public ActionResult UserProfile()
        {
            List<User> userList = new List<User>();
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            var userFilter = Builders<User>.Filter.Eq("_id", Session["ID"]);
            var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
            if (userResult != null)
            {
                userList.Add(userResult);
                return View(userList);
            }
            else
            {
                return RedirectToAction("Unauthorized", "Error");
            }
        }
        //POST: ChangeUsername from UserProfile page
        //Updates Username for your account
        [HttpPost]
        public ActionResult ChangeUsername(FormCollection collection)
        {
            foreach (string element in collection)
            {
                if (collection[element] == "")
                {
                    return RedirectToAction("UserProfile", "User", new { message = "FillAll" });//If one form is empty, Return
                }
            }
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            var filter = Builders<User>.Filter.Eq("_id", Session["ID"]);
            var result = MongoHelper.users_collection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                if (SecurePasswordHasher.Verify(collection["Password"], result.Password))
                {
                    var update = Builders<User>.Update
                    .Set("Username", collection["Username"]);
                    MongoHelper.users_collection.UpdateOne(filter, update);
                    Session["Username"] = collection["Username"];//Sets Username to Session to display on pages
                    return RedirectToAction("UserProfile", "User", new { message = "UsernameChanged" });
                }
                else
                {
                    return RedirectToAction("UserProfile", "User", new { message = "IncorrectPassword" });//Wrong Confirmation Password
                }
            }
            else
            {
                return RedirectToAction("UserProfile", "User", new { message = "Error" });//Can't Find User in database
            }
        }
        //POST: ChangeEmail from UserProfile page
        //Updates Email for your account
        [HttpPost]
        public ActionResult ChangeEmail(FormCollection collection)
        {
            foreach (string element in collection)
            {
                if (collection[element] == "")
                {
                    return RedirectToAction("UserProfile", "User", new { message = "FillAll" });//If one form is empty. and return
                }
            }
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            var filter = Builders<User>.Filter.Eq("_id", Session["ID"]);
            var result = MongoHelper.users_collection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                var checkFilter = Builders<User>.Filter.Eq("Email", collection["Email"]);
                var checkResult = MongoHelper.users_collection.Find(checkFilter).FirstOrDefault();
                if (checkResult == null)//If no matching email
                {
                    if (SecurePasswordHasher.Verify(collection["Password"], result.Password))
                    {
                        var update = Builders<User>.Update
                        .Set("Email", collection["Email"]);
                        MongoHelper.users_collection.UpdateOne(filter, update);
                        Session["Email"] = collection["Email"];//Sets Username to Session to display on pages
                        return RedirectToAction("UserProfile", "User", new { message = "EmailChanged" });
                    }
                    else
                    {
                        return RedirectToAction("UserProfile", "User", new { message = "IncorrectPassword" });//Wrong Confirmation Password
                    }
                }
                else
                {
                    return RedirectToAction("UserProfile", "User", new { message = "EmailTaken" });//An Account already has that email that you are changing to
                }
            }
            else
            {
                return RedirectToAction("UserProfile", "User", new { message = "Error" });//Can't Find User in database
            }
        }

        //POST: ChangePassword from UserProfile
        //Updates Password for your account
        [HttpPost]
        public ActionResult ChangePassword(FormCollection collection)
        {
            foreach (string element in collection)
            {
                if (collection[element] == "")
                {
                    return RedirectToAction("UserProfile", "User", new { message = "FillAll" });//If one form is empty, return
                }
            }
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            if (string.Equals(collection["NewPassword"], collection["ConfirmPassword"]))//If Password is the same
            {
                var filter = Builders<User>.Filter.Eq("_id", Session["ID"]);
                var result = MongoHelper.users_collection.Find(filter).FirstOrDefault();
                if (result != null)
                {
                    var update = Builders<User>.Update.Set("Password", SecurePasswordHasher.Hash(collection["NewPassword"]));//Update password with a hashed one
                    MongoHelper.users_collection.UpdateOne(filter, update);
                    return RedirectToAction("UserProfile", "User", new { message = "PasswordChanged" });
                }
                else
                {
                    return RedirectToAction("UserProfile", "User", new { message = "Error" });//Can't find User in Database
                }
            }
            else
            {
                return RedirectToAction("UserProfile", "User", new { message = "PasswordMismatch" });//Passwords don't match
            }
        }
        //User Pages ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------>
        //POST: Logout
        [HttpPost]
        public ActionResult Logout()
        {
            Session.Abandon();//Clears all Session variables and values
            return RedirectToAction("Login", "Home");
        }

        //Generate an ID for User
        private static Random random = new Random();
        private object GenerateRandomId(int v)
        {
            string strarray = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(strarray, v).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //https://stackoverflow.com/questions/4181198/how-to-hash-a-password/10402129#10402129
        public static class SecurePasswordHasher
        {
            //Size of Salt
            private const int SaltSize = 16;
            //Size of Hash
            private const int HashSize = 20;

            //Creates a hash from a string(password)
            public static string Hash(string password, int iteration)
            {
                //Create Salt
                byte[] salt;
                new RNGCryptoServiceProvider().GetBytes(salt = new byte[SaltSize]);

                //Create hash
                var pdbkdf2 = new Rfc2898DeriveBytes(password, salt, iteration);
                var hash = pdbkdf2.GetBytes(HashSize);

                //Combine Salt and hash
                var hashBytes = new byte[SaltSize + HashSize];
                Array.Copy(salt, 0, hashBytes, 0, SaltSize);
                Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

                //Convert to base64
                var base64Hash = Convert.ToBase64String(hashBytes);

                //Format hash with extra info
                return string.Format("$MYHASH$V1${0}${1}", iteration, base64Hash);
            }

            //Create a hash password with 10000 iterations
            public static string Hash(string password)
            {
                return Hash(password, 10000);
            }

            //Checks if hash is supported
            public static bool IsHashSupported(string hashString)
            {
                return hashString.Contains("$MYHASH$V1$");
            }

            //Verifies a password against a hash
            public static bool Verify(string password, string hashedPassword)
            {
                if (!IsHashSupported(hashedPassword))
                {
                    throw new NotSupportedException("The hashtype is not supported");
                }
                // Extract iteration and Base64 string
                var splittedHashString = hashedPassword.Replace("$MYHASH$V1$", "").Split('$');
                var iterations = int.Parse(splittedHashString[0]);
                var base64Hash = splittedHashString[1];

                // Get hash bytes
                var hashBytes = Convert.FromBase64String(base64Hash);

                // Get salt
                var salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                // Create hash with given salt
                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations);
                byte[] hash = pbkdf2.GetBytes(HashSize);

                //Get Result
                for (var i = 0; i < HashSize; i++)
                {
                    if (hashBytes[i + SaltSize] != hash[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

    }
}
