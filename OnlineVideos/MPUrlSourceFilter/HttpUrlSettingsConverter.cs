using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Rrepresents class for editing HTTP url settings in property grid.
    /// </summary>
    public class HttpUrlSettingsConverter : ExpandableObjectConverter
    {
        #region Methods

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(HttpUrlSettings))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(System.String) && (value is HttpUrlSettings))
            {
                context.PropertyDescriptor.SetValue(context.Instance, new HttpUrlSettings(value.ToString()));
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        #endregion
    }
}
