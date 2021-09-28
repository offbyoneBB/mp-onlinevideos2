
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineVideos.Downloading
{
    public class FFMPEGDownloader : MarshalByRefObject, IDownloader
    {
        System.Threading.Thread _downloadThread;
        private string _ffmpegExePath;
        private CancellationTokenSource _cts = new CancellationTokenSource(DEFAULT_TIMEOUT);
        private static TimeSpan DEFAULT_TIMEOUT= TimeSpan.FromMinutes(30);

        public bool Cancelled { get; private set; }

        public void CancelAsync()
        {
            _cts.Cancel();
            Cancelled = true;
        }

        public FFMPEGDownloader(string ffmpegExePath)
        {
            if (string.IsNullOrEmpty(ffmpegExePath))
                throw new ArgumentException("No valid path to ffmpeg.exe given.", nameof(ffmpegExePath));
            _ffmpegExePath = ffmpegExePath;
        }

        public Exception Download(DownloadInfo downloadInfo)
        {
            HttpWebResponse response = null;
            try
            {
                _downloadThread = System.Threading.Thread.CurrentThread;

                string args = string.Format($"-i \"{downloadInfo.Url}\" -codec: copy \"{downloadInfo.LocalFile}\"");
                ProcessUtils.ExecuteAsync(_ffmpegExePath, args, ProcessPriorityClass.Idle, (int)DEFAULT_TIMEOUT.TotalMilliseconds)
                    .Wait(_cts.Token);// 30 Minutes

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                if (response != null) response.Close();
            }
        }

        public void Abort()
        {
            if (_downloadThread != null) _downloadThread.Abort();
        }

        #region MarshalByRefObject overrides
        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }
        #endregion
    }

    public class ProcessUtils
    {
        public static readonly Encoding CONSOLE_ENCODING = Encoding.UTF8;
        private static readonly string CONSOLE_ENCODING_PREAMBLE = CONSOLE_ENCODING.GetString(CONSOLE_ENCODING.GetPreamble());

        public const int INFINITE = -1;
        public const int DEFAULT_TIMEOUT = 10000;

        /// <summary>
        /// Executes the <paramref name="executable"/> asynchronously and waits a maximum time of <paramref name="maxWaitMs"/> for completion.
        /// </summary>
        /// <param name="executable">Program to execute</param>
        /// <param name="arguments">Program arguments</param>
        /// <param name="priorityClass">Process priority</param>
        /// <param name="maxWaitMs">Maximum time to wait for completion</param>
        /// <returns>> <see cref="ProcessExecutionResult"/> object that respresents the result of executing the Program</returns>
        /// <remarks>
        /// This method throws an exception only if process.Start() fails (in partiular, if the <paramref name="executable"/> doesn't exist).
        /// Any other error in managed code is signaled by the returned task being set to Faulted state.
        /// If the program itself does not result in an ExitCode of 0, the returned task ends in RanToCompletion state;
        /// the ExitCode of the program will be contained in the returned <see cref="ProcessExecutionResult"/>.
        /// This method is nearly identical to ImpersonationService.ExecuteWithResourceAccessAsync; it is necessary to have this code duplicated
        /// because AsyncImpersonationProcess hides several methods of the Process class and executing these methods on the base class does
        /// therefore not work. If this method is changed it is likely that ImpersonationService.ExecuteWithResourceAccessAsync also
        /// needs to be changed.
        /// </remarks>
        public static Task<ProcessExecutionResult> ExecuteAsync(string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = DEFAULT_TIMEOUT)
        {
            var tcs = new TaskCompletionSource<ProcessExecutionResult>();
            bool exited = false;
            var process = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo(executable, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = CONSOLE_ENCODING,
                    StandardErrorEncoding = CONSOLE_ENCODING
                },
                EnableRaisingEvents = true
            };

            // We need to read standardOutput and standardError asynchronously to avoid a deadlock
            // when the buffer is not big enough to receive all the respective output. Otherwise the
            // process may block because the buffer is full and the Exited event below is never raised.
            var standardOutput = new StringBuilder();
            var standardOutputResults = new TaskCompletionSource<string>();
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    standardOutput.AppendLine(args.Data);
                else
                    standardOutputResults.SetResult(standardOutput.Length > 0 ? RemoveEncodingPreamble(standardOutput.ToString()) : null);
            };

            var standardError = new StringBuilder();
            var standardErrorResults = new TaskCompletionSource<string>();
            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    standardError.AppendLine(args.Data);
                else
                    standardErrorResults.SetResult(standardError.Length > 0 ? RemoveEncodingPreamble(standardError.ToString()) : null);
            };

            var processStart = new TaskCompletionSource<bool>();
            // The Exited event is raised in any case when the process has finished, i.e. when it gracefully
            // finished (ExitCode = 0), finished with an error (ExitCode != 0) and when it was killed below.
            // That ensures disposal of the process object.
            process.Exited += async (sender, args) =>
            {
                exited = true;
                try
                {
                    await processStart.Task;
                    // standardStreamTasksReady is only disposed when starting the process was not successful,
                    // in which case the Exited event is never raised.
                    // ReSharper disable once AccessToDisposedClosure
                    tcs.TrySetResult(new ProcessExecutionResult
                    {
                        ExitCode = process.ExitCode,
                        // standardStreamTasksReady makes sure that we do not access the standard stream tasks before they are initialized.
                        // For the same reason it is intended that these tasks (as closures) are modified (i.e. initialized).
                        // We need to take this cumbersome way because it is not possible to access the standard streams before the process
                        // is started. If on the other hand the Exited event is raised before the tasks are initialized, we need to make
                        // sure that this method waits until the tasks are initialized before they are accessed.
                        // ReSharper disable PossibleNullReferenceException
                        // ReSharper disable AccessToModifiedClosure
                        StandardOutput = await standardOutputResults.Task,
                        StandardError = await standardErrorResults.Task
                        // ReSharper restore AccessToModifiedClosure
                        // ReSharper restore PossibleNullReferenceException
                    });
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
                finally
                {
                    process.Dispose();
                }
            };

            bool processStarted = process.Start();
            processStart.SetResult(processStarted);
            if (processStarted)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                try
                {
                    // This call may throw an exception if the process has already exited when we get here.
                    // In that case the Exited event has already set tcs to RanToCompletion state so that
                    // the TrySetException call below does not change the state of tcs anymore. This is correct
                    // as it doesn't make sense to change the priority of the process if it is already finished.
                    // Any other "real" error sets the state of tcs to Faulted below.
                    process.PriorityClass = priorityClass;
                }
                catch (InvalidOperationException e)
                {
                    // This exception indicates that the process is no longer available which is probably 
                    // because the process has exited already. The exception should not be logged because 
                    // there is no guarantee that the exited event has finished setting the task to the 
                    // RanToCompletion state before this exception sets it to the Faulted state.
                    if (!exited && !process.HasExited)
                        tcs.TrySetException(e);
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            }
            else
            {
                exited = true;
                standardOutputResults.SetResult(null);
                standardErrorResults.SetResult(null);

                return Task.FromResult(new ProcessExecutionResult { ExitCode = Int32.MinValue });
            }

            if (maxWaitMs != INFINITE)
                Task.Delay(maxWaitMs).ContinueWith(task =>
                {
                    try
                    {
                        // Cancel the state of tcs if it was not set to Faulted or
                        // RanToCompletion before.
                        tcs.TrySetCanceled();
                        // Always kill the process if is running.
                        if (!exited && !process.HasExited)
                            process.Kill();
                    }
                    // ReSharper disable once EmptyGeneralCatchClause
                    // An exception is thrown in process.Kill() when the external process exits
                    // while we set tcs to canceled. In that case there is nothing to do anymore.
                    // This is not an error.
                    catch
                    { }
                });

            return tcs.Task;
        }

        /// <summary>
        /// Helper method to remove an existing encoding preamble (<see cref="Encoding.GetPreamble"/>) from the given <paramref name="rawString"/>.
        /// </summary>
        /// <param name="rawString">Raw string that might include the preamble (BOM).</param>
        /// <returns>String without preamble.</returns>
        public static string RemoveEncodingPreamble(string rawString)
        {
            if (!string.IsNullOrWhiteSpace(rawString) && rawString.StartsWith(CONSOLE_ENCODING_PREAMBLE, StringComparison.Ordinal))
                return rawString.Substring(CONSOLE_ENCODING_PREAMBLE.Length);
            return rawString;
        }
    }

    /// <summary>
    /// Represents the result of running an external process
    /// </summary>
    public class ProcessExecutionResult
    {
        /// <summary>
        /// Convenience method to check the <see cref="ExitCode"/> of a process
        /// Returns <c>true</c> if the <see cref="ExitCode"/> is 0; otherwise false
        /// </summary>
        public bool Success { get { return ExitCode == 0; } }

        /// <summary>
        /// Contains the ExitCode of the process
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Contains the StandardOutput of the process
        /// </summary>
        public String StandardOutput { get; set; }

        /// <summary>
        /// Contains the StandardError output of the process
        /// </summary>
        public String StandardError { get; set; }
    }
}
