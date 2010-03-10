namespace ZDFMediathek2009.Code
{
    using System;
    using ZDFMediathek2009.Code.DTO;

    public class ConfigurationHelper
    {
        public static string GetAktuellsteServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_aktuellste");
        }

        public static string GetBeitragsDetailsServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_beitragsDetails");
        }

        public static string GetBeitragsTrennerServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_beitragsTrenner");
        }

        public static string GetDictionaryKeyValue(string key)
        {
            foreach (value value2 in RestAgent.Configuration.dictionary)
            {
                if (value2.key.ToLower() == key.ToLower())
                {
                    return value2.Value;
                }
            }
            return key;
        }

        public static string GetGanzeSendungenServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_ganzeSendungen");
        }

        public static string GetInhaltsseiteServiceUrl()
        {
            return GetSystemKeyValue(RestAgent.Configuration, "serviceUrl_inhaltsseite");
        }

        public static string GetIstMCEUpdateVerfuegbarServiceUrlServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_istMCEUpdateVerfuegbar");
        }

        public static string GetLiveServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_live");
        }

        public static string GetMeistGesehenServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_meistGesehen");
        }

        public static string GetNavigationServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_navigation");
        }

        public static string GetRubrikenServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_rubriken");
        }

        public static string GetSendeTemplateMailServiceUrl()
        {
            return GetSystemKeyValue(RestAgent.Configuration, "serviceUrl_sendeTemplateMail");
        }

        public static string GetSendungenAbisZServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_sendungenAbisZ");
        }

        public static string GetSendungVerpasstServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_sendungVerpasst");
        }

        public static string GetSessionServiceUrl()
        {
            return GetSystemKeyValue(RestAgent.Configuration, "serviceUrl_session");
        }

        public static string GetSkinDetailsServiceUrl()
        {
            return GetSystemKeyValue(RestAgent.Configuration, "serviceUrl_skinInfo");
        }

        public static string GetSucheServiceUrl()
        {
            return GetSystemKeyValue(RestAgent.Configuration, "serviceUrl_detailsSuche");
        }

        public static string GetSystemKeyValue(string key)
        {
            foreach (value value2 in RestAgent.Configuration.system)
            {
                if (value2.key == key)
                {
                    return value2.Value;
                }
            }
            return "";
        }

        public static string GetSystemKeyValue(configuration config, string key)
        {
            foreach (value value2 in config.system)
            {
                if (value2.key == key)
                {
                    return value2.Value;
                }
            }
            return "";
        }

        public static string GetTeaserServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_teaser");
        }

        public static string GetThemenServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_themen");
        }

        public static string GetTippsServiceUrl(configuration config)
        {
            return GetSystemKeyValue(config, "serviceUrl_tipps");
        }

        public static string GetTrackingServiceUrl()
        {
            return GetSystemKeyValue(RestAgent.Configuration, "serviceUrl_tracking");
        }

        public static string GetWeitereBeitrageServiceUrl()
        {
            return GetSystemKeyValue(RestAgent.Configuration, "serviceUrl_weitereBeitraege");
        }
    }
}

