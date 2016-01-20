using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Configurator
{
    public class SecondVM : INotifyPropertyChanged
    {
        Second s;
        int originalValue;

        public SecondVM(Second s)
        {
            this.s = s;
            this.originalValue = s.Value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int Value
        {
            get
            {
                return this.s.Value;
            }
            set
            {
                this.s.Value= value;
                this.Notify();
            }
        }

        public void Notify([CallerMemberName]string propertyname = null)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }
    }
}
