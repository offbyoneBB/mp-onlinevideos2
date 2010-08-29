using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Standalone
{
    public class OVSTemplateSelector : DataTemplateSelector
    {    
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Type type = item.GetType();
            if (type.IsGenericType) type = type.GetGenericArguments().Last();
            while (type.BaseType != typeof(object)) type = type.BaseType;
            return Application.Current.Resources[type.Name] as DataTemplate;            
        }
    }
}
