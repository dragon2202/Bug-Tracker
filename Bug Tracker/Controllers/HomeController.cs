using Bug_Tracker.Models;
using MongoDB.Driver;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Mvc;
    
namespace Bug_Tracker.Controllers
{
    public class HomeController : Controller
    {
        //GET: HomePage
        public ActionResult Index()
        {
            return View();
        }
        //GET: Login 
        public ActionResult Login()
        {
            return View();
        }

        // POST: Home/Login
        //Checks is User Info is correct, before logging in
        [HttpPost]
        public ActionResult Login(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");

            var filter = Builders<User>.Filter.Eq("Email", collection["Email"]);
            var result = MongoHelper.users_collection.Find(filter).FirstOrDefault();

            foreach (string element in collection)
            {
                if (collection[element] == "")
                {
                    return RedirectToAction("Login", "Home", new { message = "FillAll" });//If a field is empty, return
                }
            }
            if (result == null)//No matching Email
            {
                return RedirectToAction("Login", "Home", new { message = "WrongEmail" });
            }
            else
            {
                if (SecurePasswordHasher.Verify(collection["Password"], result.Password))//compares hashed and given password
                {
                    //Applies Session Variables
                    Session["Email"] = result.Email;
                    Session["UserName"] = result.Username;
                    Session["ID"] = result._id;
                    Session["AdminStatus"] = "IsUser";
                    //https://forums.asp.net/t/1450177.aspx?Saving+List+int+to+a+session+and+retrieving+it+

                    MongoHelper.message_collection =
                        MongoHelper.database.GetCollection<Message>("Message");
                    var messageFilter = Builders<Message>.Filter.Eq("Recipient", Session["Email"]);
                    var messageResult = MongoHelper.message_collection.Find(messageFilter).ToList();
                    Session["MessageList"] = messageResult;//Retrieves Messages from database
                    return RedirectToAction("Dashboard", "User");
                }
                else
                {
                    return RedirectToAction("Login", "Home", new { message = "WrongPassword" });
                }
            }
        }
        //GET:GuestLogin
        public ActionResult GuestLogin()
        {
            return View();
        }

        // POST: Home/GuestLogin
        //Login as a Guest
        [HttpPost]
        public ActionResult GuestLogin(FormCollection collection)
        {
            //Applies Guest Sessions
            Session["UserName"] = "Guest " + collection["Status"];
            Session["ID"] = "Guest";
            Session["AdminStatus"] = collection["Status"];
            return RedirectToAction("Dashboard", "User");
        }
        //GET: Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Home/Register
        //Creates a New Account
        [HttpPost]
        public ActionResult Register(FormCollection collection)
        {

            try
            {
                foreach (string element in collection)
                {
                    if (collection[element] == "")
                    {
                        return RedirectToAction("Register", "Home", new { message = "FillAll" });//If a field is empty, return
                    }
                }
                if (string.Equals(collection["Password"], collection["ConfirmPassword"]))//Checks if both given passwords are the same
                {
                    //https://stackoverflow.com/questions/4181198/how-to-hash-a-password/10402129#10402129
                    MongoHelper.ConnectToMongo();
                    MongoHelper.users_collection =
                        MongoHelper.database.GetCollection<User>("Users");

                    //Create Id
                    Object id = GenerateRandomId(24);
                    var filter = Builders<User>.Filter.Eq("Email", collection["Email"]);
                    var result = MongoHelper.users_collection.Find(filter).FirstOrDefault();

                    if (result == null)//If no matching email address in db
                    {
                        //Create a hash password
                        var hash = SecurePasswordHasher.Hash(collection["Password"]);
                        //Creates a new User Object
                        MongoHelper.users_collection.InsertOneAsync(new User
                        {
                            _id = id,
                            Email = collection["Email"],
                            Username = collection["Username"],
                            Password = hash,
                            AdminStatus = new List<Privilege>()
                        });
                        return RedirectToAction("Register", "Home", new { message = "success" });
                    }
                    else
                    {
                        return RedirectToAction("Register", "Home", new { message = "registered" });
                    }

                }
                else
                {
                    return RedirectToAction("Register", "Home", new { message = "nonmatch" });
                }
            }
            catch
            {
                return RedirectToAction("Register", "Home");
            }
        }

        //GET: Forgot
        //Deletes Expired Tokens in DB when anybody access this page
        public ActionResult Forgot()
        {
            //Delete Expired Token After 15 minutes
            MongoHelper.ConnectToMongo();
            MongoHelper.recovery_collection =
                MongoHelper.database.GetCollection<AccountRecovery>("AccountRecovery");
            var recoveryFilter = Builders<AccountRecovery>.Filter.Ne("_id", "");
            var recoveryResult = MongoHelper.recovery_collection.Find(recoveryFilter).ToList();
            if (recoveryResult != null)
            {
                for (int i = 0; i < recoveryResult.Count(); i++)
                {
                    if (recoveryResult[i].expirationDateTime.AddMinutes(15) < DateTime.UtcNow)//MongoDB stores in UTC Time, therefore using DateTime.UtcNow will compare it in real time
                    {
                        var deleteFilter = Builders<AccountRecovery>.Filter.Eq("_id", recoveryResult[i]._id);
                        MongoHelper.recovery_collection.DeleteOneAsync(deleteFilter);//deletes Expired Tokens
                    }
                }
            }
            //Delete Token After one Hour
            return View();
        }

