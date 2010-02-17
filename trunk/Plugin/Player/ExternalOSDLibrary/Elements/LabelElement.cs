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
  /// This class represents a GUILabelControl
  /// </summary>
  public class LabelElement : BaseElement
  {
    #region variables
    /// <summary>
    /// GUILabelControl
    /// </summary>
    private readonly GUILabelControl _label;

    /// <summary>
    /// Font
    /// </summary>
    private readonly Font _font;

    /// <summary>
    /// Brush
    /// </summary>
    private readonly Brush _brush;

    /// <summary>
    /// Label String
    /// </summary>
    private String _labelString;
    #endregion

    #region ctor
    /// <summary>
    /// Creates the element and retrieves all information from the control
    /// </summary>
    /// <param name="control">GUIControl</param>
    public LabelElement(GUIControl control)
      : base(control)
    {
      _label = control as GUILabelControl;
      if (_label != null)
      {
        _font = getFont(_label.FontName);
        _brush = new SolidBrush(GetColor(_label.TextColor));
        _labelString = _label.Label;
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
      if (_label.Visible || GUIInfoManager.GetBool(_label.GetVisibleCondition(), _label.ParentID))
      {
        DrawStandard(graph, _labelString);
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
      String newLabel = GUIPropertyManager.Parse(_label.Label);
      if (!newLabel.Equals(_labelString))
      {
        _labelString = newLabel;
        result = true;
      }
      return result;
    }
    #endregion

    #region public methods
    /// <summary>
    /// Draws the element for additional osd informations
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="label">Label content</param>
    /// <param name="strikeout">Strikeout the label, when true</param>
    /// <param name="rectangle">Rectangle for the label</param>
    public void DrawElementAlternative(Graphics graph, String label, bool strikeout, RectangleF rectangle)
    {
      Font temp = _font;
      if (strikeout)
      {
        FontStyle style = _font.Style | FontStyle.Strikeout;
        temp = new Font(_font.FontFamily.Name, _font.Size, style);
      }
      graph.DrawString(label, temp, _brush, rectangle, StringFormat.GenericTypographic);
    }

    /// <summary>
    /// Returns the rectangle for the given label
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="label">Label</param>
    /// <returns>Rectangle</returns>
    public RectangleF GetStringRectangle(Graphics graph, String label)
    {
      GUIControl.Alignment alignment = _label.TextAlignment;
      SizeF size = graph.MeasureString(label, _font);
      RectangleF rectangle;
      if (alignment == GUIControl.Alignment.ALIGN_LEFT)
      {
        rectangle = new RectangleF((float)_label.Location.X, (float)_label.Location.Y, size.Width, _label.Height);
      }
      else rectangle = alignment == GUIControl.Alignment.ALIGN_RIGHT ? new RectangleF((float)_label.Location.X - size.Width, (float)_label.Location.Y, size.Width, _label.Height) : new RectangleF((float)_label.Location.X - (size.Width / 2), (float)_label.Location.Y - (size.Height / 2), size.Width, _label.Height);

      return rectangle;
    }
    #endregion

    #region public overrides methods
    /// <summary>
    /// Draws the element for the cache status. Only implemented in some elements
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="cacheFill">Status of the cache</param>
    public override void DrawCacheStatus(Graphics graph, float cacheFill)
    {
      if (_label.Label.Contains("#currentremaining"))
      {
        DrawStandard(graph, String.Format("{0:00.00}", cacheFill) + " %");
      }
    }
    #endregion

    #region private methods
    /// <summary>
    /// Draws the element in its standard way
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="label">Label</param>
    private void DrawStandard(Graphics graph, String label)
    {
      GUIControl.Alignment alignment = _label.TextAlignment;
      RectangleF rectangle;
      if (alignment == GUIControl.Alignment.ALIGN_LEFT)
      {
        rectangle = new RectangleF((float)_label.Location.X, (float)_label.Location.Y, _label.Width, _label.Height);
      }
      else rectangle = alignment == GUIControl.Alignment.ALIGN_RIGHT ? new RectangleF((float)_label.Location.X - _label.TextWidth, (float)_label.Location.Y, _label.Width, _label.Height) : new RectangleF((float)_label.Location.X - (_label.TextWidth / 2), (float)_label.Location.Y - (_label.TextHeight / 2), _label.Width, _label.Height);
      DrawElementAlternative(graph, label, false, rectangle);
    }
    #endregion
  }
}
