using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Settings;

namespace OnlineVideos.MediaPortal2.Configuration
{
    public class Settings
    {
        [Setting(SettingScope.User, true)]
        public bool UseAgeConfirmation { get; set; }
        [Setting(SettingScope.User, "123")]
        public string AgeConfirmationPin { get; set; }
        [Setting(SettingScope.User, 15)]
        public int UtilTimeout { get; set; }
        [Setting(SettingScope.User, 30)]
        public int CacheTimeout { get; set; }
		[Setting(SettingScope.User, "")]
		public string DownloadFolder { get; set; }

		public void SetValuesToApi()
		{
			OnlineVideoSettings.Instance.UseAgeConfirmation = UseAgeConfirmation;
			OnlineVideoSettings.Instance.CacheTimeout = CacheTimeout;
			OnlineVideoSettings.Instance.UtilTimeout = UtilTimeout;
			OnlineVideoSettings.Instance.DownloadDir = DownloadFolder;
		}
    }

    public class UseAgeConfirmation : YesNo
    {
        public override void Load()
        {
            _yes = SettingsManager.Load<Settings>().UseAgeConfirmation;
        }

        public override void Save()
        {
            Settings settings = SettingsManager.Load<Settings>();
            settings.UseAgeConfirmation = _yes;
            SettingsManager.Save(settings);
        }
    }

    public class AgeConfirmationPin : Entry
    {
        public override void Load()
        {
            _value = SettingsManager.Load<Settings>().AgeConfirmationPin;
        }

        public override int DisplayLength
        {
            get { return 20; }
        }

        public override void Save()
        {
            Settings settings = SettingsManager.Load<Settings>();
            settings.AgeConfirmationPin = _value;
            SettingsManager.Save(settings);
        }
    }

    public class UtilTimeout : LimitedNumberSelect
    {
        public override void Load()
        {
            _type = NumberType.Integer;
            _step = 1;
            _lowerLimit = 1;
            _upperLimit = 60;
            _value = SettingsManager.Load<Settings>().UtilTimeout;
        }

        public override void Save()
        {
            Settings settings = SettingsManager.Load<Settings>();
            settings.UtilTimeout = (int)_value;
            SettingsManager.Save(settings);
        }
    }

    public class CacheTimeout : LimitedNumberSelect
    {
        public override void Load()
        {
            _type = NumberType.Integer;
            _step = 1;
            _lowerLimit = 0;
            _upperLimit = 600;
            _value = SettingsManager.Load<Settings>().CacheTimeout;
        }

        public override void Save()
        {
            Settings settings = SettingsManager.Load<Settings>();
            settings.CacheTimeout = (int)_value;
            SettingsManager.Save(settings);
        }
    }

	public class DownloadFolder : PathEntry
	{
		public DownloadFolder()
		{
			_pathSelectionType = PathSelectionType.Folder;
		}

		public override void Load()
		{
			_path = SettingsManager.Load<Settings>().DownloadFolder;
		}

		public override void Save()
		{
			Settings settings = SettingsManager.Load<Settings>();
			settings.DownloadFolder = _path;
			SettingsManager.Save(settings);
		}
	}

	public class SiteManagement : CustomConfigSetting
	{
	}
    
}
