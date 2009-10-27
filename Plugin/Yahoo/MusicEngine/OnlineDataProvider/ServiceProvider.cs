using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using Yahoo;
using YahooMusicEngine.Entities;
using YahooMusicEngine.Services;
using YahooMusicEngine.Locale;

namespace YahooMusicEngine.OnlineDataProvider
{
  public class ServiceProvider
  {
    public delegate void OnErrorEventHandler(Exception ex);
    public event OnErrorEventHandler OnError;

    Yahoo.Authentication auth = null;

    private List<LocaleEntity> _avaiableLocales;
    public List<LocaleEntity> AvaiableLocales
    {
      get { return _avaiableLocales; }
    }

    private bool error;

    public bool Error
    {
      get { return error; }
      set { error = value; }
    }

    private string errorMessage;

    public string ErrorMessage
    {
      get { return errorMessage; }
      set { errorMessage = value; }
    }


    private string _appid;
    public string AppId
    {
      get { return _appid; }
      set { _appid = value; }
    }

    private string sharedSecret;

    public string SharedSecret
    {
      get { return sharedSecret; }
      set { sharedSecret = value; }
    }

    private string token;

    public string Token
    {
      get { return token; }
      set { token = value; }
    }

    private LocaleEntity _locale;
    public LocaleEntity Locale
    {
      get { return _locale; }
      set { _locale = value; }
    }

    private ResultFormat _format;

    public ResultFormat Format
    {
      get { return _format; }
      set { _format = value; }
    }

    private UserEntity curentUser;

    public UserEntity CurentUser
    {
      get { return curentUser; }
      set { curentUser = value; }
    }



    public ServiceProvider()
    {
      _avaiableLocales = new List<LocaleEntity>();
      _avaiableLocales.Add(new LocaleEntity("United States", "us", "1301797", "4195351", "en"));
      _avaiableLocales.Add(new LocaleEntity("United States (Español)", "e1", "1307666", "559940629", "es"));
      _avaiableLocales.Add(new LocaleEntity("Canada", "ca", "1307409", "642778131", "en"));
      _avaiableLocales.Add(new LocaleEntity("Mexico", "mx", "8257040", "640680961", "es"));
      _avaiableLocales.Add(new LocaleEntity("Australia", "au", "1307669", "638583826", "en"));
      _avaiableLocales.Add(new LocaleEntity("New Zealand", "nz", "5300947", "638583833", "en"));
      SetLocale("us");
      Cache = new Dictionary<string, object>();
      SharedSecret = string.Empty;
      Token = string.Empty;
      CurentUser = new UserEntity();
    }

