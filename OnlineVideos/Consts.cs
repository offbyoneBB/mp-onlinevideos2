namespace OnlineVideos
{
    public class Constants
    {
        public const string ACTION_MOVE_LEFT = "ACTION_MOVE_LEFT";
        public const string ACTION_MOVE_RIGHT = "ACTION_MOVE_RIGHT";

        public const string ACTION_PREV_ITEM = "ACTION_PREV_ITEM";
        public const string ACTION_NEXT_ITEM = "ACTION_NEXT_ITEM";

        public const string ACTION_PLAY = "ACTION_PLAY";
        public const string ACTION_MUSIC_PLAY = "ACTION_MUSIC_PLAY";
        public const string ACTION_PAUSE = "ACTION_PAUSE";
        public const string ACTION_STOP = "ACTION_STOP";

        public const string ACTION_PREVIOUS_MENU = "ACTION_PREVIOUS_MENU";
        public const string ACTION_CONTEXT_MENU = "ACTION_CONTEXT_MENU";

        public const string ACTION_FULLSCREEN = "ACTION_FULLSCREEN";

        /// <summary>
        /// When sent as action the target window position and size will be added in the message.
        /// Like ACTION_WINDOWED_0,0,640,480 (x, y, witdh, height)
        /// </summary>
        public const string ACTION_WINDOWED = "ACTION_WINDOWED_";
    }
}
