using System;
using System.Collections.Generic;
using System.Linq;
using DirectShow;
using DirectShow.BaseClasses;
using DirectShow.Helper;
using MediaPortalWrapper.Streams;
using System.Runtime.InteropServices;
using InputStreamSourceFilter.Extensions;
using MediaPortalWrapper;
using MediaPortalWrapper.NativeWrappers;
using MediaPortalWrapper.Utils;

namespace InputStreamSourceFilter
{
  /// <summary>
  /// Source Filter that parses an AbstractStream containing elementary video and audio streams.
  /// Currently the supported media types are hard coded to the media types of the sample data.
  /// </summary>
  public class StreamSourceFilter : BaseSourceFilterTemplate<StreamFileParser>
  {
    protected StreamFileParser _streamParser;

    /// <summary>
    /// Indicates that internal decryption failed.
    /// </summary>
    public bool DecryptError
    {
      get { return _streamParser != null && _streamParser.DecryptError; }
    }

    public StreamSourceFilter(InputStream stream)
      : base("InputStreamSourceFilter")
    {
      _streamParser = (StreamFileParser)m_Parsers[0];
      _streamParser.SetSource(stream);
      _streamParser.StreamChanged = OnStreamChanged;
      m_sFileName = "http://localhost/InputStream";
      //Load a dummy file
      Load(m_sFileName, null);
    }

    private void OnStreamChanged()
    {
      // TODO: implement media type update of pin during playback
      //var pin = Pins.OfType<SplitterOutputPin>().FirstOrDefault(p => p.Track.Type == DemuxTrack.TrackType.Video);
      //if (pin != null)
      //{
      //  AMMediaType mt;
      //  if (MediaTypeBuilder.TryGetType(_streamParser.InputStream.VideoStream, out mt))
      //    _streamParser.Tracks[0].SetStreamMediaType(mt);
      //  var res = RenameOutputPin(pin, 0, 0);
      //}
    }

    public override int Pause()
    {
      var result = base.Pause();
      _streamParser.PauseDemux = true;
      return result;
    }

    public override int Run(long tStart)
    {
      var result = base.Run(tStart);
      _streamParser.PauseDemux = false;
      return result;
    }

    public override int GetState(int dwMilliSecsTimeout, out FilterState filtState)
    {
      base.GetState(dwMilliSecsTimeout, out filtState);
      // This tells the graph no to cue up samples in pause state. We do this here to prevent background downloading.
      return 0x00040268 /*VFW_S_CANT_CUE*/;
      //return S_OK;
    }

    #region IAMStreamSelect Members

    public override int Count(out int pcStreams)
    {
      pcStreams = 0;
      if (m_pFileParser == null) return VFW_E_NOT_CONNECTED;
      pcStreams = _streamParser.SelectableTracks.Count;
      return NOERROR;
    }

    public override int Info(int lIndex, IntPtr ppmt, IntPtr pdwFlags, IntPtr plcid, IntPtr pdwGroup, IntPtr ppszName, IntPtr ppObject, IntPtr ppUnk)
    {
      if (lIndex >= _streamParser.SelectableTracks.Count)
        return S_FALSE;

      var selected = _streamParser.SelectableTracks[lIndex];

      if (ppmt != IntPtr.Zero)
      {
        AMMediaType mt;
        if (MediaTypeBuilder.TryGetType(selected, out mt))
        {
          IntPtr pmt = Marshal.AllocCoTaskMem(Marshal.SizeOf(mt));
          Marshal.StructureToPtr(mt, pmt, true);
          Marshal.WriteIntPtr(ppmt, pmt);
        }
        else
        {
          Marshal.WriteIntPtr(ppmt, IntPtr.Zero);
        }
      }
      if (pdwFlags != IntPtr.Zero)
      {
        var enabled = (int)(selected.StreamId == _streamParser.InputStream.AudioStream.StreamId ?
          AMStreamSelectInfoFlags.Enabled :
          AMStreamSelectInfoFlags.Disabled);

        Marshal.WriteInt32(pdwFlags, enabled);
      }
      if (plcid != IntPtr.Zero)
      {
        int lcid = selected.Language.TryGetLCID();
        if (lcid == 0)
          lcid = LOCALE_NEUTRAL;

        Marshal.WriteInt32(plcid, lcid);
      }
      if (pdwGroup != IntPtr.Zero)
      {
        if (selected.StreamType == StreamType.Audio)
          Marshal.WriteInt32(pdwGroup, 1);
        else if (selected.StreamType == StreamType.Subtitle)
          Marshal.WriteInt32(pdwGroup, 2);
        else
          Marshal.WriteInt32(pdwGroup, 0);
      }
      if (ppszName != IntPtr.Zero)
      {
        var culture = selected.Language.FromISOName();
        string name = culture != null ? culture.DisplayName : selected.Language;
        if (string.IsNullOrEmpty(name))
          name = "Audio #" + lIndex;

        Marshal.WriteIntPtr(ppszName, Marshal.StringToCoTaskMemUni(name));
      }
      if (ppObject != IntPtr.Zero)
      {
        Marshal.WriteIntPtr(ppObject, Marshal.GetIUnknownForObject(selected));
      }
      if (ppUnk != IntPtr.Zero)
      {
        Marshal.WriteIntPtr(ppUnk, IntPtr.Zero);
      }
      return NOERROR;
    }

