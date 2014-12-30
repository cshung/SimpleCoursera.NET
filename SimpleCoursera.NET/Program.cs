﻿namespace SimpleCoursera.NET
{
    using HtmlAgilityPack;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    class LectureGroup
    {
        public string Name { get; set; }
        public List<LectureGroupItem> Items { get; set; }
    }

    class LectureGroupItem
    {

        public string Name { get; set; }

        public string ProtectedUrl { get; set; }

        public string UnprotectedUrl { get; set; }
    }

    class Program
    {
        private static Random random = new Random();

        private static void Main(string[] args)
        {
            Console.Write("Username: ");
            string email = Console.ReadLine();
            Console.Write("Password: ");
            string password = Console.ReadLine();

            var cookieJar = Login(email, password);
            if (cookieJar == null)
            {
                Console.WriteLine("Login failure");
            }
            else
            {
                List<string> courses = GetCourses(cookieJar).ToList();
                foreach (var course in courses)
                {
                    //if (course.Contains("compneuro-002"))
                    {
                        if (LoginCourse(cookieJar, course))
                        {
                            var lectureGroups = GetLecturePage(cookieJar, course);
                            foreach (var lectureGroup in lectureGroups)
                            {
                                Console.WriteLine("  " + lectureGroup.Name);
                                foreach (var lectureGroupItem in lectureGroup.Items)
                                {
                                    Console.WriteLine("    " + lectureGroupItem.Name);
                                    Console.WriteLine("    " + lectureGroupItem.UnprotectedUrl);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool LoginCourse(CookieContainer cookieJar, string homelink)
        {
            using (HttpMessageHandler handler = new HttpClientHandler { AllowAutoRedirect = true, CookieContainer = cookieJar })
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "User-Agent", @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22" },
                        { "Accept", "text/html, application/xhtml+xml, */*" },
                    };
                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri(string.Format("{0}auth/auth_redirector?type=login&subtype=normal", homelink));
                    request.Method = HttpMethod.Get;
                    foreach (var kvp in headers)
                    {
                        switch (kvp.Key.ToUpperInvariant())
                        {
                            case "HOST":
                                request.Headers.Host = kvp.Value;
                                break;
                            case "REFERER":
                                request.Headers.Referrer = new Uri(kvp.Value);
                                break;
                            default:
                                request.Headers.Add(kvp.Key, kvp.Value);
                                break;
                        }
                    }
                    var response = client.SendAsync(request).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        response.Dispose();
                        return true;
                    }
                }
            }

            return false;
        }

        private static IEnumerable<string> GetCourses(CookieContainer cookieJar)
        {
            using (HttpMessageHandler handler = new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true, CookieContainer = cookieJar })
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "User-Agent", @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22" },
                        { "Referer", "https://www.coursera.org/"},
                    };
                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri("https://www.coursera.org/maestro/api/topic/list_my");
                    request.Method = HttpMethod.Get;
                    foreach (var kvp in headers)
                    {
                        switch (kvp.Key.ToUpperInvariant())
                        {
                            case "HOST":
                                request.Headers.Host = kvp.Value;
                                break;
                            case "REFERER":
                                request.Headers.Referrer = new Uri(kvp.Value);
                                break;
                            default:
                                request.Headers.Add(kvp.Key, kvp.Value);
                                break;
                        }
                    }

                    var response = client.SendAsync(request).Result;
                    JArray deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(response.Content.ReadAsStringAsync().Result);
                    foreach (var entry in deserialized)
                    {
                        // Apparently we can also have other "stuff" too!
                        yield return entry["courses"][0]["home_link"].ToString();
                    }
                }
            }
        }

        private static CookieContainer Login(string email, string password)
        {
            CookieContainer cookieJar = new CookieContainer();
            using (HttpMessageHandler handler = new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true, CookieContainer = cookieJar })
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    string csrfToken1 = ConstructToken(24);
                    string csrfToken2 = ConstructToken(24);
                    string csrfToken2_cookie_suffix = ConstructToken(8);
                    var data = string.Format("email={0}&password={1}&webrequest=true", Uri.EscapeDataString(email), Uri.EscapeDataString(password));
                    var headers = new Dictionary<string, string>
                    {
                        { "User-Agent", @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22" },
                        { "Accept", "*/*" },
                        { "X-CSRFToken", csrfToken1 },
                        { "X-CSRF2-Cookie", "csrf2_token_" + csrfToken2_cookie_suffix },
                        { "X-CSRF2-Token", csrfToken2 },
                        { "X-Requested-With", "XMLHttpRequest" },
                    };
                    // headers.Add("Origin", "https://accounts.coursera.org");
                    headers.Add("Host", "accounts.coursera.org");
                    headers.Add("Referer", "https://accounts.coursera.org/signin?mode=signin&post_redirect=%2F");
                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri("https://accounts.coursera.org/api/v1/login");
                    request.Method = HttpMethod.Post;
                    request.Content = new StringContent(data);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    cookieJar.Add(request.RequestUri, new Cookie("csrftoken", csrfToken1));
                    cookieJar.Add(request.RequestUri, new Cookie("csrf2_token_" + csrfToken2_cookie_suffix, csrfToken2));

                    foreach (var kvp in headers)
                    {
                        switch (kvp.Key.ToUpperInvariant())
                        {
                            case "HOST":
                                request.Headers.Host = kvp.Value;
                                break;
                            case "REFERER":
                                request.Headers.Referrer = new Uri(kvp.Value);
                                break;
                            default:
                                request.Headers.Add(kvp.Key, kvp.Value);
                                break;
                        }
                    }
                    var response = client.SendAsync(request).Result;

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return null;
                    }

                    foreach (var setCookieHeader in response.Headers.GetValues("Set-Cookie"))
                    {
                        var cookieTokens = setCookieHeader.Split('=', ';');
                        string cookieName = cookieTokens[0];
                        string cookieValue = cookieTokens[1];
                        cookieJar.Add(new Uri("https://www.coursera.org"), new Cookie(cookieName, cookieValue));
                        cookieJar.Add(new Uri("https://class.coursera.org"), new Cookie(cookieName, cookieValue));
                    }
                }
            }
            return cookieJar;
        }

        private static IEnumerable<LectureGroup> GetLecturePage(CookieContainer cookieJar, string homelink)
        {
            using (HttpMessageHandler handler = new HttpClientHandler { AllowAutoRedirect = true, CookieContainer = cookieJar })
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "User-Agent", @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22" },
                        { "Accept", "text/html, application/xhtml+xml, */*" }
                    };
                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri(new Uri(homelink), "lecture/index");
                    request.Method = HttpMethod.Get;
                    foreach (var kvp in headers)
                    {
                        switch (kvp.Key.ToUpperInvariant())
                        {
                            case "HOST":
                                request.Headers.Host = kvp.Value;
                                break;
                            case "REFERER":
                                request.Headers.Referrer = new Uri(kvp.Value);
                                break;
                            default:
                                request.Headers.Add(kvp.Key, kvp.Value);
                                break;
                        }
                    }
                    var response = client.SendAsync(request).Result;
                    var doc = new HtmlDocument();
                    doc.LoadHtml(response.Content.ReadAsStringAsync().Result);
                    if (doc.DocumentNode != null)
                    {
                        var divs = doc.DocumentNode.Descendants("div");
                        var courseItemListDiv = divs.FirstOrDefault(d => d.Attributes["class"] != null && d.Attributes["class"].Value == "course-item-list");
                        if (courseItemListDiv != null)
                        {
                            var lectureGroupDivs = courseItemListDiv.ChildNodes.Where(n => n.Name == "div");
                            foreach (var lectureGroupDiv in lectureGroupDivs)
                            {
                                // Just looking at the lecture group divs
                                var lectureGroupNameNode = lectureGroupDiv.ChildNodes.FirstOrDefault(cn => cn.Name == "h3");
                                if (lectureGroupNameNode != null)
                                {
                                    LectureGroup lectureGroup = new LectureGroup();
                                    lectureGroup.Items = new List<LectureGroupItem>();
                                    lectureGroup.Name = WebUtility.HtmlDecode(lectureGroupNameNode.InnerText).Trim();
                                    var lectureListNode = lectureGroupDiv.NextSibling;
                                    if (lectureListNode != null)
                                    {
                                        var lectureGroupItemNodes = lectureListNode.ChildNodes.Where(n => n.Name == "li");
                                        foreach (var lectureGroupItemNode in lectureGroupItemNodes)
                                        {
                                            LectureGroupItem lectureGroupItem = new LectureGroupItem();
                                            lectureGroupItem.Name = WebUtility.HtmlDecode(lectureGroupItemNode.ChildNodes.First(n => n.Name == "a").FirstChild.InnerText).Trim();
                                            var videoNode = lectureGroupItemNode.ChildNodes.First(ccn => ccn.Name == "div").ChildNodes.FirstOrDefault(ccn => ccn.Attributes["title"] != null && ccn.Attributes["title"].Value == "Video (MP4)");
                                            if (videoNode != null)
                                            {
                                                string protectedUrl = videoNode.Attributes["href"].Value;
                                                lectureGroupItem.ProtectedUrl = protectedUrl;
                                                lectureGroupItem.UnprotectedUrl = UnprotectUrl(cookieJar, protectedUrl);
                                            }
                                            lectureGroup.Items.Add(lectureGroupItem);
                                        }
                                    }
                                    yield return lectureGroup;
                                }

                            }
                        }
                    }
                }
            }
        }

        private static string UnprotectUrl(CookieContainer cookieJar, string protectedUrl)
        {
            using (HttpMessageHandler handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = cookieJar })
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "User-Agent", @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22" },
                        { "Accept", "text/html, application/xhtml+xml, */*" }
                    };
                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri(protectedUrl);
                    request.Method = HttpMethod.Get;
                    foreach (var kvp in headers)
                    {
                        switch (kvp.Key.ToUpperInvariant())
                        {
                            case "HOST":
                                request.Headers.Host = kvp.Value;
                                break;
                            case "REFERER":
                                request.Headers.Referrer = new Uri(kvp.Value);
                                break;
                            default:
                                request.Headers.Add(kvp.Key, kvp.Value);
                                break;
                        }
                    }
                    var response = client.SendAsync(request).Result;
                    return response.Headers.GetValues("Location").First();
                }
            }
        }

        private static string ConstructToken(int length)
        {
            var sb = new StringBuilder();
            const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (var i = 0; i < length; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }
            return sb.ToString();
        }
    }
}
