using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Configuration
    {
        public Configuration(List<Slide> dia)
        {
            this.Diashow = dia;
        }
        public List<Slide> Diashow { get; set; }
    }
}