    public override int Enable(int lIndex, AMStreamSelectEnableFlags dwFlags)
    {
      bool changed = false;
      uint oldAudioStream = _streamParser.InputStream.AudioStream.StreamId;
      for (int index = 0; index < _streamParser.SelectableTracks.Count; index++)
      {
        var track = _streamParser.SelectableTracks[index];

        bool isEnabled = (
                           index == lIndex && dwFlags == AMStreamSelectEnableFlags.Enable || // the current index should be enabled
                           dwFlags == AMStreamSelectEnableFlags.EnableAll // all should be enabled
                         ) && dwFlags != AMStreamSelectEnableFlags.DisableAll; // must not be "Disable All"

        changed |= _streamParser.InputStream.EnableStream((int)track.StreamId, isEnabled);
      }
      uint newAudioStream = _streamParser.InputStream.AudioStream.StreamId;

      if (!changed)
        return NOERROR;

      // Update output pin
      var audioPin = Pins.OfType<SplitterOutputPin>().FirstOrDefault(p => p.Track.Type == DemuxTrack.TrackType.Audio);
      if (audioPin != null)
      {
        AMMediaType mt;
        if (MediaTypeBuilder.TryGetType(_streamParser.InputStream.AudioStream, out mt))
          _streamParser.Tracks[1].SetStreamMediaType(mt);
        var res = RenameOutputPin(audioPin, oldAudioStream, newAudioStream);
      }

      if (IsActive && dwFlags != AMStreamSelectEnableFlags.DisableAll)
      {
        try
        {
          IMediaSeeking seeking = (IMediaSeeking)FilterGraph;
          if (seeking != null)
          {
            long current;
            seeking.GetCurrentPosition(out current);
            // Only seek during playback, not on initial selection
            if (current != 0)
            {
              current -= UNITS / 10;
              seeking.SetPositions(current, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
              current += UNITS / 10;
              seeking.SetPositions(current, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
            }
          }
        }
        catch
        {
        }
      }
      return NOERROR;
    }

    private HRESULT RenameOutputPin(SplitterOutputPin pPin, uint oldStreamId, uint newStreamId)
    {
      // Output Pin was found
      // Stop the Graph, remove the old filter, render the graph again, start it up again
      // This only works on pins that were connected before, or the filter graph could .. well, break
      if (pPin != null && pPin.IsConnected)
      {
        Logger.Info("RenameOutputPin() - Switching {0} Stream {1} to {2}", pPin.Name, oldStreamId, newStreamId);

        IMediaControl pControl = (IMediaControl)FilterGraph;

        FilterState oldState;
        // Get the graph state
        // If the graph is in transition, we'll get the next state, not the previous
        var hr = (HRESULT)pControl.GetState(10, out oldState);

        // Stop the filter graph
        hr = (HRESULT)pControl.Stop();

        // Audio Filters get their connected filter removed
        // This way we make sure we reconnect to the proper filter
        // Other filters just disconnect and try to reconnect later on
        PinInfo pInfo;
        IPinImpl connectedPin = pPin.Connected;
        hr = (HRESULT)connectedPin.QueryPinInfo(out pInfo);

        // Update Output Pin
        AMMediaType pmt;
        if (MediaTypeBuilder.TryGetType(_streamParser.InputStream.AudioStream, out pmt))
          pPin.SetMediaType(pmt);

        int mtIdx = connectedPin.QueryAccept(pmt);
        bool bMediaTypeFound = (mtIdx >= 0);

        if (pInfo.filter != null)
        {
          bool bRemoveFilter = !bMediaTypeFound;
          if (bRemoveFilter)
          {
            hr = (HRESULT)FilterGraph.RemoveFilter(pInfo.filter);
            // Use IGraphBuilder to rebuild the graph
            IGraphBuilder pGraphBuilder = (IGraphBuilder)FilterGraph;
            // Instruct the GraphBuilder to connect us again
            hr = (HRESULT)pGraphBuilder.Render(pPin);
          }
          else
          {
            hr = (HRESULT)ReconnectPin(pPin, pmt);
          }

          pPin.SetMediaType(pmt);
        }

        // Re-start the graph
        if (oldState == FilterState.Paused)
        {
          hr = (HRESULT)pControl.Pause();
        }
        else if (oldState == FilterState.Running)
        {
          hr = (HRESULT)pControl.Run();
        }
        return hr;
      }
      return E_FAIL;
    }

    #endregion
  }

  public class StreamFileParser : FileParser
  {
    protected InputStream _stream;
    private int _videoPackets;
    private int _audioPackets;

    public List<MediaTypedDemuxTrack> Tracks { get { return m_Tracks.OfType<MediaTypedDemuxTrack>().ToList(); } }
    public List<InputstreamInfo> SelectableTracks { get { return _stream.AudioStreams; } }
    public InputStream InputStream { get { return _stream; } }
    /// <summary>
    /// Indicates decoding errors. Detection considers missing video samples while audio samples are retrieved correctly.
    /// </summary>
    public bool DecryptError { get; private set; }
    /// <summary>
    /// Indicates if the parser should pause sample demuxing.
    /// </summary>
    public bool PauseDemux { get; set; }
    /// <summary>
    /// Callback for stream change events.
    /// </summary>
    public Action StreamChanged { get; set; }

    public void SetSource(InputStream stream)
    {
      _stream = stream;
    }

    protected override HRESULT CheckFile()
    {
      //We loaded a dummy file, just return OK
      return S_OK;
    }

    protected override HRESULT LoadTracks()
    {
      // Check if required extradata is present, if not continue packet processing until they are filled
      if (_stream.VideoStream.StreamId == 0)
      {
        DecryptError = true;
        return S_FALSE;
      }

      while (_stream.VideoStream.ExtraSize == 0)
      {
        DemuxPacketWrapper demuxPacket = _stream.Read();

        // EOS
        if (demuxPacket.IsEOS)
          return S_FALSE;
      }

      //Initialise the tracks, these create our output pins
      AMMediaType mediaType;
      if (MediaTypeBuilder.TryGetType(_stream.VideoStream, out mediaType))
        m_Tracks.Add(new MediaTypedDemuxTrack(this, DemuxTrack.TrackType.Video, mediaType));

      if (MediaTypeBuilder.TryGetType(_stream.AudioStream, out mediaType))
        m_Tracks.Add(new MediaTypedDemuxTrack(this, DemuxTrack.TrackType.Audio, mediaType) { LCID = _stream.AudioStream.Language.TryGetLCID() });

      m_rtDuration = _stream.Functions.GetTotalTime().ToDS();
      return S_OK;
    }

    //This is called by a separate thread repeatedly to fill each tracks packet cache
    public override HRESULT ProcessDemuxPackets()
    {
      // Avoid streaming downloads in pause mode
      if (PauseDemux)
        return S_OK;

      using (var demuxPacketWrapper = _stream.Read())
      {
        // EOS
        if (demuxPacketWrapper.IsEOS)
          return S_FALSE;

        DemuxPacket demuxPacket = demuxPacketWrapper.DemuxPacket;

        // Stream changes
        if (demuxPacket.StreamId == Constants.DMX_SPECIALID_STREAMCHANGE ||
            demuxPacket.StreamId == Constants.DMX_SPECIALID_STREAMINFO)
        {
          var action = StreamChanged;
          if (action != null)
            action();
        }

        if (demuxPacket.Data == IntPtr.Zero)
          return S_OK;

        //Create the packet and add the data
        var isVideo = demuxPacket.StreamId == _stream.VideoStream.StreamId;
        PacketData packet = new DemuxPacketData(demuxPacket, isVideo);

        DemuxTrack track = m_Tracks.FirstOrDefault(t => t.Type == (isVideo ? DemuxTrack.TrackType.Video : DemuxTrack.TrackType.Audio));
        if (isVideo)
          _videoPackets++;
        else
          _audioPackets++;

        // Queue samples
        if (track != null)
          track.AddToCache(ref packet);
      }

      // Check for decoding errors, commonly the audio part is working while video decoding might fail
      if (_audioPackets > 5 && _videoPackets == 0)
      {
        DecryptError = true;
        return S_FALSE;
      }
      return S_OK;
    }

    public override HRESULT SeekToTime(long time)
    {
      double startPts = 0d;
      _stream.Functions.DemuxSeekTime(time.ToMS(), false, ref startPts);
      return base.SeekToTime(time);
    }
  }

  /// <summary>
  /// Generic DemuxTrack that has a specified media type
  /// </summary>
  public class MediaTypedDemuxTrack : DemuxTrack
  {
    private AMMediaType _pmt;

    // Updates stored media type
    public void SetStreamMediaType(AMMediaType pmt)
    {
      _pmt = pmt;
    }

    public MediaTypedDemuxTrack(FileParser parser, TrackType type, AMMediaType pmt)
      : base(parser, type)
    {
      _pmt = pmt;
    }

    public override HRESULT GetMediaType(int iPosition, ref AMMediaType pmt)
    {
      if (iPosition == 0)
      {
        pmt.Set(_pmt);
        return NOERROR;
      }
      return VFW_S_NO_MORE_ITEMS;
    }
  }
}
