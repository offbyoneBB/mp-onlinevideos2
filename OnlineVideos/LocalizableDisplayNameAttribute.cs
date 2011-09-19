using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos
{
	[AttributeUsage(AttributeTargets.Field)]
	public class LocalizableDisplayNameAttribute : System.Attribute
	{
		public static readonly LocalizableDisplayNameAttribute Default = new LocalizableDisplayNameAttribute();

		string setValue = null;
		public LocalizableDisplayNameAttribute(string localizableDisplayName = null)
		{
			setValue = localizableDisplayName;
		}

		public string LocalizableDisplayName 
		{
			get
			{
				if (!string.IsNullOrEmpty(setValue) && setValue.StartsWith("#"))
				{
					string translated = null;
					if (Translation.Strings.TryGetValue(setValue.Substring(1), out translated)) return translated;
				}
				return setValue;
			}
		}
	}
}
