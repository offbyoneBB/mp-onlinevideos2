using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos
{
	/// <summary>
	/// This attribute allows setting a DisplayName for a field with optional localization support.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class LocalizableDisplayNameAttribute : System.Attribute
	{
		public static readonly LocalizableDisplayNameAttribute Default = new LocalizableDisplayNameAttribute();

		protected string displayName = null;

		/// <summary>
		/// Instantiate the Attribute class to set a human readable name.
		/// </summary>
		/// <param name="displayName">The name that should be displayed instead of the field name.</param>
		public LocalizableDisplayNameAttribute(string displayName = null)
		{
			this.displayName = displayName;
		}

		/// <summary>
		/// The name of the field of the class <see cref="Translation"/> whose value should be used as DisplayName.
		/// </summary>
		public string TranslationFieldName { get; set; }

		public string LocalizedDisplayName 
		{
			get
			{
				string result = displayName;
				if (!string.IsNullOrEmpty(TranslationFieldName)) result = Translation.Instance.GetByName(TranslationFieldName);
				return result;
			}
		}
	}
}
