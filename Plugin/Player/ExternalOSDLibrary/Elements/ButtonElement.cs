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
  /// This class represents a GUIButtoncontrol
  /// </summary>
  public class ButtonElement : BaseElement
  {
    #region variables
    /// <summary>
    /// GUIButtonControl
    /// </summary>
    private readonly GUIButtonControl _button;

    /// <summary>
    /// Focus image
    /// </summary>
    private readonly Bitmap _focusBitmap;

    /// <summary>
    /// Non focus image
    /// </summary>
    private readonly Bitmap _noFocusBitmap;

    /// <summary>
    /// Hover image
    /// </summary>
    private readonly Bitmap _hoverBitmap;

    /// <summary>
    /// Font
    /// </summary>
    private readonly Font _font;

    /// <summary>
    /// Text color
    /// </summary>
    private readonly Color _textColor;

    /// <summary>
    /// Text color non focus
    /// </summary>
    private readonly Color _textColorNoFocus;

    /// <summary>
    /// Disabled text color
    /// </summary>
    private readonly Color _disabledColor;

    /// <summary>
    /// Indicates, if the button is focused
    /// </summary>
    private bool _focus;

    /// <summary>
    /// Label of the button
    /// </summary>
    private String _label;
    #endregion

    #region ctor
    /// <summary>
    /// Creates the element and retrieves all information from the control
    /// </summary>
    /// <param name="control">GUIControl</param>
    public ButtonElement(GUIControl control)
      : base(control)
    {
      _button = control as GUIButtonControl;
      if (_button != null)
      {
        _font = getFont(_button.FontName);
        _focusBitmap = loadBitmap(_button.TexutureFocusName);
        _noFocusBitmap = loadBitmap(_button.TexutureNoFocusName);
        _hoverBitmap = loadBitmap(_button.HoverFilename);
        _textColor = GetColor(_button.TextColor);
        _textColorNoFocus = GetColor(_button.TextColorNoFocus);
        _disabledColor = GetColor(_button.DisabledColor);
        _label = _button.Label;
        _focus = _button.Focus;
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
        if (_focus)
        {
          if (_focusBitmap != null)
          {
            graph.DrawImage(_focusBitmap, (float)_button.Location.X, (float)_button.Location.Y, (float)_button.Size.Width, (float)_button.Size.Height);
          }
          if (_hoverBitmap != null)
          {
            graph.DrawImage(_hoverBitmap, _button.HoverX, _button.HoverY, _button.HoverWidth, _button.HoverHeight);
          }
        }
        else
        {
          if (_noFocusBitmap != null)
          {
            graph.DrawImage(_noFocusBitmap, (float)_button.Location.X, (float)_button.Location.Y, (float)_button.Size.Width, (float)_button.Size.Height);
          }
        }
        int labelWidth = _button.Width - 2 * _button.TextOffsetX;
        if (labelWidth <= 0)
        {
          return;
        }
        SolidBrush brush = new SolidBrush(_button.Disabled ? _disabledColor : _button.Focus ? _textColor : _textColorNoFocus);

        // render the text on the button
        int x = 0;

        switch (_button.TextAlignment)
        {
          case GUIControl.Alignment.ALIGN_LEFT:
            x = _button.TextOffsetX + _button.XPosition;
            break;

          case GUIControl.Alignment.ALIGN_RIGHT:
            x = _button.XPosition + _button.Width - _button.TextOffsetX;
            break;
        }
        Rectangle rectangle = new Rectangle(x, _button.YPosition + _button.TextOffsetY, labelWidth, _button.Height);
        graph.DrawString(_label, _font, brush, rectangle, StringFormat.GenericTypographic);
        brush.Dispose();
      }
    }

    /// <summary>
    /// Disposes the object
    /// </summary>
    public override void Dispose()
    {
      _font.Dispose();
    }

    /// <summary>
    /// Checks, if an update for the element is needed
    /// </summary>
    /// <returns>true, if an update is needed</returns>
    protected override bool CheckElementSpecificForUpdate()
    {
      bool result = false;
      String newLabel = GUIPropertyManager.Parse(_button.Label);
      if (!newLabel.Equals(_label))
      {
        _label = newLabel;
        result = true;
      }
      if (_button.Focus != _focus)
      {
        _focus = _button.Focus;
        result = true;
      }
      return result;
    }
    #endregion
  }
}
