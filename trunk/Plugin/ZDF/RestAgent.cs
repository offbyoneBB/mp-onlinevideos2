namespace ZDFMediathek2009.Code
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Xml.Serialization;
    using ZDFMediathek2009.Code.DTO;

    public class RestAgent
    {
        private string _jSessionId;
        public string ConfigurationServiceUrl;
        public string Suffix;

        public RestAgent(string baseUrl)
        {
            this.ConfigurationServiceUrl = baseUrl;
            Configuration = GetConfiguration();
        }

        public teaserlist Aktuellste(string url, string kanalID, int maxLength, int offset, bool showFullBroadcasts)
        {
            try
            {
                response response = (response) this.DoRequest(string.Concat(new object[] { url, "?id=", kanalID, "&maxLength=", maxLength, "&offset=", offset, "&ganzeSendungen=", showFullBroadcasts }), typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public video BeitragsDetail(string url, string id, string kanalId)
        {
            response response = (response) this.DoRequest(url + "?id=" + id + "&kanalId=" + kanalId, typeof(response));
            return (this.CheckStatusCode(response).Item as video);
        }

        public teaserlist BeitragsTrenner(string url, string id, string kanalId)
        {
            try
            {
                response response = (response) this.DoRequest(url + "?id=" + id + "&kanalId=" + kanalId, typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        private response CheckStatusCode(response response)
        {
            if (response == null)
            {
                throw new ServiceException("", ServiceExceptionType.Error);
            }
            if (response.status == null)
            {
                throw new ServiceException("", ServiceExceptionType.Error);
            }
            if (response.status.statuscode == statusStatuscode.ok)
            {
                return response;
            }
            if (response.status.statuscode == statusStatuscode.error)
            {
                throw new ServiceException(response.status.debuginfo, ServiceExceptionType.Error);
            }
            if (response.status.statuscode == statusStatuscode.fsk)
            {
                throw new ServiceException(response.status.debuginfo, ServiceExceptionType.FSK);
            }
            if (response.status.statuscode == statusStatuscode.geolocation)
            {
                throw new ServiceException(response.status.debuginfo, ServiceExceptionType.GeoLocation);
            }
            if (response.status.statuscode == statusStatuscode.mailNotSent)
            {
                throw new ServiceException(response.status.debuginfo, ServiceExceptionType.MainNotSent);
            }
            if (response.status.statuscode == statusStatuscode.notFound)
            {
                throw new ServiceException(response.status.debuginfo, ServiceExceptionType.NotFound);
            }
            throw new ServiceException(response.status.debuginfo, ServiceExceptionType.Error);
        }

        public static configuration Configuration { get; protected set; }
        private configuration GetConfiguration()
        {
            response response = (response) this.DoRequest(this.ConfigurationServiceUrl, typeof(response));
            return (this.CheckStatusCode(response).Item as configuration);
        }

        public teaserlist DetailsSuche(string url, string searchString, int maxLength, int offset)
        {
            response response = (response) this.DoRequest(string.Concat(new object[] { url, "?searchString=", searchString, "&maxLength=", maxLength, "&offset=", offset }), typeof(response));
            return (this.CheckStatusCode(response).Item as teaserlist);
        }

        protected virtual void DoRequest(string url)
        {
            string requestUriString = url;
            DateTime now = DateTime.Now;            
            try
            {
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(requestUriString);
                HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                new StreamReader(response.GetResponseStream()).ReadToEnd();
                DateTime time2 = DateTime.Now;
            }
            catch (Exception exception)
            {
                //Logger.Log(exception);
            }
            finally
            {
                //bool doLogging = this.DoLogging;
            }
        }

        protected virtual object DoRequest(string url, Type responseType)
        {
            string s = "null";
            string requestUriString = url;
            DateTime now = DateTime.Now;           
            try
            {
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(requestUriString);
                HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                s = new StreamReader(response.GetResponseStream()).ReadToEnd();
                DateTime time2 = DateTime.Now;
            }
            catch (Exception exception)
            {
                //Logger.Log(exception);
            }
            finally
            {
                //bool doLogging = this.DoLogging;
            }
            if (responseType == null)
            {
                return null;
            }
            try
            {
                XmlSerializer serializer = new XmlSerializer(responseType);
                StringReader textReader = new StringReader(s);
                return serializer.Deserialize(textReader);
            }
            catch (Exception exception2)
            {
                //Logger.Log("Exception by deserialization: " + exception2.Message);
                return null;
            }
        }

        public imageseries GalleryBeitragsDetail(string url, string id, string kanalId)
        {
            response response = (response) this.DoRequest(url + "?id=" + id + "&kanalId=" + kanalId, typeof(response));
            return (this.CheckStatusCode(response).Item as imageseries);
        }

        public teaserlist GanzeSendungen(string url)
        {
            try
            {
                response response = (response) this.DoRequest(url, typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public ZDFMediathek2009.Code.Teaser[] GetMCETeasers(teaserlist teaserlist, TeaserListChoiceType choice)
        {
            if (teaserlist == null)
            {
                return new ZDFMediathek2009.Code.Teaser[0];
            }
            if (teaserlist.teasers == null)
            {
                return new ZDFMediathek2009.Code.Teaser[0];
            }
            ZDFMediathek2009.Code.Teaser[] teaserArray = new ZDFMediathek2009.Code.Teaser[teaserlist.teasers.Length];
            for (int i = 0; i < teaserArray.Length; i++)
            {
                ZDFMediathek2009.Code.Teaser teaser = new ZDFMediathek2009.Code.Teaser();
                teaser.Value = teaserlist.teasers[i];
                teaser.TeaserListChoiceType = choice;
                teaserArray[i] = teaser;
            }
            return teaserArray;
        }

        public page Inhaltsseite(string url, string id)
        {
            try
            {
                response response = (response) this.DoRequest(url + "?id=" + id, typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as page);
                }
            }
            catch
            {
            }
            return null;
        }

        public Update IsUpdateAvailable(string url, string mceVersion, string serviceVersion)
        {
            try
            {
                response response = (response) this.DoRequest(url.Replace("%(VERSION)", serviceVersion) + "?cver=" + mceVersion, typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (Update) response.Item;
                }
            }
            catch
            {
            }
            return Update.Current;
        }

        public void IVWTracking(string url)
        {
            if (url != "")
            {
                DateTime now = DateTime.Now;                
                try
                {
                    HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                    request.UserAgent = "Media Center ZDFmediathek";
                    WebResponse response = request.GetResponse();
                    response.Close();
                }
                catch (Exception exception)
                {
                    //Logger.Log("Error getting logpixel: " + exception);
                }
            }
        }

        public teaserlist Live(string url, int maxLength, int offset)
        {
            try
            {
                response response = (response) this.DoRequest(string.Concat(new object[] { url, "?maxLength=", maxLength, "&offset=", offset }), typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public mainnavigation MainNavigation(string url)
        {
            try
            {
                response response = (response) this.DoRequest(url, typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as mainnavigation);
                }
            }
            catch
            {
            }
            return null;
        }

        public teaserlist MeistGesehen(string url, string id, int maxLength, int offset)
        {
            try
            {
                response response = (response) this.DoRequest(string.Concat(new object[] { url, "?id=", id, "&maxLength=", maxLength, "&offset=", offset }), typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public teaserlist Rubriken(string url, int maxLength, int offset)
        {
            try
            {
                response response = (response) this.DoRequest(string.Concat(new object[] { url, "?maxLength=", maxLength, "&offset=", offset }), typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        
        public channellist SendungenAbisZChannels(string url)
        {
            try
            {
                response response = (response) this.DoRequest(url + "?characterRangeStart=0-9&characterRangeEnd=Z&detailLevel=1", typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as channellist);
                }
            }
            catch
            {
            }
            return null;
        }

        public teaserlist SendungenAbisZTeasers(string url, string characterRangeStart, string characterRangeEnd)
        {
            try
            {
                response response = (response) this.DoRequest(url + "?characterRangeStart=" + characterRangeStart + "&characterRangeEnd=" + characterRangeEnd + "&detailLevel=2", typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public teaserlist SendungVerpasst(string url, int maxLength, int offset, string date)
        {
            try
            {
                response response = (response) this.DoRequest(string.Concat(new object[] { url, "?", date, "&maxLength=", maxLength, "&offset=", offset }), typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public string Session(string url, string jsessionid)
        {
            response response = (response) this.DoRequest(url + ";jsessionid=" + jsessionid, typeof(response));
            return this.CheckStatusCode(response).Item.ToString();
        }

        public skindetails SkinDetails(string url, string id)
        {
            try
            {
                response response = (response) this.DoRequest(url + "?id=" + id, typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as skindetails);
                }
            }
            catch
            {
            }
            return null;
        }

        public teaserlist Teaser(string url, string id)
        {
            try
            {
                response response = (response) this.DoRequest(url + "?id=" + id, typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public teaserlist Themen(string url, int maxLength, int offset)
        {
            try
            {
                response response = (response) this.DoRequest(string.Concat(new object[] { url, "?maxLength=", maxLength, "&offset=", offset }), typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public teaserlist Tipps(string url, string id, int maxLength, int offset)
        {
            try
            {
                response response = (response) this.DoRequest(string.Concat(new object[] { url, "?id=", id, "&maxLength=", maxLength, "&offset=", offset }), typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public teaserlist TippsNoChannels(string url, int maxLength, int offset)
        {
            try
            {
                response response = (response) this.DoRequest(string.Concat(new object[] { url, "?channels=false&maxLength=", maxLength, "&offset=", offset }), typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public void Tracking(string id)
        {
            this.DoRequest(ConfigurationHelper.GetTrackingServiceUrl() + ";jsessionid=" + this.JSessionId + "?id=" + HttpUtility.UrlEncode(id));
        }

        public teaserlist WeitereBeitrage(string url, string id, int maxLength, int offset)
        {
            try
            {
                response response = (response) this.DoRequest(string.Concat(new object[] { url, "?id=", id, "&maxLength=", maxLength, "&offset=", offset }), typeof(response));
                if (response.status.statuscode == statusStatuscode.ok)
                {
                    return (response.Item as teaserlist);
                }
            }
            catch
            {
            }
            return null;
        }

        public string JSessionId
        {
            get
            {
                if (this._jSessionId == null)
                {
                    this._jSessionId = this.Session(ConfigurationHelper.GetSessionServiceUrl(), "");
                }
                else
                {
                    this._jSessionId = this.Session(ConfigurationHelper.GetSessionServiceUrl(), this._jSessionId);
                }
                return this._jSessionId;
            }
        }
    }
}

