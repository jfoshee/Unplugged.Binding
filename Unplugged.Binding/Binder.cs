using System;
using System.Linq;
using System.ComponentModel;
using System.Reflection;

namespace Unplugged.Binding
{
    public class Binder : IDisposable
    {
        object _view;
        INotifyPropertyChanged _viewModel;
        readonly string[] _suffices = { "Label", "Text" };

        public Action<Action> UpdateView { get; set; }

        public void Bind(object viewModel, object view)
        {
            _view = view;

            var viewProperties = view.GetType().GetProperties();
            foreach (var viewProperty in viewProperties)
            {
                var baseName = GetBaseName(viewProperty.Name);
                var matchingProperties = viewModel.GetType().GetProperties().Where(p => p.Name.StartsWith(baseName));
                var vmProperty = matchingProperties.FirstOrDefault();
                if (vmProperty == null) continue;

                UpdateValue(viewModel, vmProperty, view, viewProperty);
            }
            _viewModel = viewModel as INotifyPropertyChanged;
            if (_viewModel != null)
                _viewModel.PropertyChanged += HandlePropertyChanged;
        }

        void UpdateValue(object viewModel, PropertyInfo vmProperty, object view, PropertyInfo viewProperty)
        {
            var value = vmProperty.GetValue(viewModel);
            var baseName = GetBaseName(viewProperty.Name);
            var controlPropertyName = vmProperty.Name.Substring(baseName.Length);
            var control = viewProperty.GetValue(view);
            if (control != null)
            {
                var controlProperty = control.GetType().GetProperty(controlPropertyName);
//                if (controlProperty != null)
                SetValue(control, controlProperty, value);
            }
            else
            {
                SetValue(view, viewProperty, value);
            }
        }

        void SetValue(object view, PropertyInfo viewProperty, object value)
        {
            if (UpdateView == null)
                viewProperty.SetValue(view, value);
            else
                UpdateView(() => viewProperty.SetValue(view, value));
        }

        string GetBaseName(string name)
        {
            foreach (var suffix in _suffices.Where(name.EndsWith))
                return name.Substring(0, name.Length - suffix.Length);
            return name;
        }

        void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vmProperty = sender.GetType().GetProperty(e.PropertyName);
            var baseName = GetBaseName(vmProperty.Name);
            var matchingProperties = _view.GetType().GetProperties().Where(p => baseName == GetBaseName(p.Name));
            var viewProperty = matchingProperties.First();
            UpdateValue(sender, sender.GetType().GetProperty(e.PropertyName), _view, viewProperty);
        }

        public void Dispose()
        {
            _viewModel.PropertyChanged -= HandlePropertyChanged;
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

