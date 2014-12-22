using System;
using System.Windows.Forms;
using DirectShowLib;
using DShowNET.Helper;
using MediaPortal.Profile;

namespace OnlineVideos.MediaPortal1
{
    public class ConfigurationPlayer
    {
        public const int WMGraphNotify = 0x0400 + 13;

        protected IFilterGraph2 _graphBuilder;
        protected DsROTEntry _rotEntry;
        protected IMediaControl _mediaCtrl;
        public IMediaEventEx mediaEvents;
        protected IVideoWindow _videoWin;
        protected Control _parentControl;

        public bool Play(string fileName, Control parent, out string ErrorOrSplitter)
        {
            ErrorOrSplitter = "";
            int hr;
            _parentControl = parent;

            _graphBuilder = (IFilterGraph2)new FilterGraph();
            _rotEntry = new DsROTEntry(_graphBuilder);

            // add the video renderer (evr does not seem to work here)
            IBaseFilter vmr9Renderer = DirectShowUtil.AddFilterToGraph(_graphBuilder, "Video Mixing Renderer 9");
            ((IVMRAspectRatioControl9)vmr9Renderer).SetAspectRatioMode(VMRAspectRatioMode.LetterBox);
            DirectShowUtil.ReleaseComObject(vmr9Renderer, 2000);

            // add the audio renderer
            IBaseFilter audioRenderer = DirectShowUtil.AddAudioRendererToGraph(_graphBuilder, MPSettings.Instance.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device"), false);
            DirectShowUtil.ReleaseComObject(audioRenderer, 2000);

            // add the source filter
            string sourceFilterName = OnlineVideos.MediaPortal1.Player.OnlineVideosPlayer.GetSourceFilterName(fileName);
			if (string.IsNullOrEmpty(sourceFilterName)) return false;
            IBaseFilter sourceFilter = null;
            try
            {
                sourceFilter = DirectShowUtil.AddFilterToGraph(_graphBuilder, sourceFilterName);
            }
            catch (Exception ex)
            {
                ErrorOrSplitter = ex.Message;
                return false;
            }

            hr = ((IFileSourceFilter)sourceFilter).Load(fileName, null);

            if (hr != 0)
            {
                ErrorOrSplitter = DirectShowLib.DsError.GetErrorText(hr);
                DirectShowUtil.ReleaseComObject(sourceFilter, 2000);
                return false;
            }

            // wait for our filter to buffer before rendering the pins
            OnlineVideos.MPUrlSourceFilter.V2.IFilterState filterState = sourceFilter as OnlineVideos.MPUrlSourceFilter.V2.IFilterState;

            if (filterState != null)
            {
                bool ready = false;

                while ((!ready) && (hr == 0))
                {
                    hr = filterState.IsFilterReadyToConnectPins(out ready);

                    System.Threading.Thread.Sleep(25);
                }
            }

            if (hr != 0)
            {
                ErrorOrSplitter = DirectShowLib.DsError.GetErrorText(hr);
                DirectShowUtil.ReleaseComObject(sourceFilter, 2000);
                return false;
            }

            OnlineVideos.MediaPortal1.Player.OnlineVideosPlayer.AddPreferredFilters(_graphBuilder, sourceFilter);

            // try to connect the filters
            int numConnected = 0;
            IEnumPins pinEnum;
            hr = sourceFilter.EnumPins(out pinEnum);
            if ((hr == 0) && (pinEnum != null))
            {
                pinEnum.Reset();
                IPin[] pins = new IPin[1];
                int iFetched;
                int iPinNo = 0;
                do
                {
                    iPinNo++;
                    hr = pinEnum.Next(1, pins, out iFetched);
                    if (hr == 0)
                    {
                        if (iFetched == 1 && pins[0] != null)
                        {
                            PinDirection pinDir;
                            pins[0].QueryDirection(out pinDir);
                            if (pinDir == PinDirection.Output)
                            {
                                hr = _graphBuilder.Render(pins[0]);
								if (hr == 0)
								{
									numConnected++;
									IPin connectedPin;
									if (pins[0].ConnectedTo(out connectedPin) == 0 && connectedPin != null)
									{
										PinInfo connectedPinInfo;
										connectedPin.QueryPinInfo(out connectedPinInfo);
										FilterInfo connectedFilterInfo;
										connectedPinInfo.filter.QueryFilterInfo(out connectedFilterInfo);
										DirectShowUtil.ReleaseComObject(connectedPin, 2000);
										IBaseFilter connectedFilter;
										if (connectedFilterInfo.pGraph.FindFilterByName(connectedFilterInfo.achName, out connectedFilter) == 0 && connectedFilter != null)
										{
											var codecInfo = GetCodecInfo(connectedFilter, connectedFilterInfo.achName);
											if (codecInfo != null)
											{
												if (string.IsNullOrEmpty(ErrorOrSplitter)) ErrorOrSplitter = codecInfo.ToString();
												else ErrorOrSplitter += ", " + codecInfo.ToString();
											}
											DirectShowUtil.ReleaseComObject(connectedFilter);
										}
									}
								}
                            }
                            DirectShowUtil.ReleaseComObject(pins[0], 2000);
                        }
                    }
                } while (iFetched == 1);
            }
            DirectShowUtil.ReleaseComObject(pinEnum, 2000);

            if (numConnected > 0)
            {
                _videoWin = _graphBuilder as IVideoWindow;
                if (_videoWin != null)
                {
                    _videoWin.put_Owner(_parentControl.Handle);
                    _videoWin.put_WindowStyle((WindowStyle)((int)WindowStyle.Child + (int)WindowStyle.ClipSiblings + (int)WindowStyle.ClipChildren));
                    _videoWin.SetWindowPosition(_parentControl.ClientRectangle.X, _parentControl.ClientRectangle.Y, _parentControl.ClientRectangle.Width, _parentControl.ClientRectangle.Height);
                    _videoWin.put_Visible(OABool.True);
                }

                _mediaCtrl = (IMediaControl)_graphBuilder;
                hr = _mediaCtrl.Run();

                mediaEvents = (IMediaEventEx)_graphBuilder;
                // Have the graph signal event via window callbacks for performance
                mediaEvents.SetNotifyWindow(_parentControl.FindForm().Handle, WMGraphNotify, IntPtr.Zero);

                _parentControl.SizeChanged += _parentControl_SizeChanged;
                return true;
            }
            else
            {
                ErrorOrSplitter = string.Format("Could not render output pins of {0}", sourceFilterName);
                DirectShowUtil.ReleaseComObject(sourceFilter, 2000);
                Stop();
                return false;
            }
        }

        CodecConfiguration.Codec? GetCodecInfo(IFilterGraph graph, string filterName)
        {
            IBaseFilter filter;
            int hr = graph.FindFilterByName(filterName, out filter);
            CodecConfiguration.Codec? result = null;
            if (hr == 0 && filter != null)
            {
                result = GetCodecInfo(filter, filterName);
                DirectShowUtil.ReleaseComObject(filter, 2000);
            }
            return result;
        }

        CodecConfiguration.Codec? GetCodecInfo(IBaseFilter filter, string name)
        {
            Guid guid;
            filter.GetClassID(out guid);
            CodecConfiguration.Codec c = new CodecConfiguration.Codec() { CLSID = guid.ToString("B"), Name = name };
            CodecConfiguration.CheckCodec(ref c);
            return c;
        }

        void _parentControl_SizeChanged(object sender, EventArgs e)
        {
            _videoWin.SetWindowPosition(_parentControl.ClientRectangle.X, _parentControl.ClientRectangle.Y, _parentControl.ClientRectangle.Width, _parentControl.ClientRectangle.Height);
        }

        public void Stop()
        {
            if (_parentControl != null)
            {
                _parentControl.SizeChanged -= _parentControl_SizeChanged;
            }
            if (_videoWin != null)
            {
                _videoWin.put_Visible(OABool.False);
                _videoWin = null;
            }
            if (_mediaCtrl != null)
            {
                _mediaCtrl.Stop();
                _mediaCtrl = null;
            }
            if (_rotEntry != null)
            {
                _rotEntry.Dispose();
                _rotEntry = null;
            }
            if (_graphBuilder != null)
            {
                DirectShowUtil.ReleaseComObject(_graphBuilder, 2000);
                _graphBuilder = null;
            }
        }
    }
}