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
  /// This class represents a GUISliderControl
  /// </summary>
  public class SliderElement : BaseElement
  {
    #region variables
    /// <summary>
    /// GUISliderControl
    /// </summary>
    private readonly GUISliderControl _slider;

    /// <summary>
    /// Background image
    /// </summary>
    private readonly Bitmap _backgroundBitmap;

    /// <summary>
    /// Slider image
    /// </summary>
    private readonly Bitmap _sliderBitmap;

    /// <summary>
    /// Slider focus image
    /// </summary>
    private readonly Bitmap _sliderFocusBitmap;

    /// <summary>
    /// String value of the slider element
    /// </summary>
    private String _strValue;

    /// <summary>
    /// Percentage of the slider element
    /// </summary>
    private int _percentage;

    /// <summary>
    /// Indicates, if the element is focused
    /// </summary>
    private bool _focus;
    #endregion

    #region ctor
    /// <summary>
    /// Creates the element and retrieves all information from the control
    /// </summary>
    /// <param name="control">GUIControl</param>
    public SliderElement(GUIControl control)
      : base(control)
    {
      _slider = control as GUISliderControl;
      if (_slider != null)
      {
        _backgroundBitmap = loadBitmap(_slider.BackGroundTextureName);
        _sliderBitmap = loadBitmap(_slider.BackTextureMidName);
        _sliderFocusBitmap = loadBitmap(_slider.BackTextureMidNameFocus);
        _focus = _slider.Focus;
        _percentage = _slider.Percentage;
        _strValue = getStringValue();
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
        const string strValue = "";
        Font font = getFont("font13");
        float backgroundPositionX = _slider.XPosition;
        float backgroundPositionY = _slider.YPosition;
        if (null != font)
        {
          SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
          graph.DrawString(GUIPropertyManager.Parse(strValue), font, brush, _slider.XPosition, _slider.YPosition);
          brush.Dispose();
        }
        //backgroundPositionX += 60;

        //int iHeight=25;
        graph.DrawImage(_backgroundBitmap, backgroundPositionX, backgroundPositionY, _backgroundBitmap.Width, _backgroundBitmap.Height);
        //_imageBackGround.SetHeight(iHeight);

        float fWidth = (float)(_backgroundBitmap.Width - _sliderBitmap.Width); //-20.0f;
        float fPos = _percentage;
        fPos /= 100.0f;
        fPos *= fWidth;
        fPos += backgroundPositionX;
        //fPos += 10.0f;
        if ((int)fWidth > 1)
        {
          if (_slider.IsFocused)
          {
            graph.DrawImage(_sliderFocusBitmap, fPos, backgroundPositionY);
          }
          else
          {
            graph.DrawImage(_sliderBitmap, fPos, backgroundPositionY);
          }
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
      if (_slider.Percentage != _percentage)
      {
        _percentage = _slider.Percentage;
        result = true;
      }
      if (_slider.Focus != _focus)
      {
        _focus = _slider.Focus;
        result = true;
      }
      String newStrValue = getStringValue();
      if (newStrValue != _strValue)
      {
        _strValue = newStrValue;
        result = true;
      }
      return result;
    }
    #endregion

    #region private methods
    private String getStringValue()
    {
      String strValue = String.Empty;
      switch (_slider.SpinType)
      {
        // Float based slider
        case GUISpinControl.SpinType.SPIN_CONTROL_TYPE_FLOAT:
          strValue = String.Format("{0}", _slider.FloatValue);
          break;
        // Integer based slider
        case GUISpinControl.SpinType.SPIN_CONTROL_TYPE_INT:
          strValue = String.Format("{0}/{1}", _slider.IntValue, 100);
          break;
      }
      return strValue;
    }
    #endregion
  }
}
