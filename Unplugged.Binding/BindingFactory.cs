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
        readonly Dictionary<string, string> _defaultProperties = new Dictionary<string, string> { { "Label", "Text" }};

        public IDisposable Bind(object viewModel, object view)
        {
            return new VvmBinding(viewModel, view, this);
        }

        public void DefaultControlProperty(string controlTypeName, string controlPropertyName)
        {
            _defaultProperties[controlTypeName] = controlPropertyName;
        }

        sealed class VvmBinding : IDisposable
        {
            readonly BindingFactory _parent;
            readonly object _view;
            readonly INotifyPropertyChanged _viewModel;
            Action<Action> UpdateView { get { return _parent.UpdateView; } }
            Dictionary<string, string> DefaultProperties { get { return _parent._defaultProperties;  } }

            public VvmBinding(object viewModel, object view, BindingFactory parent)
            {
                _parent = parent;
                _view = view;
                _viewModel = viewModel as INotifyPropertyChanged;
                var vmProperties = GetProperties(viewModel);
                foreach (var vmProperty in vmProperties)
                {
                    UpdateValue(viewModel, vmProperty, view);
                }
                if (_viewModel != null)
                    _viewModel.PropertyChanged += HandlePropertyChanged;
            }

            public void Dispose()
            {
                if (_viewModel != null)
                    _viewModel.PropertyChanged -= HandlePropertyChanged;
            }

            void UpdateValue(object viewModel, PropertyInfo vmProperty, object view)
            {
                var viewProperty = GetViewProperty(vmProperty);
                if (viewProperty == null) return;
                UpdateValue(viewModel, vmProperty, view, viewProperty);
            }

            void UpdateValue(object viewModel, PropertyInfo vmProperty, object view, PropertyInfo viewProperty)
            {
                var value = vmProperty.GetValue(viewModel);
                if (vmProperty.PropertyType != viewProperty.PropertyType)
                {
                    var control = viewProperty.GetValue(view);
                    if (control != null)
                    {
                        var baseName = GetBaseName(viewProperty.Name);
                        var controlPropertyName = "";
                        if (DefaultProperties.ContainsKey(control.GetType().Name))
                            controlPropertyName = DefaultProperties[control.GetType().Name];
                        else if (vmProperty.Name.Length > baseName.Length)
                            controlPropertyName = vmProperty.Name.Substring(baseName.Length);
                        var controlProperty = control.GetType().GetProperty(controlPropertyName);
                        if (controlProperty != null)
                        {
                            Console.WriteLine("Setting: {0}.{1} = {2}", viewProperty.Name, controlPropertyName, value);
                            SetValue(control, controlProperty, value);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Setting: {0} = {1}", viewProperty.Name, value);
                    SetValue(view, viewProperty, value);
                }
            }

            void SetValue(object view, PropertyInfo viewProperty, object value)
            {
                if (viewProperty == null)
                    return;
                if (value != null && viewProperty.PropertyType == typeof (string) && value.GetType() != typeof (string))
                    value = value.ToString();
                if (UpdateView == null)
                    viewProperty.SetValue(view, value);
                else
                    UpdateView(() => viewProperty.SetValue(view, value));
            }

            string GetBaseName(string name)
            {
                var suffixes = DefaultProperties.Keys.Concat(DefaultProperties.Values);
                foreach (var suffix in suffixes.Where(name.EndsWith))
                    name = name.Substring(0, name.Length - suffix.Length);
                return name;
            }

            void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                var vmProperty = sender.GetType().GetProperty(e.PropertyName);
                UpdateValue(sender, vmProperty, _view);
            }

            PropertyInfo GetViewProperty(PropertyInfo vmProperty)
            {
                var baseName = GetBaseName(vmProperty.Name);
                var matchingProperties = GetProperties(_view).Where(p => baseName == GetBaseName(p.Name));
                var viewProperty = matchingProperties.FirstOrDefault();
                return viewProperty;
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
            var setter = propertyInfo.GetSetMethod();
            if (setter != null)
                propertyInfo.SetValue(obj, value, new object[]{});
        }
    }
}
