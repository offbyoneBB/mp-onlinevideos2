using DirectShow;
using DirectShow.Helper;
using MediaPortal.UI.Players.Video.Tools;

namespace MediaPortal.UI.Players.InputStreamPlayer
{
  public class InputStreamDownloader : InputStreamPlayer
  {
    private readonly string _targetFilename;

    public InputStreamDownloader(string targetFilename)
    {
      _targetFilename = targetFilename;
    }

    protected override void AddSourceFilter()
    {
      base.AddSourceFilter();
      //var muxer = new CodecInfo { CLSID = "{E4B7FAF9-9CFF-4FD5-AC16-0F250F5F97B7}", Name = "SolveigMM Matroska Muxer" };
      var muxer = new CodecInfo { CLSID = "{A28F324B-DDC5-4999-AA25-D3A7E25EF7A8}", Name = "Haali Matroska Muxer" };
      IBaseFilter muxerFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, muxer.Name);

      // FileSink can be supported by muxer directly (Haali), or we need an additional file sink
      var fileSink = muxerFilter as IFileSinkFilter;
      if (fileSink == null)
      {
        var writer = new CodecInfo { CLSID = "{8596E5F0-0DA5-11D0-BD21-00A0C911CE86}", Name = "File writer" };
        fileSink = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, writer.Name) as IFileSinkFilter;
      }

      if (fileSink != null)
        fileSink.SetFileName(_targetFilename, null);

      using (DSFilter sourceFilter = new DSFilter(_streamSourceFilter))
      using (DSFilter muxerF = new DSFilter(muxerFilter))
      {
        for (int index = 0; index < sourceFilter.Output.Count; index++)
        {
          DSPin pin = sourceFilter.Output[index];
          using (pin)
          {
            if (index < muxerF.Input.Count)
              pin.Connect(muxerF.Input[index]);
          }
        }

        if (muxerF.OutputPin != null)
        {
          using (DSFilter writerF = new DSFilter((IBaseFilter)fileSink))
            muxerF.OutputPin.Connect(writerF.InputPin);
        }
      }
    }

    protected override void RenderSourceFilterPins()
    {
      // We connect them manually above
    }

    protected override void AddAudioRenderer()
    {
      // Avoid adding not needed filters here
    }

    protected override void AddPresenter()
    {
      // Avoid adding not needed filters here
    }

    protected override void AddSubtitleFilter(bool isSourceFilterPresent)
    {
      // Not required for download
    }
  }
}
