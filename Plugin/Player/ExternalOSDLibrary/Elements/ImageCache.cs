#region Copyright (C) 2006-2009 MisterD

/* 
 *	Copyright (C) 2006-2009 MisterD
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using MediaPortal.GUI.Library;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// Internal cache of the bitmaps
  /// </summary>
  internal class ImageCache
  {
    #region variables
    /// <summary>
    /// Cache container
    /// </summary>
    private static Dictionary<string, Bitmap> _imageCache;
    #endregion

    #region ctor
    /// <summary>
    /// Disable constructor as we have only static members
    /// </summary>
    private ImageCache()
    {
      
    }

    /// <summary>
    /// Dispose the cache
    /// </summary>
    public static void Dispose()
    {
      foreach(Bitmap bitmap in _imageCache.Values)
      {
        bitmap.Dispose();
      }
      _imageCache.Clear();
    }
    #endregion

    #region public static methods
    /// <summary>
    /// Get the image for the given filename
    /// If the file is already in the cache than return the cache image
    /// </summary>
    /// <param name="fileName">Filename of the image</param>
    /// <returns>The image for the given filename</returns>
    public static Bitmap GetImage(string fileName)
    {
      if(_imageCache==null)
      {
        _imageCache = new Dictionary<string, Bitmap>();
      }
      if(_imageCache.ContainsKey(fileName))
      {
        return _imageCache[fileName];
      }
      Bitmap result = null;
      String realFileName = GUIPropertyManager.Parse(fileName);
      String location = GUIGraphicsContext.Skin + @"\media\" + realFileName;
      if (File.Exists(location))
      {
        result = new Bitmap(location);
        UpdateBitmap(result);
      }
      if(result!=null)
      {
        _imageCache.Add(fileName, result);
      }
      return result;

    }
    #endregion

    #region private static methods
    /// <summary>
    /// Updates a bitmap. It converts the Color (0,0,0) and (1,1,1), so that they won't be drawn transparent
    /// if the alpha value is more than 150
    /// </summary>
    /// <param name="bitmap">Bitmap to update</param>
    private static void UpdateBitmap(Bitmap bitmap)
    {
      try
      {
        Color temp;
        for (int i = 0; i < bitmap.Width; i++)
        {
          for (int j = 0; j < bitmap.Height; j++)
          {
            temp = bitmap.GetPixel(i, j);
            if (temp.R == 0 && temp.G == 0 && temp.B == 0 && temp.A > 150)
            {
              bitmap.SetPixel(i, j, Color.FromArgb(temp.A, 5, 5, 5));
            }
            if (temp.R == 1 && temp.G == 1 && temp.B == 1 && temp.A > 150)
            {
              bitmap.SetPixel(i, j, Color.FromArgb(temp.A, 5, 5, 5));
            }
          }
        }
      }catch
      {
        Log.Info("Could not update bitmap");
      }
    }
    #endregion
  }
}
