﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Web.Mvc;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Globalization;

namespace Miniblog.Controllers
{
    public class HomeController : Controller
    {
        private string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"C:\\Users\\Rajethan Ranjan\\Source\\Repos\\Miniblog\\Miniblog\\Miniblog\\App_Data\\miniblog.mdf\";Integrated Security = True";

        public ActionResult Index()
        {
            SqlConnection con = new SqlConnection();
            con.ConnectionString = connectionString;

            SqlCommand cmd = new SqlCommand();
            SqlDataReader reader;

            List<string> rowList = new List<string>();

            cmd.CommandText = "SELECT [Id], [Title], [Content] FROM [dbo].[Post] WHERE [Status] = 1";
            cmd.Connection = con;
            con.Open();
            reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                while (reader.Read())
                {
                    rowList.Add(reader.GetString(1) + "|" + reader.GetString(2));
                }
            }
            else
            {
                ViewBag.Data = "No rows found.";
            }
            con.Close();
            
            return View(rowList);
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

            var username = Request["username"];
            var password = Request["password"];
            int userId = 0;

            SqlConnection con = new SqlConnection();
            con.ConnectionString = connectionString;

            SqlCommand cmd = new SqlCommand();
            SqlDataReader reader;

            //cmd.CommandText = "INSERT INTO [dbo].[User] (Vorname, Nachname, Phonenumber, Username, Password, Role, Status) VALUES ('asdf', 'asdf', 41793536573, 'asdf', 'asdf', 'User', 0)";
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
                        var secret = Convert.ToBase64String(time.Concat(key).ToArray());
                        //var secret = "Test";

                        //DateTime expiry = DateTime.Now.AddMinutes(5);
                        string tempDate = Convert.ToDateTime(DateTime.Now.AddMinutes(5), CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss");
                        //DateTime expiry = Convert.ToDateTime(tempDate);
                        using (SqlConnection conection = new SqlConnection(connectionString))
                        {
                            SqlCommand cmd2 = new SqlCommand();
                            cmd2.CommandText = "INSERT INTO [dbo].[Token] (Id, User_id, Tokenstring, Expiry) VALUES (, '" + userId + "', '" + secret + "', '" + tempDate + "')";
                            cmd2.Connection = conection;
                            conection.Open();
                            cmd2.ExecuteNonQuery();
                            conection.Close();
                        }

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

                        return RedirectToAction("SMS_Auth", "Home", new { userId = userId, username = username });
                    }
                    else
                    {
                        ViewBag.Message = "Username oder Passwort ist falsch";
                    }
                }
            }
            else
            {
                ViewBag.Message = "Something went wrong!";
            }

            con.Close();

            return View();
        }

        //[HttpPost]
        public ActionResult SMS_Auth(int userId, string username)
        {
            
            SqlConnection con = new SqlConnection();
            con.ConnectionString = connectionString;

            SqlCommand cmd = new SqlCommand();
            SqlDataReader reader;

            cmd.CommandText = "SELECT * FROM [dbo].[Token] WHERE [User_id] = '" + userId + "'";
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

                            using (SqlConnection conection = new SqlConnection(connectionString))
                            {
                                SqlCommand cmd2 = new SqlCommand();
                                cmd2.CommandText = "UPDATE [dbo].[Token] SET DELETED = '" + DateTime.Now.ToString() + "' WHERE [User_id] = '" + userId + "'";
                                cmd2.Connection = conection;
                                conection.Open();
                                cmd2.ExecuteNonQuery();
                                conection.Close();
                            }

                            using (SqlConnection conection = new SqlConnection(connectionString))
                            {
                                SqlCommand cmd2 = new SqlCommand();
                                cmd2.CommandText = "INSERT INTO [dbo].[Userlog] (User_id, Action) VALUES ('" + userId + "', '" + DateTime.Now.ToString() + ": login')";
                                cmd2.Connection = conection;
                                conection.Open();
                                cmd2.ExecuteNonQuery();
                                conection.Close();
                            }

                            using (SqlConnection conection = new SqlConnection(connectionString))
                            {
                                SqlCommand cmd2 = new SqlCommand();
                                cmd2.CommandText = "INSERT INTO [dbo].[Userlogin] (User_id, User_ipaddress, SessionId, Createon) VALUES ('" + userId + "', '" + Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString() + "', '" + Session.SessionID + "', '" + DateTime.Now.ToString() + "')";
                                cmd2.Connection = conection;
                                conection.Open();
                                cmd2.ExecuteNonQuery();
                                conection.Close();
                            }

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

            using (SqlConnection conection = new SqlConnection(connectionString))
            {
                SqlCommand cmd2 = new SqlCommand();
                cmd2.CommandText = "INSERT INTO [dbo].[Userlog] (User_id, Action) VALUES ('" + userId + "', '" + DateTime.Now.ToString() + ": logout')";
                cmd2.Connection = conection;
                conection.Open();
                cmd2.ExecuteNonQuery();
                conection.Close();
            }

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