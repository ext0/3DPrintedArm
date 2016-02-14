using ArmWebInterface.Cleverbot;
using ArmWebInterface.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ArmWebInterface.Controller
{
    public static class Controller
    {
        public static Queue<String> needToDo = new Queue<String>();

        [Controller("~/say/")]
        public static String sendCommand(HttpListenerContext context, String text)
        {
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            String cookie = context.Request.Cookies["session"].Value;
            CleverbotConversation activeConversation;
            bool flag = false;
            if (Sessions.active.ContainsKey(cookie))
            {
                activeConversation = Sessions.active[cookie];
            }
            else
            {
                activeConversation = Cleverbot.Cleverbot.buildSession(text);
                flag = true;
                Sessions.active.Add(cookie, activeConversation);
            }
            if (flag)
            {
                needToDo.Enqueue(activeConversation.latestReply.content);
                return activeConversation.latestReply.content;
            }
            else
            {
                needToDo.Enqueue(activeConversation.speak(text).content);
                return activeConversation.speak(text).content;
            }
        }
        [Controller("~/reset/")]
        public static String resetSession(HttpListenerContext context)
        {
            String cookie = context.Request.Cookies["session"].Value;
            if (Sessions.active.ContainsKey(cookie))
            {
                Sessions.active.Remove(cookie);
            }
            return "OK";
        }

        [Controller("~/status/")]
        public static String getStatus(HttpListenerContext context)
        {
            if (needToDo.Count > 0) return needToDo.Dequeue();
            return String.Empty;
        }
    }
}
