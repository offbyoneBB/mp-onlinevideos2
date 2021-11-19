using System;
using System.ComponentModel;
using System.Reflection;

namespace OnlineVideos.Reflection
{
    public class FieldPropertyDescriptor : PropertyDescriptor
    {
        private readonly FieldInfo _fieldInfo;

        public FieldPropertyDescriptor(FieldInfo field)
            : base(field.Name, (Attribute[])field.GetCustomAttributes(typeof(Attribute), true))
        {
            _fieldInfo = field;
        }

        public override bool Equals(object obj)
        {
            FieldPropertyDescriptor other = obj as FieldPropertyDescriptor;
            return other != null && other._fieldInfo.Equals(_fieldInfo);
        }

        public override string DisplayName
        {
            get
            {
                var attr = _fieldInfo.GetCustomAttributes(typeof(LocalizableDisplayNameAttribute), false);
                return attr.Length > 0 ? ((LocalizableDisplayNameAttribute)attr[0]).LocalizedDisplayName : base.DisplayName;
            }
        }

        public override string ToString()
        {
            return Name+":"+PropertyType.Name;
        }

        public override int GetHashCode() { return _fieldInfo.GetHashCode(); }

        public override bool IsReadOnly { get { return false; } }

        public override void ResetValue(object component) { }

        public override bool CanResetValue(object component) { return false; }

        public override bool ShouldSerializeValue(object component) { return true; }

        public override Type ComponentType { get { return _fieldInfo.DeclaringType; } }

        public override Type PropertyType { get { return _fieldInfo.FieldType; } }

        public override object GetValue(object component)
        {
            return _fieldInfo.GetValue(component);
        }

        public override void SetValue(object component, object value)
        {
            _fieldInfo.SetValue(component, value);
            OnValueChanged(component, EventArgs.Empty);

            // if this field is marked as user config, set the value also in the OnlineVideo UserStore
            object[] attrs = _fieldInfo.GetCustomAttributes(typeof(CategoryAttribute), false);
            if (attrs.Length > 0 && ((CategoryAttribute)attrs[0]).Category == UserConfigurable.ONLINEVIDEOS_USERCONFIGURATION_CATEGORY)
            {
                // values marked as password must be encrypted
                bool encrypt = false;
                attrs = _fieldInfo.GetCustomAttributes(typeof(PasswordPropertyTextAttribute), false);
                if (attrs != null && attrs.Length > 0)
                    encrypt = ((PasswordPropertyTextAttribute)attrs[0]).Password;

                string key = ((UserConfigurable)component).GetConfigurationKey(_fieldInfo.Name);
                OnlineVideoSettings.Instance.UserStore.SetValue(key, value.ToString(), encrypt);
            }
        }
    }
}
