using ArmWebInterface.Cleverbot;
using ArmWebInterface.Web;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ArmWebInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            WebServer ws = new WebServer(handleResponse, "http://+:8080/");
            ws.run();
            Console.WriteLine("Webserver running!");
            Console.ReadKey();
            ws.stop();
        }
        public static string handleResponse(HttpListenerContext context)
        {
            if (context.Response.Cookies.Count == 0)
            {
                context.Response.AddHeader("Set-Cookie", "session=" + randomString(32));
            }
            List<HttpArgument> arguments = new List<HttpArgument>();
            MethodInfo[] info = typeof(Controller.Controller).GetMethods().Where((x) => (x.GetCustomAttribute<ControllerAttribute>() != null)).ToArray();
            foreach (String key in context.Request.QueryString.AllKeys)
            {
                arguments.Add(new HttpArgument(key, context.Request.QueryString.GetValues(key).First()));
            }
            Object returned = String.Empty;
            bool flag = false;
            foreach (MethodInfo method in info)
            {
                ControllerAttribute attribute = method.GetCustomAttribute<ControllerAttribute>();
                String trimmed = context.Request.RawUrl.Substring(0, context.Request.RawUrl.LastIndexOf("/") + 1);
                if ((attribute.path.Substring(1).Equals(trimmed)))
                {
                    try
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        Dictionary<ParameterInfo, String> match = new Dictionary<ParameterInfo, String>();
                        foreach (ParameterInfo parameter in parameters)
                        {
                            foreach (HttpArgument argument in arguments)
                            {
                                if (argument.key.Equals(parameter.Name))
                                {
                                    match[parameter] = argument.value;
                                }
                            }
                        }
                        object[] finalized = new object[parameters.Length];
                        finalized[0] = context;
                        for (int i = 1; i < parameters.Length; i++)
                        {
                            finalized[i] = (match.ContainsKey(parameters[i])) ? match[parameters[i]] : null;
                        }
                        returned = method.Invoke(null, finalized);
                    }
                    catch (Exception e)
                    {
                        if (e is TargetParameterCountException)
                        {
                            returned = "An error occured due to a malformed request! Please verify parameters.";
                        }
                        else
                        {
                            returned = "An unknown error occured processing your request! [" + e.Message + "]";
                        }
                    }
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                String request = "ArmWebInterface.View." + context.Request.RawUrl.Substring(1).Replace("/", ".");
                if (request.Equals("ArmWebInterface.View."))
                {
                    request = "ArmWebInterface.View.Home.html";
                }
                bool exists = assembly.GetManifestResourceInfo(request) != null;
                if (!exists)
                {
                    context.Response.StatusCode = 404;
                    returned = fetchFromResource(assembly, "ArmWebInterface.View._Situational.404.html");
                }
                else
                {
                    return fetchFromResource(assembly, request);
                }
            }
            return returned.ToString();
        }
        public static String fetchFromResource(Assembly assembly, String resourceName)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        public static string randomString(int maxSize)
        {
            char[] chars = new char[62];
            chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
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
