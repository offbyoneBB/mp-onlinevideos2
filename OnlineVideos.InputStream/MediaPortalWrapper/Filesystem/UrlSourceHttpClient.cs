// Define this symbol to force proxy (fiddler, localhost:8888)
//#define proxy

using System;
using System.Collections.Generic;
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

    // Reusing HttpClient to improve performance and sockets usage. See http://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/.
    private static readonly HttpClient _client;
    private static readonly HttpClientHandler _handler;

    private readonly Dictionary<string, string> _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    static UrlSourceHttpClient()
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

      // Ignore some non-http headers
      if (cname.Equals("seekable", StringComparison.OrdinalIgnoreCase))
        return true;

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
            _headers.Add(cname, value);
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

        var message = _postData != null ?
          new HttpRequestMessage(HttpMethod.Post, _url) { Content = new ByteArrayContent(_postData) } :
          new HttpRequestMessage(HttpMethod.Get, _url);

        foreach (var header in _headers)
          message.Headers.TryAddWithoutValidation(header.Key, header.Value);

        HttpResponseMessage result = _client.SendAsync(message).Result;
        _contentStream = result.Content.ReadAsStreamAsync().Result;
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
