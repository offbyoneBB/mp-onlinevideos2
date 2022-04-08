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
                return FieldPropertyDescriptor.DisplayName;
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
                var pwAttr = FieldPropertyDescriptor.Attributes[typeof(PasswordPropertyTextAttribute)];
                return pwAttr != null && ((PasswordPropertyTextAttribute)pwAttr).Password;
            }
        }

        public bool IsBool { get { return FieldPropertyDescriptor.PropertyType.Equals(typeof(bool)); } }

        public bool IsEnum
        {
            get
            {
                return FieldPropertyDescriptor.PropertyType.IsEnum || 
                    !String.IsNullOrEmpty(((TypeConverterAttribute)FieldPropertyDescriptor.Attributes[typeof(TypeConverterAttribute)]).ConverterTypeName);
            }
        }

        public string Namespace { get { return FieldPropertyDescriptor.PropertyType.Namespace; } }

        public string[] GetEnumValues()
        {
            if (FieldPropertyDescriptor.PropertyType.IsEnum)
                return Enum.GetNames(FieldPropertyDescriptor.PropertyType);
            else
            {
                var res = FieldPropertyDescriptor.Converter.GetStandardValues();
                var resStrings = new string[res.Count];
                res.CopyTo(resStrings, 0);
                return resStrings;
            }
        }

        public override string ToString()
        {
            return FieldPropertyDescriptor.ToString();
        }
    }
}
