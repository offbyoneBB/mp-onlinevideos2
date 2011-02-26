using System;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interop
{
    internal enum libvlc_event_e : uint
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
    internal struct libvlc_event_t
    {
        [FieldOffset(0)] public libvlc_event_e type;

        [FieldOffset(4)] public IntPtr p_obj;

        #region media descriptor

        [FieldOffset(8)] public media_meta_changed media_meta_changed;

        [FieldOffset(8)] public media_subitem_added media_subitem_added;

        [FieldOffset(8)] public media_duration_changed media_duration_changed;

        [FieldOffset(8)] public media_parsed_changed media_parsed_changed;

        [FieldOffset(8)] public media_freed media_freed;

        [FieldOffset(8)] public media_state_changed media_state_changed;

        #endregion

        #region media instance

        [FieldOffset(8)] public media_player_buffering media_player_buffering;

        [FieldOffset(8)] public media_player_position_changed media_player_position_changed;

        [FieldOffset(8)] public media_player_time_changed media_player_time_changed;

        [FieldOffset(8)] public media_player_title_changed media_player_title_changed;

        [FieldOffset(8)] public media_player_seekable_changed media_player_seekable_changed;

        [FieldOffset(8)] public media_player_pausable_changed media_player_pausable_changed;

        #endregion

        #region media list

        [FieldOffset(8)] public media_list_item_added media_list_item_added;

        [FieldOffset(8)] public media_list_will_add_item media_list_will_add_item;

        [FieldOffset(8)] public media_list_item_deleted media_list_item_deleted;

        [FieldOffset(8)] public media_list_will_delete_item media_list_will_delete_item;

        #endregion

        #region media list player

        [FieldOffset(8)] public media_list_player_next_item_set media_list_player_next_item_set;

        #endregion

        #region snapshot taken

        [FieldOffset(8)] public media_player_snapshot_taken media_player_snapshot_taken;

        #endregion

        #region Length changed

        [FieldOffset(8)] public media_player_length_changed media_player_length_changed;

        #endregion

        #region VLM media

        [FieldOffset(8)] public vlm_media_event vlm_media_event;

        #endregion

        #region Extra MediaPlayer

        [FieldOffset(8)] public media_player_media_changed media_player_media_changed;

        #endregion
    }

    #region media descriptor

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_meta_changed
    {
        public libvlc_meta_t meta_type;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_subitem_added
    {
        public IntPtr new_child;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_duration_changed
    {
        public long new_duration;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_parsed_changed
    {
        public int new_status;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_freed
    {
        public IntPtr md;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_state_changed
    {
        public libvlc_state_t new_state;
    }

    #endregion

    #region media instance

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_player_buffering
    {
        public float new_cache;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_player_position_changed
    {
        public float new_position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_player_time_changed
    {
        public long new_time;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_player_title_changed
    {
        public int new_title;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_player_seekable_changed
    {
        public int new_seekable;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_player_pausable_changed
    {
        public int new_pausable;
    }

    #endregion

    #region media list

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_list_item_added
    {
        public IntPtr item;
        public int index;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_list_will_add_item
    {
        public IntPtr item;
        public int index;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_list_item_deleted
    {
        public IntPtr item;
        public int index;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_list_will_delete_item
    {
        public IntPtr item;
        public int index;
    }

    #endregion

    #region  media list player

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_list_player_next_item_set
    {
        public IntPtr item;
    }

    #endregion

    #region snapshot taken

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_player_snapshot_taken
    {
        public IntPtr psz_filename;
    }

    #endregion

    #region Length changed

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_player_length_changed
    {
        public long new_length;
    }

    #endregion

    #region  VLM media

    [StructLayout(LayoutKind.Sequential)]
    internal struct vlm_media_event
    {
        public IntPtr psz_media_name;
        public IntPtr psz_instance_name;
    }

    #endregion

    #region  Extra MediaPlayer

    [StructLayout(LayoutKind.Sequential)]
    internal struct media_player_media_changed
    {
        public IntPtr new_media;
    }

    #endregion
}