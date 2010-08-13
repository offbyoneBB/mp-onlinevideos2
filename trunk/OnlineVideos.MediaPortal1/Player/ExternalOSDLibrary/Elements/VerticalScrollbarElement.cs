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

using System.Drawing;
using MediaPortal.GUI.Library;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// This class represents a GUIVerticalScrollBarElement
  /// </summary>
  public class VerticalScrollBarElement : BaseElement
  {
    #region variables
    /// <summary>
    /// GUIVerticalScrollbar
    /// </summary>
    private readonly GUIVerticalScrollbar _verticalScrollBar;

    /// <summary>
    /// Background image of the scrollbar
    /// </summary>
    private readonly Bitmap _scrollBarBackground;

    /// <summary>
    /// Top image of the scrollbar
    /// </summary>
    private readonly Bitmap _scrollBarTop;

    /// <summary>
    /// Bottom image of the scrollbar
    /// </summary>
    private readonly Bitmap _scrollBarBottom;

    /// <summary>
    /// Percentage
    /// </summary>
    private float _percentage;
    #endregion

    #region ctor
    /// <summary>
    /// Creates the element and retrieves all information from the control
    /// </summary>
    /// <param name="control">GUIControl</param>
    public VerticalScrollBarElement(GUIControl control)
      : base(control)
    {
      _verticalScrollBar = control as GUIVerticalScrollbar;
      if (_verticalScrollBar != null)
      {
        _scrollBarBackground = loadBitmap(_verticalScrollBar.BackGroundTextureName);
        _scrollBarTop = loadBitmap(_verticalScrollBar.BackTextureTopName);
        _scrollBarBottom = loadBitmap(_verticalScrollBar.BackTextureBottomName);
        _percentage = _verticalScrollBar.Percentage;
      }
    }
    #endregion

    #region implemented abstract methods
    /// <summary>
    /// Draws the element on the given graphics
    /// </summary>
    /// <param name="graph">Graphics</param>
    public override void DrawElement(Graphics graph)
    {
        if (_wasVisible)
      {
        if (_scrollBarBackground != null && _scrollBarTop != null && _scrollBarBottom != null)
        {
          int iHeight = _verticalScrollBar.Height;

          graph.DrawImage(_scrollBarBackground, _verticalScrollBar.XPosition, _verticalScrollBar.YPosition, _verticalScrollBar.Width, iHeight);

          float fPercent = _percentage;
          float fPosYOff = (fPercent / 100.0f);

          int _startPositionY = _verticalScrollBar.YPosition;
          int _endPositionY = _startPositionY + iHeight;

          int iKnobHeight = _scrollBarTop.Height;
          fPosYOff *= _endPositionY - _startPositionY - iKnobHeight;

          int _knobPositionY = _startPositionY + (int)fPosYOff;
          int iXPos = _verticalScrollBar.XPosition + ((_verticalScrollBar.Width / 2) - (_scrollBarTop.Width));
          int iYPos = _knobPositionY;
          graph.DrawImage(_scrollBarTop, iXPos, iYPos);

          iXPos += _scrollBarTop.Width;
          graph.DrawImage(_scrollBarBottom, iXPos, iYPos);
        }
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
      int oldPercentage = (int)_percentage;
      int newPercentage = (int)_verticalScrollBar.Percentage;
      if (oldPercentage != newPercentage)
      {
        _percentage = _verticalScrollBar.Percentage;
        result = true;
      }
      return result;
    }
    #endregion
  }
}
