using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MediaPortalWrapper.Filesystem
{
  public enum CURLOPTIONTYPE
  {
    CURL_OPTION_OPTION,     /**< Set a general option   */
    CURL_OPTION_PROTOCOL,   /**< Set a protocol option  */
    CURL_OPTION_CREDENTIALS,/**< Set User and password  */
    CURL_OPTION_HEADER      /**< Add a Header           */
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
    public virtual double DownloadSpeed { get { return 0; } }
  }
}
