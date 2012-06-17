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
            return Application.Current.Resources[item.GetType().Name] as DataTemplate;            
        }
    }
}
