using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace OnlineVideos.MediaPortal2.Configuration
{
    public class SettingsBag
    {
        [Setting(SettingScope.User, true)]
        public bool UseAgeConfirmation { get; set; }
        [Setting(SettingScope.User, "123")]
        public string AgeConfirmationPin { get; set; }
        [Setting(SettingScope.User, 15)]
        public int UtilTimeout { get; set; }
        [Setting(SettingScope.User, 30)]
        public int CacheTimeout { get; set; }
    }

    public class UseAgeConfirmation : YesNo
    {
        public override void Load()
        {
            _yes = SettingsManager.Load<SettingsBag>().UseAgeConfirmation;
        }

        public override void Save()
        {
            SettingsBag settings = SettingsManager.Load<SettingsBag>();
            settings.UseAgeConfirmation = _yes;
            SettingsManager.Save(settings);
        }
    }

    public class AgeConfirmationPin : Entry
    {
        public override void Load()
        {
            _value = SettingsManager.Load<SettingsBag>().AgeConfirmationPin;
        }

        public override int DisplayLength
        {
            get { return 20; }
        }
        public override void Save()
        {
            SettingsBag settings = SettingsManager.Load<SettingsBag>();
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
            _value = SettingsManager.Load<SettingsBag>().UtilTimeout;
        }

        public override void Save()
        {
            SettingsBag settings = SettingsManager.Load<SettingsBag>();
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
            _value = SettingsManager.Load<SettingsBag>().CacheTimeout;
        }

        public override void Save()
        {
            SettingsBag settings = SettingsManager.Load<SettingsBag>();
            settings.CacheTimeout = (int)_value;
            SettingsManager.Save(settings);
        }
    }
    
}
