using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using TimeTable.Models;

namespace TimeTable.Controllers
{
    public class HomeController : Controller
    {
        DailyRoutineEntities db = new DailyRoutineEntities();
        public ActionResult Index()
        {
            //Fetch data from database and convert it to JSON format
            var events = db.Time_tabledb.Select(e => new
            {
                id = e.Id,
                title = e.TopicName,
                start = e.Date, //Convert date to ISO string format              
                description = e.Message               
            }).ToList();

            //Pass the JSON data to the View
            ViewBag.Events = Newtonsoft.Json.JsonConvert.SerializeObject(events);            
            return View();
        }

        [HttpPost]
        public JsonResult UpdateDeleteEvent(int eventId, string topicName, string message, string action)
        {
            if (action == "update")
            {
                using (var eventdb = new DailyRoutineEntities())
                {
                    var @event = eventdb.Time_tabledb.Find(eventId);
                    if (@event != null)
                    {
                        @event.TopicName = topicName;
                        @event.Message = message;
                        eventdb.SaveChanges();
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false });
                    }
                }
            }
            else if (action == "delete")
            {
                // Get the event from the database
                var evt = db.Time_tabledb.Find(eventId);
                if (evt != null)
                {
                    // Delete the event from the database
                    db.Time_tabledb.Remove(evt);
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false });
                }
            }

            return Json(new { success = true });
        }


        [HttpPost]
        public ActionResult AddData(string topicName, string message, string selectedDate, Time_tabledb tb)
        {
            tb.Date = selectedDate;
            tb.Message = message;
            tb.TopicName = topicName;
            // Insert the data into the database
            db.Time_tabledb.Add(tb);
            db.SaveChanges();
            // Return a success message to the user
            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult News()
        {
            List<NewsData> newsList = new List<NewsData>();
            // Replace YOUR_API_KEY with your actual API key 
            string url = "https://newsapi.org/v2/top-headlines?country=in&apiKey=";
            
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);

                HttpResponseMessage response = client.GetAsync("").Result;

                if (response.IsSuccessStatusCode)
                {
                    string data = response.Content.ReadAsStringAsync().Result;

                    JObject jsonData = JObject.Parse(data);

                    List<JToken> articles = jsonData["articles"].Children().ToList();

                    

                    foreach (JToken article in articles)
                    {
                        NewsData news = new NewsData();

                        news.Title = article["title"].ToString();
                        news.Description = article["description"].ToString();
                        news.Url = article["url"].ToString();
                        news.ImageUrl = article["urlToImage"].ToString();
                       
                        newsList.Add(news);
                        //db.NewsModels.Add(news);
                       // db.SaveChanges();
                    }

                    return View(newsList);
                }
            }

            return View();
        }
           
        

        public ActionResult Email()
        {        

            return View();
        }

        [HttpPost]
        public ActionResult SendMail(string to, string subject, string message, HttpPostedFileBase attachment)
        {
            try
            {
                var fromAddress = new MailAddress("gmail.com", "Mr.Robot");
                var toAddress = new MailAddress(to, "");
                const string fromPassword = "";
                const string body = "Message: {0}";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                var mailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = string.Format(body, message)
                };

                if (attachment != null && attachment.ContentLength > 0)
                {
                    var attachmentFileName = System.IO.Path.GetFileName(attachment.FileName);
                    mailMessage.Attachments.Add(new Attachment(attachment.InputStream, attachmentFileName));
                }

                smtp.Send(mailMessage);

                ViewBag.Message = "Mail Sent Successfully";
                return RedirectToAction("Email");
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            return View("Index");
        }
    }
}