// Define this symbol to force proxy (fiddler, localhost:8888)
//#define proxy

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CurlSharp;
using MediaPortalWrapper.Utils;

namespace MediaPortalWrapper.Filesystem
{
  public enum CURLOPTIONTYPE
  {
    CURL_OPTION_OPTION,     /**< Set a general option   */
    CURL_OPTION_PROTOCOL,   /**< Set a protocol option  */
    CURL_OPTION_CREDENTIALS,/**< Set User and password  */
    CURL_OPTION_HEADER      /**< Add a Header           */
  }

  public class UrlSource : AbstractFile
  {
    private Uri _url;
    private byte[] _postData;
    private readonly Stopwatch _sw = new Stopwatch();
    private double _speed = 0;
    private readonly CurlEasy _curl;

    private string _rawUrl;
    private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

    public UrlSource()
    {
      Curl.GlobalInit(CurlInitFlag.All);
      _curl = new CurlEasy();
#if proxy
      _curl.Proxy = "127.0.0.1:8888";
#endif
    }

    public override void Dispose()
    {
      base.Dispose();
      _curl.Dispose();
    }

    public override bool UrlCreate(string url)
    {
      _rawUrl = url;
      return Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _url);
    }

    public override double DownloadSpeed
    {
      get { return _speed; }
    }

    public override int Read(IntPtr buffer, int bufferSize)
    {
      int numRead = base.Read(buffer, bufferSize);
      if (numRead == 0 && _speed == 0)
      {
        _speed = (int)(_totalBytesRead / _sw.Elapsed.TotalSeconds);
        _sw.Stop();
      }
      return numRead;
    }

    public override bool AddOption(CURLOPTIONTYPE type, string cname, string value)
    {
      string name = cname.ToLowerInvariant();
      switch (type)
      {
        case CURLOPTIONTYPE.CURL_OPTION_CREDENTIALS:
          _curl.SetOpt(CurlOption.Username, name);
          _curl.SetOpt(CurlOption.UserPwd, value);
          break;
        case CURLOPTIONTYPE.CURL_OPTION_PROTOCOL:
        case CURLOPTIONTYPE.CURL_OPTION_HEADER:
          if (name == "referer")
            _curl.Referer = value;
          else if (name == "user-agent")
            _curl.UserAgent = value;
          else if (name == "cookie")
            _curl.Cookie = value;
          else if (name == "postdata")
            _postData = Convert.FromBase64String(value);
          else if (name == "accept" || name == "accept-language" || name == "accept-datetime" || name == "accept-charset" ||
                   name == "authorization" || name == "cache-control" || name == "connection" || name == "content-md5" ||
                   name == "content-type" ||
                   name == "date" || name == "expect" || name == "forwarded" || name == "from" || name == "if-match" ||
                   name == "if-modified-since" || name == "if-none-match" || name == "if-range" ||
                   name == "if-unmodified-since" || name == "max-forwards" ||
                   name == "origin" || name == "pragma" || name == "range" || name == "te" || name == "upgrade" ||
                   name == "via" || name == "warning" || name == "x-requested-with" || name == "dnt" ||
                   name == "x-forwarded-for" || name == "x-forwarded-host" ||
                   name == "x-forwarded-proto" || name == "front-end-https" || name == "x-http-method-override" ||
                   name == "x-att-deviceid" ||
                   name == "x-wap-profile" || name == "x-uidh" || name == "x-csrf-token" || name == "x-request-id" ||
                   name == "x-correlation-id")
          {
            _headers[cname] = value;
          }
          //SetRequestHeader(it->first, value);
          //if (name == "authorization")
          //  CLog::Logger.Log(LOGDEBUG, "CurlFile::ParseAndCorrectUrl() adding custom header option '%s: ***********'", it->first.c_str());
          //else
          //  CLog::Logger.Log(LOGDEBUG, "CurlFile::ParseAndCorrectUrl() adding custom header option '%s: %s'", it->first.c_str(), value.c_str());

          //if (name == "seekable" && value == "0")
          //  _seekable = false;
          if (name == "acceptencoding")
          {
            _headers["Accept-Encoding"] = value;
            _curl.Encoding = value;
          }
          break;
        case CURLOPTIONTYPE.CURL_OPTION_OPTION:
          {
            //_curl.SetOpt(name, value);
            break;
          }
        default:
          return false;
      }
      return true;

    }

    public override bool Open(uint flags)
    {
      try
      {
        if (_postData != null)
        {
          _contentStream = new MemoryStream();
          var data = Encoding.ASCII.GetString(_postData);

          _curl.WriteFunction = WriteData;
          _curl.WriteData = null;
          _curl.PostFields = data;
          _curl.PostFieldSize = data.Length;
          _curl.FollowLocation = true;
          _curl.Url = _rawUrl;
          _curl.Post = true;
          var asmFolder = Path.GetDirectoryName(GetType().Assembly.Location);
          _curl.CaInfo = Path.Combine(asmFolder, "curl-ca-bundle.crt");
          var headers = new CurlSlist();
          foreach (var header in _headers)
          {
            headers.Append(string.Format("{0}: {1}", header.Key, header.Value));
          }
          _curl.HttpHeader = headers;
          var code = _curl.Perform();
          _contentStream.Position = 0; // To allow reading from start
        }
        else
        {
          _sw.Start();
          _contentStream = new MemoryStream();
          _curl.SetOpt(CurlOption.Url, _rawUrl);
          _curl.WriteFunction = WriteData;
          var headers = new CurlSlist();
          foreach (var header in _headers)
          {
            headers.Append(string.Format("{0}: {1}", header.Key, header.Value));
          }
          _curl.HttpHeader = headers;
          _curl.Perform();
          _contentStream.Position = 0; // To allow reading from start
        }
        return _contentStream != null;
      }
      catch (Exception ex)
      {
        Logger.Log("UrlSource: Error opening url: {0} [{1}]", _url, ex);
        return false;
      }
    }

    private int WriteData(byte[] buf, int size, int nmemb, object extradata)
    {
      _contentStream.Write(buf, 0, buf.Length);
      return buf.Length;
    }
  }
}
