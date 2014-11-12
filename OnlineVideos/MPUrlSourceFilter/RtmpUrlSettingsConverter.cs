using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Rrepresents class for editing RTMP url settings in property grid.
    /// </summary>
    public class RtmpUrlSettingsConverter : ExpandableObjectConverter
    {
        #region Methods

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(RtmpUrlSettings))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(System.String) && (value is RtmpUrlSettings))
            {
                context.PropertyDescriptor.SetValue(context.Instance, new RtmpUrlSettings(value.ToString()));
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        #endregion
    }
}
