using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ArmWebInterface.Cleverbot
{
    public static class Cleverbot
    {
        public static String userAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.109 Safari/537.36";
        public static String buildBody(HttpArgument[] args)
        {
            return String.Join("&", args.Select((x) => (x.ToString())));
        }
        public static String buildCookies(HttpCookie[] args)
        {
            return String.Join("; ", args.Select((x) => (x.ToString())));
        }
        public static CleverbotConversation buildSession(String stimulus)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.cleverbot.com/");

            request.KeepAlive = true;
            request.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0");
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.Headers.Add("Upgrade-Insecure-Requests", @"1");
            request.UserAgent = userAgent;
            request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, sdch");
            request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            String value = response.Headers["Set-Cookie"];
            return startConversation(stimulus, new HttpCookie[] { new HttpCookie(value.Substring(0, value.IndexOf("=")), value.Substring(value.IndexOf("=") + 1, value.IndexOf(";") - value.IndexOf("=") - 1)) });
        }

        public static CleverbotConversation startConversation(String stimulus, HttpCookie[] cookies)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.cleverbot.com/webservicemin?");

            request.KeepAlive = true;
            request.Headers.Add("Origin", @"http://www.cleverbot.com");
            request.UserAgent = userAgent;
            request.ContentType = "text/plain;charset=UTF-8";
            request.Accept = "*/*";
            request.Referer = "http://www.cleverbot.com/";
            request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
            request.Headers.Set(HttpRequestHeader.Cookie, buildCookies(cookies));

            request.Method = "POST";
            request.ServicePoint.Expect100Continue = false;

            string body = @"stimulus=" + stimulus + "%21&cb_settings_language=en&cb_settings_scripting=no&islearning=1&icognoid=wsf&icognocheck=";
            body = body + md5(body.Substring(9, 35 - 9));
            byte[] postBytes = Encoding.UTF8.GetBytes(body);
            request.ContentLength = postBytes.Length;
            Stream stream = request.GetRequestStream();
            stream.Write(postBytes, 0, postBytes.Length);
            stream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            String text = readResponse(response).Trim();
            String reply = text.Substring(0, text.IndexOf("\r"));
            text = text.Substring(text.IndexOf("\r") + 1);
            String identifier = text.Substring(0, text.IndexOf("\r"));
            CookieContainer container = new CookieContainer();
            String setCookies = response.Headers["Set-Cookie"];
            String[] split = setCookies.Split(',');
            String cleverRef = String.Empty;
            String cleverRefFirst = String.Empty;
            String XAI = String.Empty;
            foreach (String s in split)
            {
                int equal = s.IndexOf("=");
                if (equal == -1) continue;
                String key = s.Substring(0, equal);
                String value = s.Substring(equal + 1, s.IndexOf(";") - s.IndexOf("=") - 1);
                if (key.Equals("cleverbotref"))
                {
                    cleverRef = value;
                }
                else if (key.Equals("cleverbotfirstref"))
                {
                    cleverRefFirst = value;
                }
                else if (key.Equals("XAI"))
                {
                    XAI = value;
                }
            }
            return new CleverbotConversation
            {
                dialogue = new List<CleverbotMessage> { new CleverbotMessage(Sender.USER, stimulus), new CleverbotMessage(Sender.CLEVERBOT, reply) },
                identifier = identifier,
                cleverRef = cleverRef,
                cleverRefFirst = cleverRefFirst,
                XAI = XAI,
                XVISString = buildCookies(cookies)
            };
        }

        public static string md5(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static string readResponse(HttpWebResponse response)
        {
            using (Stream responseStream = response.GetResponseStream())
            {
                Stream streamToRead = responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                {
                    streamToRead = new GZipStream(streamToRead, CompressionMode.Decompress);
                }
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                {
                    streamToRead = new DeflateStream(streamToRead, CompressionMode.Decompress);
                }

                using (StreamReader streamReader = new StreamReader(streamToRead, Encoding.UTF8))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }

    public enum Sender
    {
        CLEVERBOT,
        USER
    }

    public class CleverbotMessage
    {
        public Sender sender { get; }
        public String content { get; }
        public CleverbotMessage(Sender sender, String content)
        {
            this.sender = sender;
            this.content = content;
        }
        public override string ToString()
        {
            return sender + ":" + content;
        }
    }

    public class CleverbotConversation
    {
        public List<CleverbotMessage> dialogue { get; set; }
        public String identifier { get; set; }
        public String cleverRef { get; set; }
        public String cleverRefFirst { get; set; }
        public String XAI { get; set; }
        public String XVISString { get; set; }
        public CleverbotMessage latestReply
        {
            get
            {
                return dialogue[dialogue.Count - 1];
            }
        }
        public CleverbotMessage speak(String message)
        {
            String lastMessage = HttpUtility.HtmlEncode(latestReply.content);
            String newMessage = HttpUtility.HtmlEncode(message);
            dialogue.Add(new CleverbotMessage(Sender.USER, message));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.cleverbot.com/webservicemin?out=" + lastMessage + "+&in=" + newMessage + "&bot=c&cbsid=" + identifier + "&xai=" + XAI + "&ns=4&al=&dl=en&flag=&user=&mode=1&");

            request.KeepAlive = true;
            request.Headers.Add("Origin", @"http://www.cleverbot.com");
            request.UserAgent = Cleverbot.userAgent;
            request.ContentType = "text/plain;charset=UTF-8";
            request.Accept = "*/*";
            request.Referer = "http://www.cleverbot.com/";
            request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
            request.Headers.Set(HttpRequestHeader.Cookie, @"" + XVISString + " cleverbotref=" + cleverRef + "; cleverbotfirstref2=" + cleverRefFirst + "; XAI=" + XAI + ";");

            request.Method = "POST";
            request.ServicePoint.Expect100Continue = false;

            String buildString = String.Empty;
            for (int i = dialogue.Count - 1; i > 0; i--)
            {
                buildString = "&vText" + (i + 2) + "=" + HttpUtility.HtmlEncode(dialogue[i].content) + buildString;
            }
            string body = @"stimulus=" + newMessage + buildString + "&sessionid=" + identifier + "&cb_settings_language=en&cb_settings_scripting=no&islearning=1&icognoid=wsf&icognocheck=";
            body = body + Cleverbot.md5(body.Substring(9, 35 - 9));
            byte[] postBytes = Encoding.UTF8.GetBytes(body);
            request.ContentLength = postBytes.Length;
            Stream stream = request.GetRequestStream();
            stream.Write(postBytes, 0, postBytes.Length);
            stream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            String text = Cleverbot.readResponse(response).Trim();
            CleverbotMessage reply = new CleverbotMessage(Sender.CLEVERBOT, text.Substring(0, text.IndexOf("\r")));
            dialogue.Add(reply);
            return reply;
        }
    }
    public class HttpCookie
    {
        public String key;
        public String value;
        public HttpCookie(String key, String value)
        {
            this.key = key;
            this.value = value;
        }
        public override string ToString()
        {
            return String.Format("{0}={1}", key, value);
        }
    }
}
