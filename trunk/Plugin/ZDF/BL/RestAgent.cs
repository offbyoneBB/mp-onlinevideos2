namespace ZDF.BL
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Web;
    using System.Xml.Serialization;

    public class RestAgent
    {
        private string _jSession;
        private string _surveyUrl;
        public string BaseUrl;
        public string CVer = "unknown";
        public bool DoLogging;
        public string Suffix;

        public RestAgent(string baseUrl)
        {
            this.BaseUrl = baseUrl;
        }

        private string AddSuffix(string path)
        {
            int index = path.IndexOf('?');
            if (index < 0)
            {
                return (path + this.Suffix);
            }
            return (path.Substring(0, index) + this.Suffix + path.Substring(index));
        }

        public TeaserListe AktuelleMeldungen()
        {
            return (TeaserListe) this.DoRequest("/AktuelleMeldungen", typeof(TeaserListe));
        }

        public TeaserListe AktuelleSendungen()
        {
            return (TeaserListe) this.DoRequest("/AktuelleSendungen", typeof(TeaserListe));
        }

        public ZDF.BL.Beitrag Beitrag(string beitragID)
        {
            string uriPath = string.Format("/Beitrag?id={0}", HttpUtility.UrlEncode(beitragID));
            return (ZDF.BL.Beitrag) this.DoRequest(uriPath, typeof(ZDF.BL.Beitrag));
        }

        public void Bewertung(string beitragID, int sterne)
        {
            this.DoRequest("/Rating;jsessionid=" + this.JSession + "?id=" + HttpUtility.UrlEncode(beitragID) + "&rating=" + sterne.ToString().Replace(',', '.'), null, this.SurveyUrl);
        }

        protected virtual object DoRequest(string uriPath, Type responseType)
        {
            return this.DoRequest(uriPath, responseType, this.BaseUrl);
        }

        protected virtual object DoRequest(string uriPath, Type responseType, string baseUrl)
        {
            object obj2;
            string s = "null";
            string requestUriString = baseUrl + this.AddSuffix(uriPath);
            if (requestUriString.IndexOf("?") > 0)
            {
                requestUriString = requestUriString + "&cver=" + this.CVer;
            }
            else
            {
                requestUriString = requestUriString + "?cver=" + this.CVer;
            }
            DateTime now = DateTime.Now;
            DateTime minValue = DateTime.MinValue;
            try
            {
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(requestUriString);
                request.Timeout = 10000;
                HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                s = new StreamReader(response.GetResponseStream()).ReadToEnd();
                minValue = DateTime.Now;
            }
            catch (Exception exception)
            {
                Logger.Log(exception);
                Fehler error = new Fehler();
                error.Beschreibung = "Der Empfang der ZDFmediathek ist vor\x00fcbergehend gest\x00f6rt. Bitte schlie\x00dfen Sie diese Meldung mit der \"Zur\x00fcck\"- oder \"back\"-Taste auf Ihrer Fernbedienung und versuchen Sie es erneut. Sollte diese Meldung wiederholt erscheinen, schlie\x00dfen Sie die ZDFmediathek mit der gro\x00dfen gr\x00fcnen Taste auf Ihrer Fernbedienung und probieren Sie es bitte zu einem sp\x00e4teren Zeitpunkt nochmal.";
                throw new CAE_Exception(error);
            }
            finally
            {
                if (this.DoLogging)
                {
                    Logger.Log("Request at " + now.ToString("HH:mm:ss.ffffff") + ":\r\n" + requestUriString + "\r\nResponse at " + minValue.ToString("HH:mm:ss.ffffff") + ":\r\n" + s);
                }
            }
            if (responseType == null)
            {
                return null;
            }
            try
            {
                XmlSerializer serializer = new XmlSerializer(responseType);
                StringReader textReader = new StringReader(s);
                obj2 = serializer.Deserialize(textReader);
            }
            catch (Exception exception2)
            {
                try
                {
                    XmlSerializer serializer2 = new XmlSerializer(typeof(Fehler));
                    StringReader reader3 = new StringReader(s);
                    Fehler fehler2 = (Fehler) serializer2.Deserialize(reader3);
                    Logger.Log("CAE Error: \r\n" + s);
                    throw new CAE_Exception(fehler2);
                }
                catch (CAE_Exception)
                {
                    throw;
                }
                catch
                {
                    Logger.Log(exception2);
                    try
                    {
                        if (s != null)
                        {
                            Logger.Log("Response text was as follows:\r\n" + s);
                        }
                    }
                    catch
                    {
                    }
                    throw new ApplicationException("Es ist ein Fehler aufgetreten.", exception2);
                }
            }
            return obj2;
        }

        public ZDF.BL.Teaser[] Inhalt(Kanaltyp typ, string filter)
        {
            return (ZDF.BL.Teaser[]) this.DoRequest(string.Concat(new object[] { "/Inhalt?typ=", typ, "&filter=", HttpUtility.UrlEncode(filter) }), typeof(ZDF.BL.Teaser[]));
        }

        public Update IstUpdateVerfuegbar()
        {
            int num = (int) this.DoRequest("/IstUpdateVerfuegbar", typeof(int));
            return (Update) num;
        }

        public TeaserListe LiveSendungen(DateTime date, int zeitschiene)
        {
            string uriPath = string.Format("/LiveSendungen?tag={0}&zeitschiene={1}", date.ToString("d", new CultureInfo("DE-de").DateTimeFormat), zeitschiene);
            return (TeaserListe) this.DoRequest(uriPath, typeof(TeaserListe));
        }

        public TeaserListe MeistGesehen()
        {
            return (TeaserListe) this.DoRequest("/MeistGesehen", typeof(TeaserListe));
        }

        public TeaserListe Redaktionstipps()
        {
            return (TeaserListe) this.DoRequest("/Redaktionstipps", typeof(TeaserListe));
        }

        public TeaserListe SiebenTageRueckblick(DateTime date, int zeitschiene)
        {
            string uriPath = string.Format("/SiebenTageRueckblick?tag={0}&zeitschiene={1}", date.ToString("d", new CultureInfo("DE-de").DateTimeFormat), zeitschiene);
            return (TeaserListe) this.DoRequest(uriPath, typeof(TeaserListe));
        }

        public ZDF.BL.Suchergebnis Suchergebnis(string begriff, int von, int bis, SortOption option, bool aufsteigend, string kanal, bool video, bool bilderserie, bool audio, bool interaktiv, DateTime vonDatum, DateTime bisDatum)
        {
            string uriPath = string.Format("/Suchergebnis?begriff={0}&von={1}&bis={2}&sortiertNach={3}&nurKanal={4}&mitVideo={5}&mitBildergalerie={6}&mitAudio={7}&mitInteraktiv={8}&vonDatum={9}&bisDatum={10}&aufsteigend={11}", new object[] { HttpUtility.UrlEncode(begriff), von, bis, option, kanal, video, bilderserie, audio, interaktiv, vonDatum.ToString("d", new CultureInfo("DE-de").DateTimeFormat), bisDatum.ToString("d", new CultureInfo("DE-de").DateTimeFormat), aufsteigend });
            return (ZDF.BL.Suchergebnis) this.DoRequest(uriPath, typeof(ZDF.BL.Suchergebnis));
        }

        public TeaserListe Teaser(string parentID)
        {
            return this.Teaser(parentID, "Alle");
        }

        public TeaserListe Teaser(string parentID, string sort)
        {
            return (TeaserListe) this.DoRequest("/Teaser?parentID=" + HttpUtility.UrlEncode(parentID) + "&filter=" + sort, typeof(TeaserListe));
        }

        public void TrackBeitrag(string beitragID)
        {
            this.DoRequest("/Tracking;jsessionid=" + this.JSession + "?id=" + HttpUtility.UrlEncode(beitragID), null, this.SurveyUrl);
        }

        public string[] TrackingIVW(string pageName, string navigationPath, string broadcastID)
        {
            string uriPath = "/TrackingIVW?seite=" + pageName;
            if (navigationPath != null)
            {
                uriPath = uriPath + "&pfad=" + HttpUtility.UrlEncode(navigationPath);
            }
            if (broadcastID != null)
            {
                uriPath = uriPath + "&beitragID=" + broadcastID;
            }
            return (string[]) this.DoRequest(uriPath, typeof(string[]));
        }

        public TrennerSlot[] TrennerSlots(string beitragID, string kanalID)
        {
            return (TrennerSlot[]) this.DoRequest("/TrennerSlots?beitragID=" + HttpUtility.UrlEncode(beitragID) + "&kanalID=" + HttpUtility.UrlEncode(kanalID), typeof(TrennerSlot[]));
        }

        public TeaserListe WeitereBeitraege(string beitragID, string kanalID)
        {
            string uriPath = string.Format("/WeitereBeitraege?beitragID={0}&kanalID={1}", HttpUtility.UrlEncode(beitragID), HttpUtility.UrlEncode(kanalID));
            return (TeaserListe) this.DoRequest(uriPath, typeof(TeaserListe));
        }

        public string JSession
        {
            get
            {
                if (this._jSession == null)
                {
                    this._jSession = (string) this.DoRequest("/Session", typeof(string), this.SurveyUrl);
                }
                else
                {
                    this._jSession = (string) this.DoRequest("/Session;jsessionid=" + this._jSession, typeof(string), this.SurveyUrl);
                }
                return this._jSession;
            }
        }

        public string SurveyUrl
        {
            get
            {
                if (this._surveyUrl == null)
                {
                    return this.BaseUrl;
                }
                return this._surveyUrl;
            }
            set
            {
                this._surveyUrl = value;
            }
        }
    }
}

