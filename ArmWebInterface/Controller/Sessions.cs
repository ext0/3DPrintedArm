using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArmWebInterface.Cleverbot;

namespace ArmWebInterface.Controller
{
    public static class Sessions
    {
        public static Dictionary<String, CleverbotConversation> active = new Dictionary<String, CleverbotConversation>();
    }
}
