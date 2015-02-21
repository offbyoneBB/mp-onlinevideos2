using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using OnlineVideos.Reflection;

namespace OnlineVideos
{
    /// <summary>
    /// Abstract base class for all classes with fields that are attributed with the <see cref="CategoryAttribute"/> 
    /// with category <see cref="ONLINEVIDEOS_USERCONFIGURATION_CATEGORY"/>.
    /// </summary>
    public abstract class UserConfigurable : MarshalByRefObject, ICustomTypeDescriptor
    {
        public const string ONLINEVIDEOS_USERCONFIGURATION_CATEGORY = "OnlineVideosUserConfiguration";

        #region MarshalByRefObject overrides
        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }
        #endregion

        #region ICustomTypeDescriptor Implementation with Fields as Properties

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(null);
        }

        private PropertyDescriptorCollection cachedPropertyDescriptors;
        private FilterCache cachedFilter;

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            bool filtering = (attributes != null && attributes.Length > 0);
            PropertyDescriptorCollection props = cachedPropertyDescriptors;
            FilterCache cache = cachedFilter;

            // Use a cached version if possible
            if (filtering && cache != null && cache.IsValid(attributes))
                return cache.FilteredProperties;
            else if (!filtering && props != null)
                return props;

            // Create the property collection and filter
            props = new PropertyDescriptorCollection(null);
            foreach (PropertyDescriptor prop in
                TypeDescriptor.GetProperties(
                this, attributes, true))
            {
                props.Add(prop);
            }
            foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                FieldPropertyDescriptor fieldDesc = new FieldPropertyDescriptor(field);
                if (!filtering || fieldDesc.Attributes.Contains(attributes))
                    props.Add(fieldDesc);
            }

            // Store the computed properties
            if (filtering)
            {
                cache = new FilterCache();
                cache.Attributes = attributes;
                cache.FilteredProperties = props;
                cachedFilter = cache;
            }
            else cachedPropertyDescriptors = props;

            return props;
        }

        #endregion

        internal abstract string GetConfigurationKey(string fieldName);

        /// <summary>
        /// Sets the user configurable value from the <see cref="OnlineVideoSettings.UserStore "/> to the given field
        /// when it is attributed with the <see cref="CategoryAttribute"/> and the <see cref="ONLINEVIDEOS_USERCONFIGURATION_CATEGORY"/>.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="categoryAttribute"></param>
        protected virtual void SetUserConfigurationValue(FieldInfo field, CategoryAttribute categoryAttribute)
        {
            if (categoryAttribute != null &&
                categoryAttribute.Category == ONLINEVIDEOS_USERCONFIGURATION_CATEGORY &&
                OnlineVideoSettings.Instance.UserStore != null)
            {
                try
                {
                    // values marked as password must be decrypted
                    bool decrypt = false;
                    object[] attrs = field.GetCustomAttributes(typeof(PasswordPropertyTextAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                        decrypt = ((PasswordPropertyTextAttribute)attrs[0]).Password;

                    string value = OnlineVideoSettings.Instance.UserStore.GetValue(GetConfigurationKey(field.Name), decrypt);
                    if (value != null)
                    {
                        if (field.FieldType.IsEnum)
                        {
                            field.SetValue(this, Enum.Parse(field.FieldType, value));
                        }
                        else
                        {
                            field.SetValue(this, Convert.ChangeType(value, field.FieldType));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("{0} - could not set User Configuration Value: {1}. Error: {2}", ToString(), field.Name, ex.Message);
                }
            }
        }

        public List<FieldPropertyDescriptorByRef> GetUserConfigurationProperties()
        {
            List<FieldPropertyDescriptorByRef> result = new List<FieldPropertyDescriptorByRef>();
            CategoryAttribute attr = new CategoryAttribute(ONLINEVIDEOS_USERCONFIGURATION_CATEGORY);
            var props = ((ICustomTypeDescriptor)this).GetProperties(new Attribute[] { attr });
            foreach (PropertyDescriptor prop in props) if (prop.Attributes.Contains(attr) && prop is FieldPropertyDescriptor) result.Add(new FieldPropertyDescriptorByRef() { FieldPropertyDescriptor = prop as FieldPropertyDescriptor });
            return result;
        }

        public string GetConfigValueAsString(FieldPropertyDescriptorByRef config)
        {
            object result = config.FieldPropertyDescriptor.GetValue(this);
            return result == null ? string.Empty : result.ToString();
        }

        public void SetConfigValueFromString(FieldPropertyDescriptorByRef config, string value)
        {
            object valueConverted = null;
            if (config.FieldPropertyDescriptor.PropertyType.IsEnum) valueConverted = Enum.Parse(config.FieldPropertyDescriptor.PropertyType, value);
            else valueConverted = Convert.ChangeType(value, config.FieldPropertyDescriptor.PropertyType);
            config.FieldPropertyDescriptor.SetValue(this, valueConverted);
        }
    }
}
