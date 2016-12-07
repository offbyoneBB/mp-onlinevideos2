using System;
using System.IO;
using System.Linq;
using DirectShow.Helper;
using InputStreamSourceFilter;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.Players.Video.Subtitles;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortalWrapper;

namespace MediaPortal.UI.Players.InputStreamPlayer
{
  public class InputStreamPlayer : VideoPlayer
  {
    private InputStream _stream;
    protected StreamSourceFilter _streamSourceFilter;

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

      RenderSourceFilterPins();
    }

    protected override void AddAudioRenderer()
    {
      if (_stream.AudioStream.StreamId != 0)
        base.AddAudioRenderer();
    }

    protected virtual void RenderSourceFilterPins()
    {
      int hr;
      using (DSFilter source2 = new DSFilter(_streamSourceFilter))
        foreach (DSPin pin in source2.Output)
          using (pin)
          {
            hr = pin.Render();
          }
    }

    protected override void AddSubtitleFilter(bool isSourceFilterPresent)
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();
      int preferredSubtitleLcid = settings.PreferredSubtitleLanguage;

      ServiceRegistration.Get<ILogger>().Debug("{0}: Adding MPC-HC subtitle engine", PlayerTitle);
      SubtitleStyle defStyle = new SubtitleStyle();
      defStyle.Load();
      MpcSubtitles.SetDefaultStyle(ref defStyle, false);

      IntPtr upDevice = SkinContext.Device.NativePointer;
      string filename = string.Empty;

      string paths;
      if (GetSubtitlePath(out paths, out filename))
      {
        MpcSubtitles.LoadSubtitles(upDevice, _displaySize, filename, _graphBuilder, paths, preferredSubtitleLcid);
        MpcSubtitles.SetEnable(settings.EnableSubtitles);
      }
    }

    protected bool GetSubtitlePath(out string paths, out string filename)
    {
      filename = _stream.FakeFilename;
      paths = null;
      if (_stream.SubtitlePaths.Any())
      {
        paths = string.Join(",", _stream.SubtitlePaths.Select(Path.GetDirectoryName).Distinct().ToArray());
        return true;
      }
      return false;
    }

    protected override void FreeCodecs()
    {
      base.FreeCodecs();
      if (_stream != null)
        _stream.Dispose();
    }
  }
}
