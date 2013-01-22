using System;
using System.Linq;
using System.ComponentModel;
using System.Reflection;

namespace Unplugged.Binding
{
    public class Binder
    {
        object _view;

        void UpdateValue(object viewModel, PropertyInfo vmProperty, object view, PropertyInfo viewProperty)
        {
            var value = vmProperty.GetValue(viewModel);
            var controlPropertyName = vmProperty.Name.Substring(viewProperty.Name.Length);
            var control = viewProperty.GetValue(view);
            var controlProperty = control.GetType().GetProperty(controlPropertyName);
            controlProperty.SetValue(control, value);
        }

        public void Bind(object viewModel, object view)
        {
            _view = view;

            var viewProperties = view.GetType().GetProperties();
            foreach (var viewProperty in viewProperties)
            {
                var name = viewProperty.Name;
                var matchingProperties = viewModel.GetType().GetProperties().Where(p => p.Name.StartsWith(name));
                var vmProperty = matchingProperties.FirstOrDefault();
                if (vmProperty == null) continue;

                UpdateValue(viewModel, vmProperty, view, viewProperty);
            }
            if (viewModel is INotifyPropertyChanged)
                ((INotifyPropertyChanged)viewModel).PropertyChanged += HandlePropertyChanged;
        }
        
        void HandlePropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            var vmProperty = sender.GetType().GetProperty(e.PropertyName);
            var matchingProperties = _view.GetType().GetProperties().Where(p => vmProperty.Name.StartsWith(p.Name));
            var viewProperty = matchingProperties.First();
            UpdateValue(sender, sender.GetType().GetProperty(e.PropertyName), _view, viewProperty);
        }
    }

    public static class ReflectionExtensionMethods
    {
        public static object GetValue(this PropertyInfo propertyInfo, object obj)
        {
            return propertyInfo.GetValue(obj, new object[]{});
        }

        public static void SetValue(this PropertyInfo propertyInfo, object obj, object value)
        {
            propertyInfo.SetValue(obj, value, new object[]{});
        }
    }
}

