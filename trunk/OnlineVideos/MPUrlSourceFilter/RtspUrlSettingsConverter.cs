using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represents class for editing RTSP url settings in property grid.
    /// </summary>
    public class RtspUrlSettingsConverter : ExpandableObjectConverter
    {
        #region Methods

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(RtspUrlSettings))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(System.String) && (value is RtspUrlSettings))
            {
                context.PropertyDescriptor.SetValue(context.Instance, new RtspUrlSettings(value.ToString()));
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        #endregion
    }

}
