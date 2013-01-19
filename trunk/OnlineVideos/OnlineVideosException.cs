using System;
using System.Runtime.Serialization;

namespace OnlineVideos
{
    /// <summary>
    /// Exception that can be used by SiteUtils to show explicit error messages to the user when methods executed in background fail.
    /// The <see cref="Message"/> of this <see cref="Exception"/> will be presented to the user.
    /// </summary>
	[Serializable]
    public class OnlineVideosException : Exception
    {
		public OnlineVideosException() : base() { }
		
		protected OnlineVideosException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public OnlineVideosException(string message) : base(message) 
        {
            ShowCurrentTaskDescription = true;
        }

        public OnlineVideosException(string message, bool showCurrentTaskDescription)
            : base(message)
        {
            ShowCurrentTaskDescription = showCurrentTaskDescription;
        }

        /// <summary>
        /// If true, shows the description of the current task on the error dialog. Set this to false to
        /// hide this (e.g. for info messages)
        /// </summary>
        public bool ShowCurrentTaskDescription { get; set; }
    }
}
