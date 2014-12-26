using System;
using System.ComponentModel;

namespace OnlineVideos.MPUrlSourceFilter.UserSettings
{
    /// <summary>
    /// Generic converter for editing user configurable settings as expandable objects on a siteutil in property grid.
    /// </summary>
    public class ExpandableUserSettingObjectConverter<T> : ExpandableObjectConverter where T : class
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(T))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(System.String) && (value is T))
            {
                context.PropertyDescriptor.SetValue(context.Instance, value);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
