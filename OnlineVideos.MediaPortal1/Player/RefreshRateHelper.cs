using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaPortal.Player;
using MediaPortal.Profile;

namespace OnlineVideos.MediaPortal1.Player
{
    internal static class RefreshRateHelper
    {
        internal static List<double> fpsList = null;

        internal static double MatchConfiguredFPS(double probedFps)
        {
            if (fpsList == null)
            {
                fpsList = new List<double>();
                NumberFormatInfo provider = new NumberFormatInfo() { NumberDecimalSeparator = "." };
                Settings xmlreader = new MPSettings();
                for (int i = 1; i < 100; i++)
                {
                    string name = xmlreader.GetValueAsString("general", "refreshrate0" + Convert.ToString(i) + "_name", "");
                    if (string.IsNullOrEmpty(name)) continue;
                    string fps = xmlreader.GetValueAsString("general", name + "_fps", "");
                    string[] fpsArray = fps.Split(';');
                    foreach (string fpsItem in fpsArray)
                    {
                        double fpsAsDouble = -1;
                        double.TryParse(fpsItem, NumberStyles.AllowDecimalPoint, provider, out fpsAsDouble);
                        if (fpsAsDouble > -1) fpsList.Add(fpsAsDouble);
                    }
                }
                fpsList = fpsList.Distinct().ToList();
                fpsList.Sort();
            }
            if (fpsList != null && fpsList.Count > 0)
            {
                return fpsList.FirstOrDefault(f => Math.Abs(f - probedFps) < 0.24f);
            }
            return default(double);
        }

        internal static void ChangeRefreshRateToMatchedFps(double matchedFps, string file)
        {
            Log.Instance.Info("Changing RefreshRate for matching configured FPS: {0}", matchedFps);
            RefreshRateChanger.SetRefreshRateBasedOnFPS(matchedFps, file, RefreshRateChanger.MediaType.Video);
            if (RefreshRateChanger.RefreshRateChangePending)
            {
                TimeSpan ts = DateTime.Now - RefreshRateChanger.RefreshRateChangeExecutionTime;
                if (ts.TotalSeconds > RefreshRateChanger.WAIT_FOR_REFRESHRATE_RESET_MAX)
                {
                    Log.Instance.Info("RefreshRateChanger.DelayedRefreshrateChanger: waited {0}s for refreshrate change, but it never took place (check your config). Proceeding with playback.", RefreshRateChanger.WAIT_FOR_REFRESHRATE_RESET_MAX);
                    RefreshRateChanger.ResetRefreshRateState();
                }
                else
                {
                    Log.Instance.Info("RefreshRateChanger.DelayedRefreshrateChanger: waited {0}s for refreshrate change. Proceeding with playback.", ts.TotalSeconds);
                }
            }
        }
    }
}
