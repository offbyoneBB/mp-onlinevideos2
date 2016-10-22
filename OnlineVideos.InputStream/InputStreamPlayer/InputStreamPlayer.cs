using DirectShow.Helper;
using InputStreamSourceFilter;
using MediaPortal.UI.Players.Video;
using MediaPortalWrapper;

namespace MediaPortal.UI.Players.InputStreamPlayer
{
  public class InputStreamPlayer : VideoPlayer
  {
    private InputStream _stream;

    public void InitStream(InputStream onlineSource)
    {
      _stream = onlineSource;
    }

    protected override void AddSourceFilter()
    {
      var sourceFilter = new StreamSourceFilter(_stream);
      var hr = _graphBuilder.AddFilter(sourceFilter, sourceFilter.Name);
      new HRESULT(hr).Throw();

      using (DSFilter source2 = new DSFilter(sourceFilter))
        foreach (DSPin pin in source2.Output)
          using (pin)
          {
            hr = pin.Render();
          }
    }
  }
}
