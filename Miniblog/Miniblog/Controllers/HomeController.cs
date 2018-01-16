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
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Login()
        {
            // In order to make this code work -> replace all UPPERCASE-Placeholders with the corresponding data!
            var username = Request["username"];
            var password = Request["password"];
            int userId = 0;

            var mode = "SMS";

            SqlConnection con = new SqlConnection();
            con.ConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"C:\\miniblog.mdf\";Integrated Security=True;";

            SqlCommand cmd = new SqlCommand();
            SqlDataReader reader;

            cmd.CommandText = "SELECT [Id], [username], [password], [phonenumber] FROM [dbo].[User] WHERE [username] = '" + username + "' AND [password] = '" + password + "'";
            cmd.Connection = con;

            con.Open();

            reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (username == reader["username"].ToString() && password == reader["password"].ToString())
                    {
                        userId = Convert.ToInt32(reader["id"]);
                        var request = (HttpWebRequest)WebRequest.Create("https://rest.nexmo.com/sms/json");

                        byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
                        byte[] key = Guid.NewGuid().ToByteArray();
                        string secret = Convert.ToBase64String(time.Concat(key).ToArray());

                        string expiry = DateTime.Now.AddMinutes(5).ToString();

                        cmd.CommandText = "INSERT INTO [dbo].[Token] (user_id, tokenstring, expiry) VALUES('" + userId + "', '" + secret + "', '" + expiry + "')";
                        cmd.ExecuteNonQuery();

                        var postData = "api_key=1cb5b15d";
                        postData += "&api_secret=ea21d1dbbd4f86d4";
                        postData += "&to=" + reader["phonenumber"];
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

            return RedirectToAction("Home", "SMS_Auth", new { userId = userId, username = username });
        }

        [HttpPost]
        public ActionResult SMS_Auth(int userId, string username)
        {
            SqlConnection con = new SqlConnection();
            con.ConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"C:\\miniblog.mdf\";Integrated Security=True;";

            SqlCommand cmd = new SqlCommand();
            SqlDataReader reader;

            cmd.CommandText = "SELECT [Id], [username], [password], [phonenumber] FROM [dbo].[Token] WHERE [user_id] = '" + userId + "'";
            cmd.Connection = con;

            con.Open();

            reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (Convert.ToDateTime(reader["expriy"]) > DateTime.Now)
                    {
                        string sms_key = Request["sms_key"];
                        string secret = reader["tokenstring"].ToString();

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
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}