// Define this symbol to force proxy (fiddler, localhost:8888)
//#define proxy

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using MediaPortalWrapper.Utils;

namespace MediaPortalWrapper.Filesystem
{
  public class UrlSourceHttpClient : AbstractFile
  {
    private Uri _url;
    private byte[] _postData;
    private readonly Stopwatch _sw = new Stopwatch();
    private double _speed = 0;

    private readonly HttpClient _client;
    private readonly HttpClientHandler _handler;

    public UrlSourceHttpClient()
    {
      _handler = new HttpClientHandler
      {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        UseCookies = false, /* Note: although this sounds wrong, it allows to manually set cookies via header. Otherwise only cookie containers can be used */
#if proxy
        Proxy = new WebProxy(new Uri("http://127.0.0.1:8888"), false),
#endif
      };
      _client = new HttpClient(_handler, true);
    }

    public override void Dispose()
    {
      base.Dispose();
      if (_client != null)
        _client.Dispose();
      if (_handler != null)
        _handler.Dispose();
    }

    public override bool UrlCreate(string url)
    {
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
      // Correct spelling of some names
      if (cname.Equals("AcceptEncoding", StringComparison.OrdinalIgnoreCase))
        cname = "Accept-Encoding";

      switch (type)
      {
        case CURLOPTIONTYPE.CURL_OPTION_CREDENTIALS:
          _handler.Credentials = new NetworkCredential(cname, value);
          break;
        case CURLOPTIONTYPE.CURL_OPTION_PROTOCOL:
        case CURLOPTIONTYPE.CURL_OPTION_HEADER:
          if (cname.Equals("postdata", StringComparison.OrdinalIgnoreCase))
            _postData = Convert.FromBase64String(value);
          else
            _client.DefaultRequestHeaders.TryAddWithoutValidation(cname, value);
          break;
        case CURLOPTIONTYPE.CURL_OPTION_OPTION:
          break;
        default:
          return false;
      }
      return true;
    }

    public override bool Open(uint flags)
    {
      try
      {
        _sw.Start();
        if (_postData != null)
        {
          ByteArrayContent content = new ByteArrayContent(_postData);
          HttpResponseMessage result = _client.PostAsync(_url, content).Result;
          _contentStream = result.Content.ReadAsStreamAsync().Result;
        }
        else
        {
          _contentStream = _client.GetStreamAsync(_url).Result;
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