    public void SetLocale(string apilang)
    {
      foreach (LocaleEntity entity in _avaiableLocales)
      {
        if (entity.ApiHost == apilang)
        {
          Locale = entity;
        }
      }
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void ClearCache()
    {
      Cache.Clear();
    }

    public void SetLocaleByName(string langname)
    {
      foreach (LocaleEntity entity in _avaiableLocales)
      {
        if (entity.Locale == langname)
        {
          Locale = entity;
        }
      }
    }
    /// <summary>
    /// Inits this instance.
    /// </summary>
    public void Init()
    {
      if (!string.IsNullOrEmpty(Token))
      {
        auth = new Authentication(this.AppId, this.SharedSecret);
        UserInformationService userinfo = new UserInformationService();
        this.GetData(userinfo);
        this.CurentUser = userinfo.Response.User;
      }
    }

    private Dictionary<string,object> Cache = null;

 
    public string BuildUrl(IService serv)
    {
      string url = string.Format("http://{0}.music.yahooapis.com/{1}/v1/{2}", Locale.ApiHost, serv.ServiceName, serv.Resource);
      StringBuilder data = new StringBuilder();
      data.Append("appid=" + HttpUtility.UrlEncode(AppId));
      foreach (KeyValuePair<string, string> kpv in serv.Params)
      {
        data.Append(string.Format("&{0}={1}", kpv.Key, kpv.Value));
      }
      return url + "?" + data.ToString();
    }

    public XmlDocument GetData(IService serv)
    {
      return GetData(serv, true);
    }

    public XmlDocument GetData(IService serv,bool useCache)
    {
      string url = BuildUrl(serv);
      Error = false;
      ErrorMessage = string.Empty;
      if (!Cache.ContainsKey(url) || !useCache)
      {
        try
        {
          if (auth == null)
          {
            auth = new Authentication(this.AppId, this.SharedSecret);
          }
          // Redirect the user to the use sign-in page  
          auth.Token = this.Token;
          // Attempt to get user credentials  
          if (!string.IsNullOrEmpty(Token) && (!string.IsNullOrEmpty(SharedSecret)))
          {
            auth.UpdateCredentials();
          }
          Stream response;
          //XmlDocument doc = RetrieveData(url);
          if (auth.IsCredentialed)
          {
            response = auth.GetAuthenticatedServiceStream(new Uri(url));
          }
          else
          {
            response = RetrieveData(url);
          }
          // Get the stream associated with the response.
          StreamReader reader = new StreamReader(response, System.Text.Encoding.UTF8, true);
          String sXmlData = reader.ReadToEnd().Replace('\0', ' ');
          response.Close();
          reader.Close();
          XmlDocument doc = new XmlDocument();
          doc.LoadXml(sXmlData);
          serv.Parse(doc);
          Cache.Add(url, doc);
          return doc;
        }
        catch (AuthenticationException ex)
        {
            if (OnError != null) OnError(ex);
            Error = true;
            ErrorMessage = ex.Message;
            return null;
        }
        catch (Exception ex)
        {
          if (OnError != null) OnError(ex);
          Error = true;
          ErrorMessage = ex.Message;
          return null;
        }
      }
      else
      {
        serv.Parse((XmlDocument)Cache[url]);
        return (XmlDocument)Cache[url];
      }
    }

    public XmlDocument PostData(IService serv)
    {
      bool useCache = false;
      string url = BuildUrl(serv); //string.Format("http://{0}.music.yahooapis.com/{1}/v1/{2}?appid={3}", Locale.ApiHost, serv.ServiceName,serv.Resource,AppId);
      StringBuilder data = new StringBuilder();
      data.Append("appid=" + AppId);
      foreach (KeyValuePair<string, string> kpv in serv.Params)
      {
        data.Append(string.Format("&{0}={1}", kpv.Key, kpv.Value));
      }
      Error = false;
      ErrorMessage = string.Empty;
      if (!Cache.ContainsKey(url) && !useCache)
      {
        try
        {
          if (auth == null)
          {
            auth = new Authentication(this.AppId, this.SharedSecret);
          }
          // Redirect the user to the use sign-in page  
          auth.Token = this.Token;
          // Attempt to get user credentials  
          if (!string.IsNullOrEmpty(Token) && (!string.IsNullOrEmpty(SharedSecret)))
          {
            auth.UpdateCredentials();
          }
          Stream response = null;
          //XmlDocument doc = RetrieveData(url);
          if (auth.IsCredentialed)
          {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            // Set type to POST  
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new System.Net.CookieContainer();
            request.CookieContainer.SetCookies(new System.Uri((new System.Uri(url)).AbsoluteUri), auth.Cookies);

            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] byteData = encoding.GetBytes(data.ToString());

            // Set the content length in the request headers  
            request.UserAgent = "BBAuth .NET";
            //request.ContentLength = byteData.Length;
            // Write data  
            Stream postStream = request.GetRequestStream();
            postStream.Write(byteData, 0, byteData.Length);
            postStream.Close();

            using (HttpWebResponse webresponse = (HttpWebResponse)request.GetResponse())
            {

              if (webresponse != null) // Get the stream associated with the response.
                response = webresponse.GetResponseStream();
              //response = auth.GetAuthenticatedServiceStream(new Uri(url));
            }
          }
          else
          {
            return null;
            //response = RetrieveData(url);
          }
          // Get the stream associated with the response.
          if (response == null)
            return null;

          StreamReader reader = new StreamReader(response, System.Text.Encoding.UTF8, true);
          String sXmlData = reader.ReadToEnd().Replace('\0', ' ');
          response.Close();
          reader.Close();
          XmlDocument doc = new XmlDocument();
          doc.LoadXml(sXmlData);
          serv.Parse(doc);
          Cache.Add(url, doc);
          return doc;
        }
        catch (WebException webExcp)
        {
          WebExceptionStatus status = webExcp.Status;
          // If status is WebExceptionStatus.ProtocolError, 
          //   there has been a protocol error and a WebResponse 
          //   should exist. Display the protocol error.
          if (status == WebExceptionStatus.ProtocolError)
          {
            HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
            StreamReader reader = new StreamReader(httpResponse.GetResponseStream(), System.Text.Encoding.UTF8, true);
            String sXmlData = reader.ReadToEnd().Replace('\0', ' ');
            Console.WriteLine((int)httpResponse.StatusCode + " - "
               + httpResponse.StatusCode);
          }
          return null;
        }
        catch (AuthenticationException ex)
        {

          return null;
        }
        //catch (Exception ex)
        //{
        //  //System.Windows.Forms.MessageBox.Show(ex.Message);
        //  if (OnError != null)
        //    OnError(ex);
        //  Error = true;
        //  ErrorMessage = ex.Message;
        //  return null;
        //}
      }
      else
      {
        serv.Parse((XmlDocument)Cache[url]);
        return (XmlDocument)Cache[url];
      }
    }

