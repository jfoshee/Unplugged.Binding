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
        public bool UseViewModelMethods { get; set; }
        readonly Dictionary<string, string> _controlProperties = new Dictionary<string, string> { { "Label", "Text" } };

        public IDisposable Bind(object viewModel, object view)
        {
            return new VvmBinding(viewModel, view, this);
        }

        public void AddControlPropertyConvention(string controlTypeName, string controlPropertyName)
        {
            _controlProperties[controlTypeName] = controlPropertyName;
        }

        sealed class VvmBinding : IDisposable
        {
            readonly BindingFactory _parent;
            readonly object _view;
            readonly INotifyPropertyChanged _viewModel;
            Action<Action> UpdateView { get { return _parent.UpdateView; } }
            Dictionary<string, string> ControlProperties { get { return _parent._controlProperties;  } }
            IEnumerable<string> ControlSuffixes { get { return ControlProperties.Keys; } }
            IEnumerable<string> ControlPropertySuffixes { get { return ControlProperties.Values; } } 
            bool UseViewModelMethods { get { return _parent.UseViewModelMethods; } }

            public VvmBinding(object viewModel, object view, BindingFactory parent)
            {
                _parent = parent;
                _view = view;
                _viewModel = viewModel as INotifyPropertyChanged;
                UpdateValuesFromViewModelProperties(viewModel, view);
                if (_viewModel != null)
                    _viewModel.PropertyChanged += HandlePropertyChanged;
                if (UseViewModelMethods)
                    UpdateValuesFromViewModelMethods(viewModel, view);
            }

            public void Dispose()
            {
                if (_viewModel != null)
                    _viewModel.PropertyChanged -= HandlePropertyChanged;
            }

            void UpdateValuesFromViewModelProperties(object viewModel, object view)
            {
                var vmProperties = GetProperties(viewModel);
                foreach (var vmProperty in vmProperties)
                    UpdateValue(viewModel, vmProperty, view);
            }

            void UpdateValuesFromViewModelMethods(object viewModel, object view)
            {
                var vmMethods = viewModel.GetType().GetMethods()
                                       .Where(m => !m.GetParameters().Any() && m.ReturnType != typeof(void));
                foreach (var vmMethod in vmMethods)
                {
                    var baseName = GetBaseName(vmMethod);
                    var viewProperty = GetViewProperty(baseName);
                    if (viewProperty == null)
                    {
                        Console.WriteLine("Binding warning: No view property for Method: {0}()", vmMethod.Name);
                        continue;
                    }
                    var value = vmMethod.Invoke(viewModel);
                    UpdateValue(vmMethod.ReturnType, vmMethod.Name, value, view, viewProperty);
                }
            }

            void UpdateValue(object viewModel, PropertyInfo vmProperty, object view)
            {
                var viewProperty = GetViewProperty(vmProperty);
                if (viewProperty == null)
                {
                    Console.WriteLine("Binding warning: No view property for Property: {0}", vmProperty.Name);
                    return;
                }
                UpdateValue(viewModel, vmProperty, view, viewProperty);
            }

            void UpdateValue(object viewModel, PropertyInfo vmProperty, object view, PropertyInfo viewProperty)
            {
                var value = vmProperty.GetValue(viewModel);
                UpdateValue(vmProperty.PropertyType, vmProperty.Name, value, view, viewProperty);
            }

            private void UpdateValue(Type vmMemberType, string vmMemberName, object value, object view, PropertyInfo viewProperty)
            {
                if (vmMemberType == viewProperty.PropertyType)
                {
                    Console.WriteLine("Setting: {0} = {1}", viewProperty.Name, value);
                    SetValue(view, viewProperty, value);
                    return;
                }
                var control = viewProperty.GetValue(view);
                if (control == null) return;
                var baseName = GetBaseName(viewProperty);
                var controlPropertyName = "";
                if (ControlProperties.ContainsKey(control.GetType().Name))
                    controlPropertyName = ControlProperties[control.GetType().Name];
                else if (vmMemberName.Length > baseName.Length)
                    controlPropertyName = vmMemberName.Substring(baseName.Length);
                var controlProperty = control.GetType().GetProperty(controlPropertyName);
                if (controlProperty == null) return;
                Console.WriteLine("Setting: {0}.{1} = {2}", viewProperty.Name, controlPropertyName, value);
                SetValue(control, controlProperty, value);
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

            string GetBaseName(MemberInfo member)
            {
                var name = member.Name;
                name = TrimFirstMatchedSuffix(name, ControlPropertySuffixes);
                name = TrimFirstMatchedSuffix(name, ControlSuffixes);
                return name;
            }

            static string TrimFirstMatchedSuffix(string name, IEnumerable<string> suffixes)
            {
                var suffix = suffixes.FirstOrDefault(name.EndsWith);
                return suffix != null ? 
                    name.Substring(0, name.Length - suffix.Length) : 
                    name;
            }

            void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                var vmProperty = sender.GetType().GetProperty(e.PropertyName);
                UpdateValue(sender, vmProperty, _view);
            }

            PropertyInfo GetViewProperty(PropertyInfo vmProperty)
            {
                var baseName = GetBaseName(vmProperty);
                return GetViewProperty(baseName);
            }

            PropertyInfo GetViewProperty(string baseName)
            {
                var matchingProperties = GetProperties(_view).Where(p => baseName == GetBaseName(p));
                return matchingProperties.FirstOrDefault();
            }

            static IEnumerable<PropertyInfo> GetProperties(object obj)
            {
                return obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
        }
    }

    public static class ReflectionExtensionMethods
    {
        static readonly object[] EmptyArray = new object[] { };

        public static object GetValue(this PropertyInfo propertyInfo, object obj)
        {
            return propertyInfo.GetValue(obj, EmptyArray);
        }

        public static void SetValue(this PropertyInfo propertyInfo, object obj, object value)
        {
            var setter = propertyInfo.GetSetMethod();
            if (setter != null)
                propertyInfo.SetValue(obj, value, EmptyArray);
        }

        public static object Invoke(this MethodInfo methodInfo, object obj)
        {
            return methodInfo.Invoke(obj, EmptyArray);
        }
    }
}
