using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Office.Interop.Excel;
using System.Data.OleDb;
using System.Data;
using System.Net.Http;
using Newtonsoft.Json.Linq;
namespace NaiveBayse.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult NaiveClassifier()
        {
            return View(new MailModel());
        }
        [HttpPost]
        public ActionResult NaiveClassifier(MailModel mModel)
        {
            if (!string.IsNullOrEmpty(mModel.mailText))
            {
                string filepath = @"~/Content/EnglishSpam.xlsx";
                string path = Server.MapPath(filepath);
                var connectionString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=Excel 12.0;", path);
                string sql = "SELECT * FROM [EnglishSpam$A1:B5000]";
                var adapter = new OleDbDataAdapter(sql, connectionString);
                System.Data.DataTable dataTable = new System.Data.DataTable();
                System.Data.DataTable dataTable2 = new System.Data.DataTable();
                DataSet ds = new DataSet();
                adapter.Fill(ds, "wordlist");
                dataTable = ds.Tables["wordlist"];
                List<Words> words = new List<Words>();
                int spamCount = 0;
                int hamCount = 0;
                int totalCount = 0;
                int TruePositive = 0;
                int TrueNegative = 0;
                int FalsePositive = 0;
                int FalseNegative = 0;
                foreach (DataRow dr in dataTable.Rows)
                {
                    string st = dr[1].ToString().Trim();
                    string classs = dr[0].ToString().ToLower().Trim();
                    string[] strArray = st.Replace(",", "").Replace(".", "").Replace(";", "").Replace("'", "").Split(' ');

                    if (strArray.Length > 0)
                    {
                        foreach (var wo in strArray)
                        {
                            if (wo.Length > 2)
                            {
                                var chk = words.Where(x => x.word == wo.ToString()).FirstOrDefault();
                                if (chk == null)
                                {
                                    Words w = new Words
                                    {
                                        word = wo.ToString().ToLower(),
                                        wordCount = 1,

                                    };
                                    if (classs == "ham")
                                    {
                                        w.hamCount = 1;
                                    }
                                    else
                                    {
                                        w.spamCount = 1;
                                    }
                                    words.Add(w);
                                }
                                else
                                {
                                    chk.wordCount++;
                                    if (classs == "ham")
                                    {
                                        chk.hamCount++;
                                    }
                                    else
                                    {
                                        chk.spamCount++;
                                    }
                                }
                            }
                        }
                    }
                    if (classs == "ham")
                    {
                        hamCount++;
                    }
                    else
                    {
                        spamCount++;
                    }
                }
                totalCount = spamCount + hamCount;
                foreach (var item in words)
                {
                    item.probOfWordWithHam = item.hamCount / hamCount;
                    item.probOfWordWithSpam = item.spamCount / spamCount;
                }
                sql = "SELECT * FROM [EnglishSpam$A5001:B5500]";
                adapter = new OleDbDataAdapter(sql, connectionString);
                adapter.Fill(ds, "wordlist2");
                dataTable2 = ds.Tables["wordlist2"];
                foreach (DataRow dr in dataTable2.Rows)
                {
                    string st = dr[1].ToString().ToLower();
                    string classs = dr[0].ToString();
                    string[] strArray = st.Replace(",", "").Replace(".", "").Replace(";", "").Replace("'", "").Split(' ');
                    decimal pHam = 1;
                    decimal pSpam = 1;
                    if (strArray.Length > 0)
                    {
                        foreach (var wo in strArray)
                        {
                            if (wo.Length > 2 && words.Where(x => x.word == wo.ToString().ToLower()).ToList().Count > 0)
                            {
                                pHam = pHam * words.Where(x => x.word == wo.ToString().ToLower()).FirstOrDefault().probOfWordWithHam;
                                pSpam = pSpam * words.Where(x => x.word == wo.ToString().ToLower()).FirstOrDefault().probOfWordWithSpam;
                            }
                        }
                    }
                    pHam = pHam * hamCount / totalCount;
                    pSpam = pSpam * spamCount / totalCount;
                    string result = "";
                    if (pHam > pSpam)
                    {
                        result = "ham";
                    }
                    else
                    {
                        result = "spam";
                    }
                    string classss = dr[0].ToString().ToLower().Trim();
                    if (result == classss && classss == "ham")
                    {
                        TruePositive++;
                    }
                    else if (result == classss && classss == "spam")
                    {
                        TrueNegative++;
                    }
                    else if (result != classss && classss == "ham")
                    {
                        FalsePositive++;
                    }
                    else
                    {
                        FalseNegative++;
                    }
                }
                ViewBag.TruePositive = TruePositive;
                ViewBag.TrueNegative = TrueNegative;
                ViewBag.FalsePositive = FalsePositive;
                ViewBag.FalseNegative = FalseNegative;

                string mailResult = CheckHamOrSpan(mModel.mailText, words, hamCount, spamCount, totalCount);
                ViewBag.MailResult = mailResult;
            }


            return View(new MailModel());
        }
        public string CheckHamOrSpan(string mailText, List<Words> words, decimal hamCount, decimal spamCount, decimal totalCount)
        {
            string st = mailText.ToLower();
            string[] strArray = st.Replace(",", "").Replace(".", "").Replace(";", "").Replace("'", "").Split(' ');
            decimal pHam = 1;
            decimal pSpam = 1;
            if (strArray.Length > 0)
            {
                foreach (var wo in strArray)
                {
                    if (wo.Length > 2 && words.Where(x => x.word == wo.ToString().ToLower()).ToList().Count > 0)
                    {
                        decimal pH = words.Where(x => x.word == wo.ToString().ToLower()).FirstOrDefault().probOfWordWithHam;
                        decimal pS = words.Where(x => x.word == wo.ToString().ToLower()).FirstOrDefault().probOfWordWithSpam;
                        //if (pH > 0 && pS > 0)
                        //{
                            pHam = pHam * pH;// == 0 ? one : pH);

                            pSpam = pSpam * pS;// == 0 ? one : pS);
                        //}
                    }
                }
            }
            pHam = pHam * hamCount / totalCount;
            pSpam = pSpam * spamCount / totalCount;
            if (pHam > pSpam)
            {
                return "ham";
            }
            else
            {
                return "spam";
            }

        }
        //public ActionResult Tweets()
        //{
        //    var model = new List<Tweets>();
        //    var client = new HttpClient();
        //    var task = 
        //        client.GetAsync("http://search.twitter.com/search.json?q=nepali")
        //        .ContinueWith((taskwithMsg) =>
        //        {
        //            var response = taskwithMsg.Result;
        //            var jsonTask = response.Content.ReadAsStringAsync();
        //            jsonTask.Wait();
        //            var jsonObject = jsonTask.Result;
        //            model.AddRange((
        //                from JObject jo in (JArray)jsonObject["results"]
        //                select new Tweets
        //                {
        //                    Name = jo["from_user"].ToString(),
        //                    Text = jo["text"].ToString()
        //                }
        //                ));

        //        });
        //    task.Wait();
        //    return View(model);
        //}
    }
    public class MailModel
    {
        public string mailText { get; set; }
    }
    public class Tweets
    {
        public string Name { get; set; }
        public string Text { get; set; }
    }
    public class Words
    {
        public Words()
        {
            hamCount = 1;
            spamCount = 1;
        }
        public string word { get; set; }
        public decimal wordCount { get; set; }
        public decimal hamCount { get; set; }
        public decimal spamCount { get; set; }
        public decimal probOfWordWithSpam { get; set; }
        public decimal probOfWordWithHam { get; set; }
    }
}