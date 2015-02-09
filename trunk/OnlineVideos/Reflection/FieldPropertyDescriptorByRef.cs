using System;
using System.ComponentModel;

namespace OnlineVideos.Reflection
{
    public class FieldPropertyDescriptorByRef : MarshalByRefObject
    {
        internal FieldPropertyDescriptor FieldPropertyDescriptor { get; set; }

        public string DisplayName
        {
            get
            {
                var attr = FieldPropertyDescriptor.Attributes[typeof(LocalizableDisplayNameAttribute)];
                if (attr != null && ((LocalizableDisplayNameAttribute)attr).LocalizedDisplayName != null) return ((LocalizableDisplayNameAttribute)attr).LocalizedDisplayName;
                else return FieldPropertyDescriptor.DisplayName;
            }
        }

        public string Description
        {
            get
            {

                var descAttr = FieldPropertyDescriptor.Attributes[typeof(DescriptionAttribute)];
                return descAttr != null ? ((DescriptionAttribute)descAttr).Description : string.Empty;
            }
        }

        public bool IsPassword
        {
            get
            {
                return FieldPropertyDescriptor.Attributes.Contains(new System.ComponentModel.PasswordPropertyTextAttribute(true));
            }
        }

        public bool IsBool { get { return FieldPropertyDescriptor.PropertyType.Equals(typeof(bool)); } }

        public bool IsEnum { get { return FieldPropertyDescriptor.PropertyType.IsEnum; } }

        public string Namespace { get { return FieldPropertyDescriptor.PropertyType.Namespace; } }

        public string[] GetEnumValues()
        {
            return Enum.GetNames(FieldPropertyDescriptor.PropertyType);
        }
    }
}
