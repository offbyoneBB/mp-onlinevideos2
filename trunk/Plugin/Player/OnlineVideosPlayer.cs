using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DirectShowLib;
using DShowNET.Helper;
using MediaPortal.Configuration;
using MediaPortal.Player;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using System.Runtime.InteropServices;
using System.Drawing;

namespace OnlineVideos.Player
{
    public class OnlineVideosPlayer : VideoPlayerVMR9
    {
        public OnlineVideosPlayer()
            : base(g_Player.MediaType.Video)
        { }

        public OnlineVideosPlayer(g_Player.MediaType type)
            : base(type)
        { }

        protected override bool GetInterfaces()
        {
            string currentFileLower = CurrentFile.ToLower();

            if (currentFileLower.StartsWith("mms://") || currentFileLower.Contains(".asf"))
                return BuildGraphForMMS();
            else if (currentFileLower.StartsWith("rtsp://"))
                return BuildGraphForRTSP();
            else if (currentFileLower.EndsWith(".m4v") || currentFileLower.EndsWith(".mp4") || currentFileLower.EndsWith(".mov") || currentFileLower.EndsWith(".flv"))
                return BuildGraphWithFileSourceUrl();
            else
                return base.GetInterfaces();
        }

        bool BuildGraphForRTSP()
        {
            base.graphBuilder = (IGraphBuilder)new FilterGraph();

            // add video renderer
            Log.Info("VideoPlayerVMR9: Enabling DX9 exclusive mode", new object[0]);
            GUIMessage message = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SWITCH_FULL_WINDOWED, 0, 0, 0, 1, 0, null);
            GUIWindowManager.SendMessage(message);
            this.Vmr9 = new VMR9Util();
            this.Vmr9.AddVMR9(base.graphBuilder);
            this.Vmr9.Enable(false);

            // add the audio renderer
            using (Settings settings = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                string audiorenderer = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                DirectShowUtil.AddAudioRendererToGraph(base.graphBuilder, audiorenderer, false);
            }

            // add the source filter
            IBaseFilter sourceFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, "MediaPortal File Reader");
            if (sourceFilter == null) return false;
            int result = ((IFileSourceFilter)sourceFilter).Load(CurrentFile, null);
            if (result != 0) return false;

            IEnumPins enumPins;
            IPin[] sourceFilterPins = new IPin[2];
            int fetched;
            result = sourceFilter.EnumPins(out enumPins);
            result = enumPins.Next(2, sourceFilterPins, out fetched);
            // connect the pins automatically
            result = base.graphBuilder.Render(sourceFilterPins[0]); // audio
            result = base.graphBuilder.Render(sourceFilterPins[1]); // video

            // cleanup resources
            DirectShowUtil.ReleaseComObject(sourceFilter);
            DirectShowUtil.ReleaseComObject(enumPins);
            DirectShowUtil.ReleaseComObject(sourceFilterPins[0]);
            DirectShowUtil.ReleaseComObject(sourceFilterPins[1]);

            // set fields for playback
            base.mediaCtrl = (IMediaControl)base.graphBuilder;
            base.mediaEvt = (IMediaEventEx)base.graphBuilder;
            base.mediaSeek = (IMediaSeeking)base.graphBuilder;
            base.mediaPos = (IMediaPosition)base.graphBuilder;
            base.basicAudio = base.graphBuilder as IBasicAudio;
            DirectShowUtil.EnableDeInterlace(base.graphBuilder);
            base.m_iVideoWidth = this.Vmr9.VideoWidth;
            base.m_iVideoHeight = this.Vmr9.VideoHeight;

            if (!this.Vmr9.IsVMR9Connected)
            {
                base.mediaCtrl = null;
                CloseInterfaces();
                return false;
            }
            this.Vmr9.SetDeinterlaceMode();

