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
  /// This class represents a GUITextScrollUpControl
  /// </summary>
  public class TextScrollUpElement : BaseElement
  {
    #region variables
    /// <summary>
    /// GUITextScrollUpControl
    /// </summary>
    private readonly GUITextScrollUpControl _textScrollUp;

    /// <summary>
    /// Font
    /// </summary>
    private readonly Font _font;

    /// <summary>
    /// Brush
    /// </summary>
    private readonly Brush _brush;

    /// <summary>
    /// Label of the text scrollup element
    /// </summary>
    private String _label;
    #endregion

    #region ctor
    /// <summary>
    /// Creates the element and retrieves all information from the control
    /// </summary>
    /// <param name="control">GUIControl</param>
    public TextScrollUpElement(GUIControl control)
      : base(control)
    {
      _textScrollUp = control as GUITextScrollUpControl;
      if (_textScrollUp != null)
      {
        _font = getFont(_textScrollUp.FontName);
        _brush = new SolidBrush(GetColor(_textScrollUp.TextColor));
        _label = _textScrollUp.Property;
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
      if (_textScrollUp.Visible || GUIInfoManager.GetBool(_textScrollUp.GetVisibleCondition(), _textScrollUp.ParentID))
      {
        SizeF textSize = graph.MeasureString(_label, _font);
        RectangleF rectangle;
        if (_textScrollUp.TextAlignment == GUIControl.Alignment.ALIGN_LEFT)
        {
          rectangle = new RectangleF((float)_textScrollUp.Location.X, (float)_textScrollUp.Location.Y, _textScrollUp.Width, Math.Max(textSize.Height, _textScrollUp.Height));
        }
        else rectangle = _textScrollUp.TextAlignment == GUIControl.Alignment.ALIGN_RIGHT ? new RectangleF((float)_textScrollUp.Location.X - textSize.Width, (float)_textScrollUp.Location.Y, _textScrollUp.Width, Math.Max(textSize.Height, _textScrollUp.Height)) : new RectangleF((float)_textScrollUp.Location.X - (textSize.Width / 2), (float)_textScrollUp.Location.Y - (textSize.Height / 2), _textScrollUp.Width, Math.Max(textSize.Height, _textScrollUp.Height));
        graph.DrawString(GUIPropertyManager.Parse(_label), _font, _brush, rectangle, StringFormat.GenericTypographic);
      }
    }

    /// <summary>
    /// Disposes the object
    /// </summary>
    public override void Dispose()
    {
      _font.Dispose();
      _brush.Dispose();
    }

    /// <summary>
    /// Checks, if an update for the element is needed
    /// </summary>
    /// <returns>true, if an update is needed</returns>
    protected override bool CheckElementSpecificForUpdate()
    {
      bool result = false;
      String newLabel = GUIPropertyManager.Parse(_textScrollUp.Property);
      if (!newLabel.Equals(_label))
      {
        _label = _textScrollUp.Property;
        result = true;
      }
      return result;
    }
    #endregion
  }
}
