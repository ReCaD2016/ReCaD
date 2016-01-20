using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Client
{
    class Slide
    {
        public Slide(int time, Content cont)
        {
            this.AppearanceDuration = time;
            this.Content = cont;
        }

        public int AppearanceDuration { get; set; }

        public Content Content { get; set; }

    }
}
