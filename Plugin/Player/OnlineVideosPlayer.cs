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
                this.Cleanup();
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
                this.Cleanup();
                return false;
            }
            this.Vmr9.SetDeinterlaceMode();

            return true;
        }        

        System.Reflection.MethodInfo cleanupMethod = null;
        private void Cleanup()
        {
            if (cleanupMethod == null) cleanupMethod = typeof(VideoPlayerVMR9).GetMethod("Cleanup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cleanupMethod.Invoke(this, null);
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
