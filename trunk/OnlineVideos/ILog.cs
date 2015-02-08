using System;

namespace OnlineVideos
{
	/// <summary>
	/// This interface defines the contract a class should implement that wants to receive all log messages generated in the OnlineVideos Core and Sites.
	/// </summary>
    public interface ILog
    {
        void Debug(string format, params object[] arg);
        void Error(Exception ex);
        void Error(string format, params object[] arg);
        void Info(string format, params object[] arg);
        void Warn(string format, params object[] arg);
    }

    /// <summary>
    /// This static class simply delegates Log calls to the <see cref="OnlineVideoSettings.Logger"/>.
	/// If no logger is set - no exception is generated - the messages are simply ignored.
	/// The logging class lives in the main applications AppDomain, so make sure the objects passed as args
    /// are marked with the <see cref="SerializableAttribute"/> or inherit from <see cref="MarshalByRefObject"/>.
    /// </summary>
    public static class Log
    {
		/// <summary>
		/// Appends a message to the logfile when the current verbosity is set to Debug or lower.
		/// </summary>
		/// <param name="format">The messe to log. Can be a formatstring referencing the <paramref name="args"/> like: {0},{1:g},...</param>
		/// <param name="arg">An array of arguments used for the <paramref name="format"/> string.</param>
        public static void Debug(string format, params object[] arg)
        {
            if (OnlineVideoSettings.Instance.Logger != null) OnlineVideoSettings.Instance.Logger.Debug(format, arg);
        }

		/// <summary>
		/// Overload to simply append the output of <see cref="Exception.ToString"/> to the logfile when the current verbosity is set to Error or lower.
		/// </summary>
		/// <param name="ex">The <see cref="Exception"/> to log.</param>
        public static void Error(Exception ex)
        {
            if (OnlineVideoSettings.Instance.Logger != null) OnlineVideoSettings.Instance.Logger.Error(ex.ToString());
        }

		/// <summary>
		/// Appends a message to the logfile when the current verbosity is set to Error or lower.
		/// </summary>
		/// <param name="format">The messe to log. Can be a formatstring referencing the <paramref name="args"/> like: {0},{1:g},...</param>
		/// <param name="arg">An array of arguments used for the <paramref name="format"/> string.</param>
        public static void Error(string format, params object[] arg)
        {
            if (OnlineVideoSettings.Instance.Logger != null) OnlineVideoSettings.Instance.Logger.Error(format, arg);
        }

		/// <summary>
		/// Appends a message to the logfile when the current verbosity is set to Information or lower.
		/// </summary>
		/// <param name="format">The messe to log. Can be a formatstring referencing the <paramref name="args"/> like: {0},{1:g},...</param>
		/// <param name="arg">An array of arguments used for the <paramref name="format"/> string.</param>
        public static void Info(string format, params object[] arg)
        {
            if (OnlineVideoSettings.Instance.Logger != null) OnlineVideoSettings.Instance.Logger.Info(format, arg);
        }

		/// <summary>
		/// Appends a message to the logfile when the current verbosity is set to Warning or lower.
		/// </summary>
		/// <param name="format">The messe to log. Can be a formatstring referencing the <paramref name="args"/> like: {0},{1:g},...</param>
		/// <param name="arg">An array of arguments used for the <paramref name="format"/> string.</param>
        public static void Warn(string format, params object[] arg)
        {
            if (OnlineVideoSettings.Instance.Logger != null) OnlineVideoSettings.Instance.Logger.Warn(format, arg);
        }
    }
}
