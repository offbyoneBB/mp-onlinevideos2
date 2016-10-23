using DirectShow.Helper;
using InputStreamSourceFilter;
using MediaPortal.UI.Players.Video;
using MediaPortalWrapper;

namespace MediaPortal.UI.Players.InputStreamPlayer
{
  public class InputStreamPlayer : VideoPlayer
  {
    private InputStream _stream;
    private StreamSourceFilter _streamSourceFilter;

    /// <summary>
    /// Indicates that internal decryption failed.
    /// </summary>
    public bool DecryptError
    {
      get { return _streamSourceFilter != null && _streamSourceFilter.DecryptError; }
    }

    public void InitStream(InputStream onlineSource)
    {
      _stream = onlineSource;
    }

    protected override void AddSourceFilter()
    {
      _streamSourceFilter = new StreamSourceFilter(_stream);
      var hr = _graphBuilder.AddFilter(_streamSourceFilter, _streamSourceFilter.Name);
      new HRESULT(hr).Throw();

      using (DSFilter source2 = new DSFilter(_streamSourceFilter))
        foreach (DSPin pin in source2.Output)
          using (pin)
          {
            hr = pin.Render();
          }
    }
  }
}
