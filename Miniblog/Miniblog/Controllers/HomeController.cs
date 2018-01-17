using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Web.Mvc;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using System.Web.Configuration;

namespace Miniblog.Controllers
{
    public class HomeController : Controller
    {
        private string connectionString = @"Data Source=(local);AttachDbFilename=|DataDirectory|\miniblog.mdf;database=miniblog;Integrated Security=True;";

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string user)
        {
            /*var username = Request["username"];
            if(username == "admin")
            {
                Session["username"] = "admin";
                return RedirectToAction("Dashboard", "Admin");
            }
            else if(username == "user")
            {
                Session["username"] = "user";
                return RedirectToAction("Dashboard", "User");
            }*/
            // In order to make this code work -> replace all UPPERCASE-Placeholders with the corresponding data!
            var username = Request["username"];
            var password = Request["password"];
            int userId = 0;

            SqlConnection con = new SqlConnection();
            con.ConnectionString = connectionString;
            //con.ConnectionString = @"Data Source=(local);AttachDbFilename=|DataDirectory|\miniblog.mdf;database=miniblog;Integrated Security=True;";

            SqlCommand cmd = new SqlCommand();
            SqlDataReader reader;

            cmd.CommandText = "SELECT [Id], [Username], [Password], [Phonenumber] FROM [dbo].[User] WHERE [Username] = '" + username + "' AND [Password] = '" + password + "'";
            cmd.Connection = con;

            con.Open();

            reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (username == reader["Username"].ToString() && password == reader["Password"].ToString())
                    {
                        userId = Convert.ToInt32(reader["Id"]);
                        var request = (HttpWebRequest)WebRequest.Create("https://rest.nexmo.com/sms/json");

                        byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
                        byte[] key = Guid.NewGuid().ToByteArray();
                        string secret = Convert.ToBase64String(time.Concat(key).ToArray());

                        string expiry = DateTime.Now.AddMinutes(5).ToString();

                        cmd.CommandText = "INSERT INTO [dbo].[Token] (User_id, Tokenstring, Expiry) VALUES ('" + userId + "', '" + secret + "', '" + expiry + "')";
                        cmd.ExecuteNonQuery();

                        var postData = "api_key=1cb5b15d";
                        postData += "&api_secret=ea21d1dbbd4f86d4";
                        postData += "&to=" + reader["Phonenumber"];
                        postData += "&from=\"\"Miniblog\"\"";
                        postData += "&text=\"" + secret + "\"";
                        var data = Encoding.ASCII.GetBytes(postData);

                        request.Method = "POST";
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.ContentLength = data.Length;

                        using (var stream = request.GetRequestStream())
                        {
                            stream.Write(data, 0, data.Length);
                        }

                        var response = (HttpWebResponse)request.GetResponse();
                        var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                        ViewBag.Message = responseString;

                    }
                    else
                    {
                        ViewBag.Message = "Wrong Credentials";
                    }
                }
            }
            else
            {
                ViewBag.Message = "Username or password is wrong!";
            }

            con.Close();

            return RedirectToAction("Home", "SMS_Auth", new { userId = userId, username = username });
        }

        [HttpPost]
        public ActionResult SMS_Auth(int userId, string username)
        {
            SqlConnection con = new SqlConnection();
            con.ConnectionString = connectionString;

            SqlCommand cmd = new SqlCommand();
            SqlDataReader reader;

            cmd.CommandText = "SELECT [Id], [Username], [Password], [Phonenumber] FROM [dbo].[Token] WHERE [User_id] = '" + userId + "'";
            cmd.Connection = con;

            con.Open();

            reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (Convert.ToDateTime(reader["Expriy"]) > DateTime.Now)
                    {
                        string sms_key = Request["sms_key"];
                        string secret = reader["Tokenstring"].ToString();

                        if (sms_key == secret)
                        {
                            Session["userId"] = userId;
                            Session["username"] = username;

                            return RedirectToAction("Home", "Index");
                        }
                        else
                        {
                            ViewBag.Message = "SMS Code is wrong";
                        }
                    }
                    else
                    {
                        ViewBag.Message = "SMS Code is expired";

                    }
                }
            }

            con.Close();

            return View();
        }

        public ActionResult Logout()
        {
            string userId = Session["userId"].ToString();
            string sessionId = Session.SessionID;

            SqlConnection con = new SqlConnection();
            con.ConnectionString = connectionString;

            SqlCommand cmd = new SqlCommand();

            //entry in userlogin table
            cmd.CommandText = "UPDATE [dbo].[Userlogin] SET Deletedon = '" + DateTime.Now.ToString() + "' WHERE User_id = '" + userId + "' AND Sessionid = '" + sessionId + "'";
            cmd.Connection = con;
            con.Open();

            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO [dbo].[Userlog] (User_id, Action) VALUES ('" + userId + "', '" + DateTime.Now.ToString() + ": logout')";
            cmd.Connection = con;

            cmd.ExecuteNonQuery();

            con.Close();

            //destroy sessions
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Home", "Login");
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}