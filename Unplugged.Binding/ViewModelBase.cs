using System;
using System.ComponentModel;

namespace Unplugged.Binding
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void Notify(params string[] propertyNames)
        {
            if (PropertyChanged != null)
                foreach (var propertyName in propertyNames)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