            return true;
        }

        bool BuildGraphForMMS()
        {
            base.graphBuilder = (IGraphBuilder)new FilterGraph();

            Log.Info("VideoPlayerVMR9: Enabling DX9 exclusive mode", new object[0]);
            GUIMessage message = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SWITCH_FULL_WINDOWED, 0, 0, 0, 1, 0, null);
            GUIWindowManager.SendMessage(message);
            this.Vmr9 = new VMR9Util();
            this.Vmr9.AddVMR9(base.graphBuilder);
            this.Vmr9.Enable(false);

            // add the source filter manually
            IBaseFilter sourceFilter = DirectShowUtil.AddFilterToGraph(base.graphBuilder, "WM ASF Reader");

            if (sourceFilter == null)
            {
                Error.SetError("Unable to load DirectshowFilter: WM ASF Reader", "Windows Media Player not installed?");
                Log.Error("Unable to load DirectshowFilter: WM ASF Reader", new object[0]);
                return false;
            }

            // load the file with the source filter
            int result = ((IFileSourceFilter)sourceFilter).Load(CurrentFile, null);
            if (result != 0) return false;

            // add the audio renderer
            using (Settings settings = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                string audiorenderer = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                DirectShowUtil.AddAudioRendererToGraph(graphBuilder, audiorenderer, false);
            }

            // get the output pins of the WM ASF Reader
            IEnumPins enumPins;
            IPin[] sourceFilterPins = new IPin[2];
            int fetched;
            result = sourceFilter.EnumPins(out enumPins);
            result = enumPins.Next(2, sourceFilterPins, out fetched);
            // connect the pins automatically
            base.graphBuilder.Render(sourceFilterPins[0]);
            base.graphBuilder.Render(sourceFilterPins[1]);

            // cleanup resources
            DirectShowUtil.ReleaseComObject(sourceFilter);
            DirectShowUtil.ReleaseComObject(enumPins);
            DirectShowUtil.ReleaseComObject(sourceFilterPins[0]);
            DirectShowUtil.ReleaseComObject(sourceFilterPins[1]);

            // set fields for playback
            base.mediaCtrl = (IMediaControl)base.graphBuilder;
            base.mediaEvt = (IMediaEventEx)base.graphBuilder;
            base.mediaSeek = (IMediaSeeking)base.graphBuilder;
            base.mediaPos = (IMediaPosition)base.graphBuilder;
            base.basicAudio = base.graphBuilder as IBasicAudio;
            DirectShowUtil.EnableDeInterlace(base.graphBuilder);
            base.m_iVideoWidth = this.Vmr9.VideoWidth;
            base.m_iVideoHeight = this.Vmr9.VideoHeight;

            if (!this.Vmr9.IsVMR9Connected)
            {
                base.mediaCtrl = null;
                CloseInterfaces();
                return false;
            }
            this.Vmr9.SetDeinterlaceMode();
            return true;
        }

        bool BuildGraphWithFileSourceUrl()
        {            
            try
            {
                graphBuilder = (IGraphBuilder)new FilterGraph();

                // add the source filter manually
                IBaseFilter sourceFilter = DirectShowUtil.AddFilterToGraph(graphBuilder, "File Source (URL)");

                // load the url with the source filter                
                int result = ((IFileSourceFilter)sourceFilter).Load(CurrentFile, null);
                if (result != 0) return false;
                
                // buffer before starting playback
                try
                {
                    GUIWaitCursor.Init(); GUIWaitCursor.Show(); // init and show the wait cursor while buffering
                    long total = 0, current = 0, last = 0;
                    do
                    {
                        result = ((IAMOpenProgress)sourceFilter).QueryProgress(out total, out current);
                        GUIWindowManager.Process(); // keep GUI responsive
                        if (current - last >= (double)total * 0.01) // log every percent
                        {
                            Log.Debug("Buffering: {0}/{1} KB", current / 1024, total / 1024);
                            last = current;
                        }
                    }
                    while (current < (double)total * OnlineVideoSettings.Instance.playbuffer * 0.01);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                finally
                {
                    GUIWaitCursor.Hide(); // hide the wait cursor
                }

                // switch to directx fullscreen mode
                Log.Info("OnlineVideosPlayer: Enabling DX9 exclusive mode");
                GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SWITCH_FULL_WINDOWED, 0, 0, 0, 1, 0, null);
                GUIWindowManager.SendMessage(msg);

                // add the VMR9 in the graph
                // after enabling exclusive mode, if done first it causes MediPortal to minimize if for example the "Windows key" is pressed while playing a video
                Vmr9 = new VMR9Util();
                Vmr9.AddVMR9(graphBuilder);
                Vmr9.Enable(false);

                // add the audio renderer
                //using (Settings settings = new MPSettings()) // only available in 1.1+
                using (Settings settings = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    string audiorenderer = settings.GetValueAsString("movieplayer", "audiorenderer", "Default DirectSound Device");
                    DirectShowUtil.AddAudioRendererToGraph(graphBuilder, audiorenderer, false);
                }

                // get the output pin of the source filter
                IEnumPins enumPins;
                IPin[] sourceFilterPins = new IPin[1];
                int fetched;
                result = sourceFilter.EnumPins(out enumPins);
                result = enumPins.Next(1, sourceFilterPins, out fetched);

                // connect the pin automatically
                base.graphBuilder.Render(sourceFilterPins[0]);                

                // cleanup resources
                DirectShowUtil.ReleaseComObject(sourceFilterPins[0]);
                DirectShowUtil.ReleaseComObject(enumPins);
                DirectShowUtil.ReleaseComObject(sourceFilter);                                

                // set fields for playback
                mediaCtrl = (IMediaControl)graphBuilder;
                mediaEvt = (IMediaEventEx)graphBuilder;
                mediaSeek = (IMediaSeeking)graphBuilder;
                mediaPos = (IMediaPosition)graphBuilder;
                basicAudio = graphBuilder as IBasicAudio;
                DirectShowUtil.EnableDeInterlace(graphBuilder);
                m_iVideoWidth = Vmr9.VideoWidth;
                m_iVideoHeight = Vmr9.VideoHeight;

                if (!Vmr9.IsVMR9Connected)
                {
                    //VMR9 is not supported, switch to overlay
                    mediaCtrl = null;
                    CloseInterfaces();
                    return false;
                }
                Vmr9.SetDeinterlaceMode();
                return true;
            }
            catch (Exception ex)
            {
                Error.SetError("Unable to play movie", "Unable build graph for VMR9");
                Log.Error("OnlineVideosPlayer:exception while creating DShow graph {0} {1}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        System.Reflection.FieldInfo vmr9Field = null;
        private VMR9Util Vmr9
        {
            get
            {
                if (vmr9Field == null) vmr9Field = typeof(VideoPlayerVMR9).GetField("Vmr9", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (VMR9Util)vmr9Field.GetValue(this);
            }
            set
            {
                if (vmr9Field == null) vmr9Field = typeof(VideoPlayerVMR9).GetField("Vmr9", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                vmr9Field.SetValue(this, value);
            }
        }
    }
}
