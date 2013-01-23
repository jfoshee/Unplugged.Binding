using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Unplugged.Binding
{


    public class BindingFactory
    {
        public Action<Action> UpdateView { get; set; }

        public IDisposable Bind(object viewModel, object view)
        {
            return new VvmBinding(viewModel, view, UpdateView);
        }

        sealed class VvmBinding : IDisposable
        {
            readonly Action<Action> _updateView;
            readonly object _view;
            readonly INotifyPropertyChanged _viewModel;
            readonly string[] _suffices = { "Label", "Text" };

            public VvmBinding(object viewModel, object view, Action<Action> updateView)
            {
                _updateView = updateView;
                _view = view;
                _viewModel = viewModel as INotifyPropertyChanged;
                var viewProperties = GetProperties(view);
                foreach (var viewProperty in viewProperties)
                {
                    var baseName = GetBaseName(viewProperty.Name);
                    var matchingProperties = GetProperties(viewModel).Where(p => p.Name.StartsWith(baseName));
                    var vmProperty = matchingProperties.FirstOrDefault();
                    if (vmProperty == null) continue;
                    UpdateValue(viewModel, vmProperty, view, viewProperty);
                }
                if (_viewModel != null)
                    _viewModel.PropertyChanged += HandlePropertyChanged;
            }

            public void Dispose()
            {
                _viewModel.PropertyChanged -= HandlePropertyChanged;
            }

            void UpdateValue(object viewModel, PropertyInfo vmProperty, object view, PropertyInfo viewProperty)
            {
                var value = vmProperty.GetValue(viewModel);
                var baseName = GetBaseName(viewProperty.Name);
                if (vmProperty.Name.Length > baseName.Length)
                {
                    var controlPropertyName = vmProperty.Name.Substring(baseName.Length);
                    var control = viewProperty.GetValue(view);
                    if (control != null)
                    {
                        var controlProperty = control.GetType().GetProperty(controlPropertyName);
                        SetValue(control, controlProperty, value);
                    }
                }
                else
                    SetValue(view, viewProperty, value);
            }

            void SetValue(object view, PropertyInfo viewProperty, object value)
            {
                if (_updateView == null)
                    viewProperty.SetValue(view, value);
                else
                    _updateView(() => viewProperty.SetValue(view, value));
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
                var matchingProperties = GetProperties(_view).Where(p => baseName == GetBaseName(p.Name));
                var viewProperty = matchingProperties.FirstOrDefault();
                if (viewProperty != null)
                    UpdateValue(sender, vmProperty, _view, viewProperty);
            }

            static IEnumerable<PropertyInfo> GetProperties(object obj)
            {
                return obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
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
            if (propertyInfo == null)
                return;
            var setter = propertyInfo.GetSetMethod();
            if (setter != null)
                propertyInfo.SetValue(obj, value, new object[]{});
        }
    }
}
