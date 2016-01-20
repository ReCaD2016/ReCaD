using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class ScreenCapture : Content
    {
        public ScreenCapture(string ip, string processname)
        {
            this.IPTargetAgent = ip;
            this.ProcessName = processname;
        }
        public string IPTargetAgent { get; set; }

        public string ProcessName { get; set; }
    }
}
