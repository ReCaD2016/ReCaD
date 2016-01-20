using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Webpage : Content
    {
        public Webpage(string url)
        {
            this.URL = url;
        }
        public string URL { get; set; }
    }
}
