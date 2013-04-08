using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UiComponents.Media.General;
using OnlineVideos.Sites;
using MediaPortal.Common.Localization;

namespace OnlineVideos.MediaPortal2
{
	public class SiteSettingViewModel : ListItem
	{
		const string KEY_VALUE = "Value";

		protected AbstractProperty _nameProperty;
		public AbstractProperty NameProperty { get { return _nameProperty; } }
		public string Name
		{
			get { return (string)_nameProperty.GetValue(); }
			set { _nameProperty.SetValue(value); }
		}

		protected AbstractProperty _descriptionProperty;
		public AbstractProperty DescriptionProperty { get { return _descriptionProperty; } }
		public string Description
		{
			get { return (string)_descriptionProperty.GetValue(); }
			set { _descriptionProperty.SetValue(value); }
		}

		protected AbstractProperty _valueProperty;
		public AbstractProperty ValueProperty { get { return _valueProperty; } }
		public string Value
		{
			get { return (string)_valueProperty.GetValue(); }
			set { _valueProperty.SetValue(value); }
		}

		protected AbstractProperty _newValueProperty;
		public AbstractProperty NewValueProperty { get { return _newValueProperty; } }
		public string NewValue
		{
			get { return (string)_newValueProperty.GetValue(); }
			set { _newValueProperty.SetValue(value); }
		}

		protected AbstractProperty _possibleValuesProperty;
		public AbstractProperty PossibleValuesProperty
		{
			get
			{
				if (!_possibleValuesProperty.HasValue()) // create entries upon first use
					_possibleValuesProperty.SetValue(CreatePossibleValuesList());
				return _possibleValuesProperty;
			}
		}
		public ItemsList PossibleValues
		{
			get { return (ItemsList)_possibleValuesProperty.GetValue(); }
			set { _possibleValuesProperty.SetValue(value); }
		}

		ItemsList CreatePossibleValuesList()
		{
			var result = new ItemsList();
			if (PropertyDescriptor.IsBool)
			{
				var item = new ListItem(Consts.KEY_NAME, new StringId("[System.Yes]")) { Selected = Value == true.ToString() };
				item.AdditionalProperties.Add(KEY_VALUE, true.ToString());
				result.Add(item);

				item = new ListItem(Consts.KEY_NAME, new StringId("[System.No]")) { Selected = Value == false.ToString() };
				item.AdditionalProperties.Add(KEY_VALUE, false.ToString());
				result.Add(item);
			}
			else if (PropertyDescriptor.IsEnum)
			{
				foreach (string e in PropertyDescriptor.GetEnumValues())
				{
					var item = new ListItem(Consts.KEY_NAME, e) { Selected = Value == e };
					item.AdditionalProperties.Add(KEY_VALUE, e);
					result.Add(item);
				}
			}
			return result;
		}

        public SiteUtilBase.FieldPropertyDescriptorByRef PropertyDescriptor { get; protected set; }
		public SiteViewModel Site { get; protected set; }

		public SiteSettingViewModel(SiteViewModel site, SiteUtilBase.FieldPropertyDescriptorByRef propertyDescriptor)
			: base(Consts.KEY_NAME, propertyDescriptor.DisplayName)
        {
			Site = site;
			PropertyDescriptor = propertyDescriptor;

			_nameProperty = new WProperty(typeof(string), propertyDescriptor.DisplayName);
			_descriptionProperty = new WProperty(typeof(string), propertyDescriptor.Description);

			string valueAsString = site.Site.GetConfigValueAsString(propertyDescriptor);
			_valueProperty = new WProperty(typeof(string), propertyDescriptor.IsPassword ? new string('*', valueAsString.Length) : valueAsString);
			_newValueProperty = new WProperty(typeof(string), valueAsString);

			_possibleValuesProperty = new WProperty(typeof(ItemsList), null);

			Command = new MethodDelegateCommand(() => Configure());
		}

		void Configure()
		{
			// show different dialog depending on bool, enum or string
			if (PropertyDescriptor.IsBool || PropertyDescriptor.IsEnum)
			{
				ServiceRegistration.Get<IScreenManager>().ShowDialog("ovsDialogSiteSettingChoice");
			}
			else
			{
				NewValue = Site.Site.GetConfigValueAsString(PropertyDescriptor);
				ServiceRegistration.Get<IScreenManager>().ShowDialog("ovsDialogSiteSettingText");
			}
		}

		public void SetNewTextValue()
		{
			Site.Site.SetConfigValueFromString(PropertyDescriptor, NewValue);
			Value = PropertyDescriptor.IsPassword ? new string('*', NewValue.Length) : NewValue;
			Site.UserSettingsChanged = true;
		}

		public void SetNewChoiceValue(ListItem selectedItem)
		{
			NewValue = selectedItem.AdditionalProperties[KEY_VALUE].ToString();
			SetNewTextValue();
			foreach (var item in PossibleValues) item.Selected = item == selectedItem;
		}
	}
}
