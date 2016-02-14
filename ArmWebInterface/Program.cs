using ArmWebInterface.Cleverbot;
using ArmWebInterface.Web;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ArmWebInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            WebServer ws = new WebServer(SendResponse, "http://localhost:8080/sign/");
            ws.run();
            Console.WriteLine("Webserver running!");
            Console.ReadKey();
            ws.stop();
        }
        public static string SendResponse(HttpListenerRequest request)
        {
            List<HttpArgument> arguments = new List<HttpArgument>();
            MethodInfo[] info = typeof(Controller.Controller).GetMethods().Where((x) => (x.GetCustomAttribute<ControllerAttribute>() != null)).ToArray();
            foreach (String key in request.QueryString.AllKeys)
            {
                arguments.Add(new HttpArgument(key, request.QueryString.GetValues(key).First()));
            }
            Object returned = String.Empty;
            foreach (HttpArgument argument in arguments)
            {
                foreach (MethodInfo method in info)
                {
                    if (method.GetCustomAttribute<ControllerAttribute>().key.Equals(argument.key))
                    {
                        try
                        {
                            returned = method.Invoke(null, new object[] { request, argument });
                        }
                        catch (Exception e)
                        {
                            returned = "An error occured processing your request! [" + e.Message + "]";
                        }
                        break;
                    }
                }
            }
            return returned.ToString();
        }
    }
    public class HttpArgument
    {
        public readonly String key;
        public readonly String value;
        public HttpArgument(String key, String value)
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
