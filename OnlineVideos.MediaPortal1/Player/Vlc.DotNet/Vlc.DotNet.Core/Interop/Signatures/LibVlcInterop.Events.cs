using System;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interops.Signatures
{
    namespace LibVlc
    {
        namespace AsynchronousEvents
        {
            public enum EventTypes : uint
            {
                MediaMetaChanged = 0,
                MediaSubItemAdded,
                MediaDurationChanged,
                MediaParsedChanged,
                MediaFreed,
                MediaStateChanged,

                MediaPlayerMediaChanged = 0x100,
                MediaPlayerNothingSpecial,
                MediaPlayerOpening,
                MediaPlayerBuffering,
                MediaPlayerPlaying,
                MediaPlayerPaused,
                MediaPlayerStopped,
                MediaPlayerForward,
                MediaPlayerBackward,
                MediaPlayerEndReached,
                MediaPlayerEncounteredError,
                MediaPlayerTimeChanged,
                MediaPlayerPositionChanged,
                MediaPlayerSeekableChanged,
                MediaPlayerPausableChanged,
                MediaPlayerTitleChanged,
                MediaPlayerSnapshotTaken,
                MediaPlayerLengthChanged,
                MediaPlayerVideoOutChanged,

                MediaListItemAdded = 0x200,
                MediaListWillAddItem,
                MediaListItemDeleted,
                MediaListWillDeleteItem,

                MediaListViewItemAdded = 0x300,
                MediaListViewWillAddItem,
                MediaListViewItemDeleted,
                MediaListViewWillDeleteItem,

                MediaListPlayerPlayed = 0x400,
                MediaListPlayerNextItemSet,
                MediaListPlayerStopped,

                MediaDiscovererStarted = 0x500,
                MediaDiscovererEnded,

                VlmMediaAdded = 0x600,
                VlmMediaRemoved,
                VlmMediaChanged,
                VlmMediaInstanceStarted,
                VlmMediaInstanceStopped,
                VlmMediaInstanceStatusInit,
                VlmMediaInstanceStatusOpening,
                VlmMediaInstanceStatusPlaying,
                VlmMediaInstanceStatusPause,
                VlmMediaInstanceStatusEnd,
                VlmMediaInstanceStatusError
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct LibVlcEventArgs
            {
                [FieldOffset(0)]
                public EventTypes Type;

                [FieldOffset(4)]
                public IntPtr ObjectHandle;

                #region media descriptor

                [FieldOffset(8)]
                public MediaMetaChangedArgs MediaMetaChanged;

                [FieldOffset(8)]
                public MediaSubitemAddedArgs MediaSubitemAdded;

                [FieldOffset(8)]
                public MediaDurationChangedArgs MediaDurationChanged;

                [FieldOffset(8)]
                public MediaParsedChangedArgs MediaParsedChanged;

                [FieldOffset(8)]
                public MediaFreedArgs MediaFreed;

                [FieldOffset(8)]
                public MediaStateChangedArgs MediaStateChanged;

                #endregion

                #region media instance

                [FieldOffset(8)]
                public MediaPlayerBufferingArgs MediaPlayerBuffering;

                [FieldOffset(8)]
                public MediaPlayerPositionChangedArgs MediaPlayerPositionChanged;

                [FieldOffset(8)]
                public MediaPlayerTimeChangedArgs MediaPlayerTimeChanged;

                [FieldOffset(8)]
                public MediaPlayerTitleChangedArgs MediaPlayerTitleChanged;

                [FieldOffset(8)]
                public MediaPlayerSeekableChangedArgs MediaPlayerSeekableChanged;

                [FieldOffset(8)]
                public MediaPlayerPausableChangedArgs MediaPlayerPausableChanged;

                [FieldOffset(8)]
                public MediaPlayerVideoOutChangedArgs MediaPlayerVideoOutChanged;

                #endregion

                #region media list

                [FieldOffset(8)]
                public MediaListItemAddedArgs MediaListItemAdded;

                [FieldOffset(8)]
                public MediaListWillAddItemArgs MediaListWillAddItem;

                [FieldOffset(8)]
                public MediaListItemDeletedArgs MediaListItemDeleted;

                [FieldOffset(8)]
                public MediaListWillDeleteItemArgs MediaListWillDeleteItem;

                #endregion

                #region media list player

                [FieldOffset(8)]
                public MediaListPlayerNextItemSetArgs MediaListPlayerNextItemSet;

                #endregion

                #region snapshot taken

                [FieldOffset(8)]
                public MediaPlayerSnapshotTakenArgs MediaPlayerSnapshotTaken;

                #endregion

                #region Length changed

                [FieldOffset(8)]
                public MediaPlayerLengthChangedArgs MediaPlayerLengthChanged;

                #endregion

                #region VLM media

                [FieldOffset(8)]
                public VlmMediaEventArgs VlmMediaEvent;

                #endregion

                #region Extra MediaPlayer

                [FieldOffset(8)]
                public MediaPlayerMediaChangedArgs MediaPlayerMediaChanged;

                #endregion
            }

            #region media descriptor

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaMetaChangedArgs
            {
                public Media.Metadatas MetaType;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaSubitemAddedArgs
            {
                public IntPtr NewChild;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaDurationChangedArgs
            {
                public long NewDuration;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaParsedChangedArgs
            {
                public int NewStatus;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaFreedArgs
            {
                public IntPtr MediaHandler;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaStateChangedArgs
            {
                public Media.States NewState;
            }

            #endregion

            #region media instance

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaPlayerBufferingArgs
            {
                public float NewCache;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaPlayerPositionChangedArgs
            {
                public float NewPosition;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaPlayerTimeChangedArgs
            {
                public long NewTime;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaPlayerTitleChangedArgs
            {
                public int NewTitle;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaPlayerSeekableChangedArgs
            {
                public int NewSeekable;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaPlayerPausableChangedArgs
            {
                public int NewPausable;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaPlayerVideoOutChangedArgs
            {
                public int NewCount;
            }

            #endregion

            #region media list

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaListItemAddedArgs
            {
                public IntPtr ItemHandle;
                public int Index;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaListWillAddItemArgs
            {
                public IntPtr ItemHandle;
                public int Index;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaListItemDeletedArgs
            {
                public IntPtr ItemHandle;
                public int Index;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaListWillDeleteItemArgs
            {
                public IntPtr ItemHandle;
                public int Index;
            }

            #endregion

            #region  media list player

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaListPlayerNextItemSetArgs
            {
                public IntPtr ItemHandle;
            }

            #endregion

            #region snapshot taken

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaPlayerSnapshotTakenArgs
            {
                public IntPtr pszFilename;
            }

            #endregion

            #region Length changed

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaPlayerLengthChangedArgs
            {
                public long NewLength;
            }

            #endregion

            #region  VLM media

            [StructLayout(LayoutKind.Sequential)]
            public struct VlmMediaEventArgs
            {
                public IntPtr pszMediaName;
                public IntPtr pszInstanceName;
            }

            #endregion

            #region  Extra MediaPlayer

            [StructLayout(LayoutKind.Sequential)]
            public struct MediaPlayerMediaChangedArgs
            {
                public IntPtr NewMediaHandle;
            }

            #endregion

        }
    }
}