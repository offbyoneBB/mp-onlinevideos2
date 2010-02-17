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
  /// This class represents a GUICheckMarkControl
  /// </summary>
  public class CheckMarkElement : BaseElement
  {
    #region variables
    /// <summary>
    /// GUICheckMarkControl
    /// </summary>
    private readonly GUICheckMarkControl _checkMark;

    /// <summary>
    /// Check focus image
    /// </summary>
    private readonly Bitmap _checkFocusBitmap;

    /// <summary>
    /// Check non focus image
    /// </summary>
    private readonly Bitmap _checkNoFocusBitmap;

    /// <summary>
    /// Font
    /// </summary>
    private readonly Font _font;

    /// <summary>
    /// Disabled text color
    /// </summary>
    private readonly Color _disabledColor;

    /// <summary>
    /// Text color
    /// </summary>
    private readonly Color _textColor;

    /// <summary>
    /// Indicates, if the checkmark is focused
    /// </summary>
    private bool _focus;

    /// <summary>
    /// Indicates, if the checkmark is selected
    /// </summary>
    private bool _selected;

    /// <summary>
    /// Indicates, if the checkmark is disabled
    /// </summary>
    private bool _disabled;

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
    public CheckMarkElement(GUIControl control)
      : base(control)
    {
      _checkMark = control as GUICheckMarkControl;
      if (_checkMark != null)
      {
        _checkFocusBitmap = loadBitmap(_checkMark.CheckMarkTextureName);
        _checkNoFocusBitmap = loadBitmap(_checkMark.CheckMarkTextureNameNF);
        _font = getFont(_checkMark.FontName);
        _disabledColor = GetColor(_checkMark.DisabledColor);
        _textColor = GetColor(_checkMark.TextColor);
        _focus = _checkMark.Focus;
        _selected = _checkMark.Selected;
        _disabled = _checkMark.Disabled;
        _label = _checkMark.Label;
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
      if (_checkMark.Visible || GUIInfoManager.GetBool(_checkMark.GetVisibleCondition(), _checkMark.ParentID))
      {
        int dwTextPosX = _checkMark.XPosition;
        int dwCheckMarkPosX = _checkMark.XPosition;
        Rectangle _rectangle = new Rectangle();
        _rectangle.X = _checkMark.YPosition;
        _rectangle.Y = _checkMark.YPosition;
        _rectangle.Height = _checkFocusBitmap.Height;
        if (null != _font)
        {
          SizeF sizeF;
          if (_checkMark.TextAlignment == GUIControl.Alignment.ALIGN_LEFT)
          {
            sizeF = graph.MeasureString(GUIPropertyManager.Parse(_label), _font);
            dwCheckMarkPosX += ((int)(sizeF.Width) + 5);
          }
          else
          {
            dwTextPosX = (dwCheckMarkPosX + _checkFocusBitmap.Width + 5);
            graph.MeasureString(GUIPropertyManager.Parse(_label), _font);
          }
          if (_disabled)
          {
            SolidBrush brush = new SolidBrush(_disabledColor);
            graph.DrawString(GUIPropertyManager.Parse(_label), _font, brush, dwTextPosX, _checkMark.YPosition);
            brush.Dispose();
          }
          else
          {
            if (_focus)
            {
              SolidBrush brush = new SolidBrush(_textColor);
              graph.DrawString(GUIPropertyManager.Parse(_label), _font, brush, dwTextPosX, _checkMark.YPosition);
              brush.Dispose();
            }
            else
            {
              SolidBrush brush = new SolidBrush(_disabledColor);
              graph.DrawString(GUIPropertyManager.Parse(_label), _font, brush, dwTextPosX, _checkMark.YPosition);
              brush.Dispose();
            }
          }
        }
        if (_selected)
        {
          graph.DrawImage(_checkFocusBitmap, dwCheckMarkPosX, _checkMark.YPosition);
        }
        else
        {
          graph.DrawImage(_checkNoFocusBitmap, dwCheckMarkPosX, _checkMark.YPosition);
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
      String newLabel = GUIPropertyManager.Parse(_checkMark.Label);
      if (!newLabel.Equals(_label))
      {
        _label = newLabel;
        result = true;
      }
      if (_checkMark.Focus != _focus)
      {
        _focus = _checkMark.Focus;
        result = true;
      }
      if (_checkMark.Disabled != _disabled)
      {
        _disabled = _checkMark.Disabled;
        result = true;
      }
      if (_checkMark.Selected != _selected)
      {
        _selected = _checkMark.Selected;
        result = true;
      }
      return result;
    }
    #endregion
  }
}