        [HttpPost]
        //Deletes Expired Tokens and Sends Emails to accounts on how to reset password
        public async Task<ActionResult> Forgot(FormCollection collection)
        {
            MongoHelper.ConnectToMongo();
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            MongoHelper.recovery_collection =
                MongoHelper.database.GetCollection<AccountRecovery>("AccountRecovery");

            var userFilter = Builders<User>.Filter.Eq("Email", collection["Email"]);
            var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
            if (userResult != null)
            {
                var recoveryFilter = Builders<AccountRecovery>.Filter.Eq("accountID", userResult._id);
                var recoveryResult = MongoHelper.recovery_collection.Find(recoveryFilter).ToList();
                if (recoveryResult != null)
                {
                    for (int i = 0; i < recoveryResult.Count(); i++)//Deletes Extra Tokens for the Account, Expired or Not Expired
                    {
                        var deleteFilter = Builders<AccountRecovery>.Filter.Eq("_id", recoveryResult[i]._id);
                        var deleteUpdate = MongoHelper.recovery_collection.DeleteOneAsync(deleteFilter);
                    }
                }
                Object id = GenerateRandomId(24);
                string token = GenerateRandomId(24).ToString();
                await MongoHelper.recovery_collection.InsertOneAsync(new AccountRecovery
                {
                    _id = id,
                    accountID = userResult._id.ToString(),
                    token = SecurePasswordHasher.Hash(token),
                    expirationDateTime = DateTime.Now.AddMinutes(15)//Adds 15 minutes before token expiration 
                });
                await SendMail(collection["Email"].ToString(), id, token);
                return RedirectToAction("Forgot", "Home", new { message = "Sent" });
            }
            else
            {
                return RedirectToAction("Forgot", "Home", new { message = "Failure" });
            }
        }
        //https://sendgrid.com/docs/for-developers/sending-email/v3-csharp-code-example/
        //Sends Emails to Accounts
        async Task SendMail(string email, Object id, string token)
        {
            string Host = null;
            if (System.Web.HttpContext.Current.Request.Url.Host == "localhost")//NonProduction Link
            {
                Host = "<html><body><a href='https://" + System.Web.HttpContext.Current.Request.Url.Host + ":" + System.Web.HttpContext.Current.Request.Url.Port + "/Home/ResetPassword/" + id + "'> Reset</a>";
            }
            else
            {
                Host = "<html><body><a href='https://" + System.Web.HttpContext.Current.Request.Url.Host + "/Home/ResetPassword" + id + "'>Reset</a>";//Production Link
            }
            var key = "";//REMOVED For Security Reasons. A SendGrid api key would be here instead
            var client = new SendGridClient(key);
            var from = new EmailAddress("em2999@gmail.com", "Bug Tracker");
            var subject = "Password Reset";
            var to = new EmailAddress(email, "");
            var plainTextContent = "Reset Password for Bug Tracker";
            var htmlContent = "<strong>Click on this link to start the process to reset your password" + Host + "</strong> <br> <strong>Token: " + token + "<br> This Token Expires On " + String.Format("{0:F}", DateTime.Now.AddMinutes(15)) + "</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
        //GET: ResetPassword
        public ActionResult ResetPassword(string id)
        {
            return View();
        }
        //POST:ResetPassword
        //Carries out the function to replace password
        [HttpPost]
        public ActionResult ResetPassword(FormCollection collection)
        {
            foreach (string element in collection)
            {
                if (collection[element] == "")
                {
                    return RedirectToAction("ResetPassword", "Home", new { message = "FillAll" });//If one form is empty
                }
            }
            MongoHelper.ConnectToMongo();
            MongoHelper.recovery_collection =
                MongoHelper.database.GetCollection<AccountRecovery>("AccountRecovery");
            MongoHelper.users_collection =
                MongoHelper.database.GetCollection<User>("Users");
            var filter = Builders<AccountRecovery>.Filter.Eq("_id", collection["ID"]);//Finds the Account Recovery Info and the account related to it
            var result = MongoHelper.recovery_collection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                if (SecurePasswordHasher.Verify(collection["Token"], result.token))
                {
                    if (result.expirationDateTime < DateTime.UtcNow)//MongoDB stores in UTC Time, therefore using DateTime.UtcNow will compare it in real time
                    {
                        return RedirectToAction("ResetPassword", "Home", new { message = "TokenExpiration" });
                    }
                    else
                    {
                        if (string.Equals(collection["Password"], collection["ConfirmPassword"]))//Passwords Match
                        {
                            var userFilter = Builders<User>.Filter.Eq("_id", result.accountID);
                            var userResult = MongoHelper.users_collection.Find(userFilter).FirstOrDefault();
                            if (userResult != null)
                            {
                                var update = Builders<User>.Update.Set("Password", SecurePasswordHasher.Hash(collection["Password"]));//Replaces password with a new one
                                MongoHelper.users_collection.UpdateOne(userFilter, update);
                            }
                            MongoHelper.recovery_collection.DeleteOneAsync(filter);//Delete Token
                            return RedirectToAction("ResetPassword", "Home", new { message = "Success" });
                        }
                        else
                        {
                            return RedirectToAction("ResetPassword", "Home", new { message = "PasswordMismatch" });
                        }
                    }
                }
                else
                {
                    return RedirectToAction("ResetPassword", "Home", new { message = "TokenMismatch" });
                }
            }
            else
            {
                return RedirectToAction("ResetPassword", "Home", new { message = "Error" });
            }
        }

        //https://stackoverflow.com/questions/4181198/how-to-hash-a-password/10402129#10402129
        //Function to Hash Password
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


        //Generate an ID for User
        private static Random random = new Random();
        private object GenerateRandomId(int v)
        {
            string strarray = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(strarray, v).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