    private Stream RetrieveData(string sUrl)
    {
      if (sUrl == null || sUrl.Length < 1 || sUrl[0] == '/')
      {
        return null;
      }
      //sUrl = this.Settings.UpdateUrl(sUrl);
      HttpWebRequest request = null;
      HttpWebResponse response = null;
      try
      {
        request = (HttpWebRequest)WebRequest.Create(sUrl);
        request.Timeout = 20000;
        response = (HttpWebResponse)request.GetResponse();

        if (response != null) // Get the stream associated with the response.
          return response.GetResponseStream();

      }
      catch (Exception e)
      {
        // can't connect, timeout, etc
      }
      finally
      {
        //if (response != null) response.Close(); // screws up the decompression
      }

      return null;
    }

    #region generic func's
    public string GetStationImage(string StationtId, int height)
    {
      return GetStationImage(StationtId, "size=" + height.ToString());
    }

    public string GetStationImage(string StationtId, string param)
    {
      if (string.IsNullOrEmpty(param))
        return string.Format("http://d.yimg.com/img.music.yahoo.com/image/v1/station/video/category/{0}?fallback=fast", StationtId);
      else
        return string.Format("http://d.yimg.com/img.music.yahoo.com/image/v1/station/video/category/{0}?fallback=fast&{1}", StationtId, param);
    }

    public string GetArtistImage(string ArtistId, int height )
    {
     return GetArtistImage(ArtistId, "size=" + height.ToString());
    }

    public string GetArtistImage(string ArtistId, string param)
    {
      if (string.IsNullOrEmpty(param))
        return string.Format("http://d.yimg.com/img.music.yahoo.com/image/v1/artist/{0}?fallback=fast", ArtistId);
      else
        return string.Format("http://d.yimg.com/img.music.yahoo.com/image/v1/artist/{0}?fallback=fast&{1}", ArtistId, param);
    }

    public string GetVideoImage(string VideoId, int height)
    {
      return GetArtistImage(VideoId, "size=" + height.ToString());
    }

    public string GetVideoImage(string VideoId, string param)
    {
      if (string.IsNullOrEmpty(param))
        return string.Format("http://d.yimg.com/img.music.yahoo.com/image/v1/video/{0}", VideoId);
      else
        return string.Format("http://d.yimg.com/img.music.yahoo.com/image/v1/video/{0}?{1}", VideoId, param);
    }

    #endregion
  }
}
