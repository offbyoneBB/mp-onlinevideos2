using System;
using System.Drawing;
using MediaPortal.Common.Localization;
using MediaPortal.UI.SkinEngine;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using OnlineVideos.Sites;

namespace OnlineVideos.MediaPortal2
{
    public class SiteUtilIconConverter : IValueConverter
    {
        public bool Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
        {
            result = null;

			var siteUtil = value as SiteUtilBase;
			var onlineSite = value as OnlineSiteViewModel;
			string siteName = siteUtil != null ? siteUtil.Settings.Name : onlineSite != null ? onlineSite.Site.Name : null;
			if (!string.IsNullOrEmpty(siteName))
            {
                string subDir = string.IsNullOrEmpty(parameter as string) ? "Icons" : parameter as string;
                // use Icon with the same name as the Site
				string image = System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, subDir + @"\" + siteName + ".png");
                if (System.IO.File.Exists(image)) result = image;
                else if (siteUtil != null)
                {
                    // if that does not exist, try icon with the same name as the Util
					image = System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, subDir + @"\" + siteUtil.Settings.UtilName + ".png");
					if (System.IO.File.Exists(image)) result = image;
					else
					{
						if (siteUtil.Settings.UtilName == "DownloadedVideo")
						{
							result = "DownloadedVideo.png";
						}
					}
                }
            }
			return true;
        }

        public bool ConvertBack(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
        {
            throw new NotImplementedException();
        }
    }

    public class LanguageIconConverter : IValueConverter
    {
        public bool Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
        {
            result = null;
            if (value == null) return false;
            string lang = value.ToString();
            string filename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"Skin\default\images\LanguageFlags\" + lang + ".png");
            if (System.IO.File.Exists(filename))
            {
                result = filename;
                return true;
            }
            return false;
        }

        public bool ConvertBack(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
        {
            throw new NotImplementedException();
        }
    }

	public class SiteStateColorConverter : IValueConverter
	{
		public bool Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
		{
			result = null;
			if (value == null) return false;
			if (!Enum.IsDefined(typeof(OnlineVideosWebservice.SiteState), value)) return false;
			var state = (OnlineVideosWebservice.SiteState)value;
			switch (state)
			{
				case OnlineVideosWebservice.SiteState.Broken:
					result = Color.FromArgb(255, 53, 87).FromDrawingColor();
					break;
				case OnlineVideosWebservice.SiteState.Reported:
					result = Color.FromArgb(255, 220, 96).FromDrawingColor();
					break;
				case OnlineVideosWebservice.SiteState.Working:
					result = Color.FromArgb(55, 194, 48).FromDrawingColor();
					break;
			}
			return true;
		}

		public bool ConvertBack(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
		{
			throw new NotImplementedException();
		}
	}

    public class ExtendedInfoConverter : IValueConverter
    {
        public bool Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
        {
            result = string.Empty;
            var model = value as VideoViewModel;
            string param = parameter as string;
            if (model != null && model.VideoInfo != null && !string.IsNullOrEmpty(param))
            {
                string myValue;
				if (model.GetExtendedPropertyValue(param, out myValue)) 
                {
                    result = myValue;
                    return true;
                }
            }
            return false;
        }

        public bool ConvertBack(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
        {
            throw new NotImplementedException();
        }
    }

	public class BoolFocusPrioConverter : IValueConverter
	{
		public bool Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
		{
			result = (bool)value ? SetFocusPriority.Highest : SetFocusPriority.Default;
			return true;
		}

		public bool ConvertBack(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
		{
			throw new NotImplementedException();
		}
	}

    public class FilterStateOptionLocalizedConverter : IValueConverter
    {
        public bool Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
        {
            result = LocalizationHelper.Translate("[OnlineVideos." + ((SiteManagementWorkflowModel.FilterStateOption)value).ToString() + "]");
            return true;
        }

        public bool ConvertBack(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
        {
            throw new NotImplementedException();
        }
    }

    public class LanguageCodeLocalizedConverter : IValueConverter
    {
        public bool Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
        {
            if (string.IsNullOrEmpty(value as string))
                result = LocalizationHelper.Translate("[OnlineVideos.All]");
            else
            {
                if (parameter == null)
                    result = TranslationLoader.GetLocalizedLanguageName(value as string);
                else
                    result = value as string;
            }
            return true;
        }

        public bool ConvertBack(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
        {
            throw new NotImplementedException();
        }
    }

}
