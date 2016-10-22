using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using KodiAddon;

namespace libXBMC_addon
{
  public enum CURLOPTIONTYPE
  {
    CURL_OPTION_OPTION,     /**< Set a general option   */
    CURL_OPTION_PROTOCOL,   /**< Set a protocol option  */
    CURL_OPTION_CREDENTIALS,/**< Set User and password  */
    CURL_OPTION_HEADER      /**< Add a Header           */
  }

  public static class FileFactory
  {
    private static readonly IDictionary<IntPtr, AbstractFile> _files = new Dictionary<IntPtr, AbstractFile>();
    private static readonly object _fileIndexLock = new object();
    private static IntPtr _nextFileIndex = new IntPtr(1);

    public static IntPtr AddFile(AbstractFile file)
    {
      lock (_fileIndexLock)
      {
        IntPtr index = _nextFileIndex;
        _files.Add(index, file);
        _nextFileIndex = IntPtr.Add(_nextFileIndex, 1);
        return index;
      }
    }

    public static bool TryGetValue(IntPtr key, out AbstractFile file)
    {
      lock (_fileIndexLock)
      {
        return _files.TryGetValue(key, out file) && file != null;
      }
    }

    public static bool Remove(IntPtr key)
    {
      lock (_fileIndexLock)
      {
        AbstractFile file;
        if (TryGetValue(key, out file))
        {
          file.Dispose();
          _files.Remove(key);
          return true;
        }
        return false;
      }
    }
  }

  public abstract class AbstractFile : IDisposable
  {
    protected Stream _contentStream;
    protected string _sourceLocation;
    protected long _totalBytesRead;

    public virtual bool UrlCreate(string url)
    {
      _sourceLocation = url;
      return true;
    }

    public virtual void Dispose()
    {
      if (_contentStream != null)
        _contentStream.Dispose();
    }

    public long TotalBytesRead { get { return _totalBytesRead; } }

    public virtual int Read(IntPtr buffer, int bufferSize)
    {
      if (_contentStream == null)
        return -1;
      int numRead;
      byte[] localBuffer = new byte[bufferSize];
      if ((numRead = _contentStream.Read(localBuffer, 0, bufferSize)) > 0)
      {
        //var str = Encoding.UTF8.GetString(localBuffer);
        Marshal.Copy(localBuffer, 0, buffer, numRead);
        _totalBytesRead += numRead;
      }
      return numRead;
    }

    public abstract bool Open(uint flags);
    public abstract bool AddOption(CURLOPTIONTYPE type, string name, string value);
  }

  public class UrlSource : AbstractFile
  {
    private Uri _url;
    private readonly CompressionWebClient _client;
    private byte[] _postData;

    public UrlSource()
    {
      _client = new CompressionWebClient();
    }

    public override void Dispose()
    {
      base.Dispose();
      if (_client != null)
        _client.Dispose();
    }

    public override bool UrlCreate(string url)
    {
      Logger.Log("File.UrlCreate: {0}", url);
      return Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _url);
    }

    public override bool AddOption(CURLOPTIONTYPE type, string cname, string value)
    {
      string name = cname.ToLowerInvariant();
      switch (type)
      {
        case CURLOPTIONTYPE.CURL_OPTION_CREDENTIALS:
          {
            //m_curl.SetUserName(name);
            //m_curl.SetPassword(value);
            break;
          }
        case CURLOPTIONTYPE.CURL_OPTION_PROTOCOL:
        case CURLOPTIONTYPE.CURL_OPTION_HEADER:
          //if (name == "auth")
          //{
          //  if (!string.IsNullOrEmpty(value))
          //  _httpauth = value.ToLowerInvariant();
          //  else
          //  _httpauth = "any";
          //}
          //else if (name == "referer")
          //  SetReferer(value);
          //else if (name == "user-agent")
          //  SetUserAgent(value);
          //else if (name == "cookie")
          //  SetCookie(value);
          //else if (name == "acceptencoding" || name == "encoding")
          //  SetAcceptEncoding(value);
          //else if (name == "noshout" && value == "true")
          //  m_skipshout = true;
          //else if (name == "seekable" && value == "0")
          //  m_seekable = false;
          //else if (name == "accept-charset")
          //  SetAcceptCharset(value);
          //else if (name == "sslcipherlist")
          //  m_cipherlist = value;
          //else if (name == "connection-timeout")
          //  m_connecttimeout = strtol(value.c_str(), NULL, 10);
          //else 
          if (name == "postdata")
          {
            _postData = Convert.FromBase64String(value);
            //_postData = Encoding.UTF8.GetString(Convert.FromBase64String(value));
          }
          //// other standard headers (see https://en.wikipedia.org/wiki/List_of_HTTP_header_fields)
          else if (name == "accept" || name == "accept-language" || name == "accept-datetime" ||
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
            _client.Headers.Add(cname, value);
          }
          //SetRequestHeader(it->first, value);
          //if (name == "authorization")
          //  CLog::Logger.Log(LOGDEBUG, "CurlFile::ParseAndCorrectUrl() adding custom header option '%s: ***********'", it->first.c_str());
          //else
          //  CLog::Logger.Log(LOGDEBUG, "CurlFile::ParseAndCorrectUrl() adding custom header option '%s: %s'", it->first.c_str(), value.c_str());

          //if (name == "seekable" && value == "0")
          //  _seekable = false;
          if (name == "acceptencoding")
            _client.Headers["Accept-Encoding"] = value;
          //m_curl.SetProtocolOption(name, value);
          break;
        case CURLOPTIONTYPE.CURL_OPTION_OPTION:
          {
            //m_curl.SetOption(name, value);
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
          var result = _client.UploadData(_url, _postData);
          _contentStream = new MemoryStream(result);
        }
        else
        {
          _contentStream = _client.OpenRead(_url);
        }
        return _contentStream != null;
      }
      catch (Exception ex)
      {
        Logger.Log("UrlSource: Error opening url: {0} [{1}]", _url, ex);
        return false;
      }
    }
  }
}
