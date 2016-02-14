using ArmWebInterface.Cleverbot;
using ArmWebInterface.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ArmWebInterface.Controller
{
    public static class Controller
    {
        [Controller("say")]
        public static String sendCommand(HttpListenerRequest request, HttpArgument argument)
        {
            String cookie = request.Cookies["session"].Value;
            CleverbotConversation activeConversation;
            bool flag = false;
            if (Sessions.active.ContainsKey(cookie))
            {
                activeConversation = Sessions.active[cookie];
            }
            else
            {
                activeConversation = Cleverbot.Cleverbot.buildSession(argument.value);
                flag = true;
                Sessions.active.Add(cookie, activeConversation);
            }
            if (flag)
            {
                return activeConversation.latestReply.content;
            }
            else
            {
                return activeConversation.speak(argument.value).content;
            }
        }
        [Controller("reset")]
        public static String resetSession(HttpListenerRequest request, HttpArgument argument)
        {
            String cookie = request.Cookies["session"].Value;
            if (Sessions.active.ContainsKey(cookie))
            {
                Sessions.active.Remove(cookie);
            }
            return "OK";
        }
    }
}
