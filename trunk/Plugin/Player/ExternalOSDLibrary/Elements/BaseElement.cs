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
using System.Drawing;
using MediaPortal.GUI.Library;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// Base class for all gui elements
  /// </summary>
  public abstract class BaseElement : IDisposable
  {
    #region variables
    /// <summary>
    /// Indicates, if the element was visible
    /// </summary>
    protected bool _wasVisible;

    /// <summary>
    /// Control of the base element
    /// </summary>
    protected GUIControl _control;
    #endregion

    #region ctor
    /// <summary>
    /// Initialize the base element
    /// </summary>
    /// <param name="control">GUIControl</param>
    protected BaseElement(GUIControl control)
    {
      _control = control;
    }
    #endregion

    #region abstract methods
    /// <summary>
    /// Draws the element on the given graphics
    /// </summary>
    /// <param name="graph">Graphics</param>
    public abstract void DrawElement(Graphics graph);

    /// <summary>
    /// Checks, if an update for the element is needed
    /// </summary>
    /// <returns>true, if an update is needed</returns>
    protected abstract bool CheckElementSpecificForUpdate();
    #endregion

    #region protected methods
    /// <summary>
    /// Creates a color for the given value. And guarantees that it doesn't get transparent
    /// </summary>
    /// <param name="colorValue">Value of the color</param>
    /// <returns>Color struct</returns>
    protected static Color GetColor(long colorValue)
    {
      Color color = Color.FromArgb((int)colorValue);
      if (color.R == 0 && color.G == 0 && color.B == 0)
      {
        color = Color.FromArgb(5, 5, 5);
      }
      else if (color.R == 1 && color.G == 1 && color.B == 1)
      {
        color = Color.FromArgb(5, 5, 5);
      }
      else
      {
        color = Color.FromArgb(color.R, color.G, color.B);
      }
      return color;
    }

    /// <summary>
    /// Creates a font object based on the given name
    /// </summary>
    /// <param name="name">Name of the font</param>
    /// <returns>Font </returns>
    protected static Font getFont(String name)
    {
      GUIFont guiFont = GUIFontManager.GetFont(name);
      return new Font(guiFont.FileName, guiFont.FontSize, guiFont.FontStyle);
    }

    /// <summary>
    /// Loads the bitmap with the given filename
    /// </summary>
    /// <param name="fileName">Filename of the bitmap</param>
    /// <returns>Bitmap</returns>
    protected static Bitmap loadBitmap(String fileName)
    {
      return ImageCache.GetImage(fileName);
    }
    #endregion

    #region public methods
    /// <summary>
    /// Draws the element for the cache status. Only implemented in some elements
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="cacheFill">Status of the cache</param>
    public virtual void DrawCacheStatus(Graphics graph, float cacheFill)
    {
    }

    /// <summary>
    /// Checks, if an update for the element is needed
    /// </summary>
    /// <returns>true, if an update is needed</returns>
    public bool CheckForUpdate()
    {
      _control.UpdateVisibility();
      bool newVisible = _control.Visible || GUIInfoManager.GetBool(_control.GetVisibleCondition(),_control.ParentID);
      if (newVisible == _wasVisible)
      {
        if (newVisible)
        {
          return CheckElementSpecificForUpdate();
        }
        return false;
      }
      _wasVisible = newVisible;
      if (true)
      {
        CheckElementSpecificForUpdate();
      }
      return true;
    }
    #endregion

    #region IDisposable Member
    /// <summary>
    /// Disposes the object
    /// </summary>
    public abstract void Dispose();

    #endregion

  }
}
