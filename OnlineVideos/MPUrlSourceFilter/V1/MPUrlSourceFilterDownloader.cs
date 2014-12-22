using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1
{
    /// <summary>
    /// Represents class for downloading single stream with MediaPortal Url Source Filter.
    /// </summary>
    public static class MPUrlSourceFilterDownloader
    {
        #region Methods

		public static void ClearDownloadCache()
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MPUrlSource");
			if (Directory.Exists(path)) foreach (var file in Directory.GetFiles(path)) try { File.Delete(file); } catch {}
			path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MPUrlSourceSplitter");
			if (Directory.Exists(path))foreach (var file in Directory.GetFiles(path)) try { File.Delete(file); } catch {}
		}

        #endregion
    }
}
