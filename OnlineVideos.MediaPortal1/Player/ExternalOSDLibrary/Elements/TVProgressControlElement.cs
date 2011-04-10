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
  /// This class represents a GUITVProgressControl
  /// </summary>
  public class TVProgressControlElement : BaseElement
  {
    #region variables
    /// <summary>
    /// GUIProgressControl
    /// </summary>
    private readonly GUITVProgressControl _progressControl;

    /// <summary>
    /// Left image
    /// </summary>
    private readonly Bitmap _Fill1Texture;

    /// <summary>
    /// Middle image
    /// </summary>
    private readonly Bitmap _Fill2Texture;

    /// <summary>
    /// Right image
    /// </summary>
    private readonly Bitmap _Fill3Texture;

    /// <summary>
    /// Background image
    /// </summary>
    private readonly Bitmap _FillBackGround;

    /// <summary>
    /// Percentage of the progress control
    /// </summary>
    private int _percentage1;
    private int _percentage2;
    private int _percentage3;
    #endregion

    #region ctor
    /// <summary>
    /// Creates the element and retrieves all information from the control
    /// </summary>
    /// <param name="control">GUIControl</param>
    public TVProgressControlElement(GUIControl control)
      : base(control)
    {
      _progressControl = control as GUITVProgressControl;
      if (_progressControl != null)
      {
          _Fill1Texture = loadBitmap(_progressControl.Fill1TextureName);
          _Fill2Texture = loadBitmap(_progressControl.Fill2TextureName);
          _Fill3Texture = loadBitmap(_progressControl.Fill3TextureName);
          _FillBackGround = loadBitmap(_progressControl.FillBackGroundName);
          _percentage1 = (int)Math.Round(_progressControl.Percentage1);
          _percentage2 = (int)Math.Round(_progressControl.Percentage2);
          _percentage3 = (int)Math.Round(_progressControl.Percentage3);
      }
    }
    #endregion

    #region implmenented abstract method
    /// <summary>
    /// Draws the element on the given graphics
    /// </summary>
    /// <param name="graph">Graphics</param>
    public override void DrawElement(Graphics graph)
    {
        if (_wasVisible)
      {
        DrawProgressBar(graph);
      }
    }

    /// <summary>
    /// Disposes the object
    /// </summary>
    public override void Dispose()
    {
    }

    /// <summary>
    /// Checks, if an update for the element is needed
    /// </summary>
    /// <returns>true, if an update is needed</returns>
    protected override bool CheckElementSpecificForUpdate()
    {
      bool result = false;

      int newPercentage1 = (int)Math.Round(_progressControl.Percentage1);
      int newPercentage2 = (int)Math.Round(_progressControl.Percentage2);
      int newPercentage3 = (int)Math.Round(_progressControl.Percentage3);

      if (newPercentage1 != _percentage1 || newPercentage2 != _percentage2 || newPercentage3 != _percentage3)
      {
          _percentage1 = newPercentage1;
          _percentage2 = newPercentage2;
          _percentage3 = newPercentage3;
            result = true;
      }
      return result;
    }
    #endregion

    #region public overrides methods
    /// <summary>
    /// Draws the element for the cache status.
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="cacheFill">Status of the cache</param>
    public override void DrawCacheStatus(Graphics graph, float cacheFill)
    {
      DrawProgressBar(graph);
    }
    #endregion

    #region private methods
    /// <summary>
    /// Draws the progress bar with the given width and percentage
    /// </summary>
    /// <param name="graph">Graphics</param>
    private void DrawProgressBar(Graphics graph)
    {
        int iWidth = _progressControl.Width;
        iWidth -= 2 * _progressControl.FillX;

        // render first color
        int xoff = GUIGraphicsContext.ScaleHorizontal(3);
        int yoff = GUIGraphicsContext.ScaleVertical(_progressControl.TopTextureYOffset);

        int xPos = _progressControl.XPosition + _progressControl.FillX + xoff;

        int yPos = _progressControl.YPosition + (_progressControl.FillY / 2) - (_progressControl.FillHeight / 2) + (yoff / 2);
        
        float fWidth = (float)iWidth;
        fWidth /= 100.0f;
        fWidth *= (float)_progressControl.Percentage1;
        int iWidth1 = (int)Math.Floor(fWidth);
        if (iWidth1 > 0)
        {
            graph.DrawImage(_Fill1Texture, xPos, yPos, iWidth1, _progressControl.FillHeight);
        }

        int iCurPos = iWidth1 + xPos;
        
        //render 2nd color
        float fPercent;
        if (_progressControl.Percentage2 >= _progressControl.Percentage1)
        {
            fPercent = _progressControl.Percentage2 - _progressControl.Percentage1;
        }
        else
        {
            fPercent = 0;
        }
        fWidth = (float)iWidth;
        fWidth /= 100.0f;
        fWidth *= (float)fPercent;
        int iWidth2 = (int)Math.Floor(fWidth);
        if (iWidth2 > 0)
        {
            graph.DrawImage(_Fill2Texture, iCurPos, yPos, iWidth2, _progressControl.FillHeight);
        }
        iCurPos = iWidth1 + iWidth2 + xPos;

        //render 3rd color
        if (_progressControl.Percentage3 >= _progressControl.Percentage2)
        {

            fPercent = _progressControl.Percentage3 - _progressControl.Percentage2;
        }
        else
        {
            fPercent = 0;
        }
        fWidth = (float)iWidth;
        fWidth /= 100.0f;
        fWidth *= (float)fPercent;
        int iWidth3 = (int)Math.Floor(fWidth);
        if (iWidth3 > 0)
        {
            graph.DrawImage(_Fill3Texture, iCurPos, yPos, iWidth3, _progressControl.FillHeight);
        }
    }    
    #endregion
  }
}
