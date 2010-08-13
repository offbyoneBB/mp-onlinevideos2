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
using MediaPortal.GUI.Library;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// This class represents a GUIListElement
  /// </summary>
  public class ListElement : BaseElement
  {
    #region internal classes
    /// <summary>
    /// Internal class for ListButtons
    /// </summary>
    private class ListButtonElement : BaseElement
    {
      #region variables
      /// <summary>
      /// Focus
      /// </summary>
      private bool _focus;

      /// <summary>
      /// Image focus
      /// </summary>
      private readonly Bitmap _imageFocus;

      /// <summary>
      /// Image non focus
      /// </summary>
      private readonly Bitmap _imageNonFocus;

      /// <summary>
      /// X Position
      /// </summary>
      private int _positionX;

      /// <summary>
      /// Y Position
      /// </summary>
      private int _positionY;

      /// <summary>
      /// Width
      /// </summary>
      private readonly float _width;

      /// <summary>
      /// Height
      /// </summary>
      private readonly float _height;

      /// <summary>
      /// Indicates if an update is needed
      /// </summary>
      private bool needUpdate;
      #endregion

      #region ctor
      /// <summary>
      /// Creates the listbutton element
      /// </summary>
      /// <param name="positionX">X Position</param>
      /// <param name="positionY">Y Position</param>
      /// <param name="width">Width</param>
      /// <param name="height">Height</param>
      /// <param name="buttonFocusName">FileName of the focus image</param>
      /// <param name="buttonNonFocusName">FileName of the non focus image</param>
      public ListButtonElement(int positionX, int positionY, float width, float height, String buttonFocusName, String buttonNonFocusName)
        : base(null)
      {
        _imageFocus = loadBitmap(buttonFocusName);
        _imageNonFocus = loadBitmap(buttonNonFocusName);
        _positionX = positionX;
        _positionY = positionY;
        _width = width;
        _height = height;
        _focus = false;
      }
      #endregion

      #region implmenented abstract method
      /// <summary>
      /// Draws the element on the given graphics
      /// </summary>
      /// <param name="graph">Graphics</param>
      public override void DrawElement(Graphics graph)
      {
        if (_focus)
        {
          if (_imageFocus != null)
          {
            graph.DrawImage(_imageFocus, _positionX, _positionY, _width, _height);
          }
        }
        else
        {
          if (_imageNonFocus != null)
          {
            graph.DrawImage(_imageNonFocus, _positionX, _positionY, _width, _height);
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
        bool result = needUpdate;
        if (needUpdate)
        {
          needUpdate = false;
        }
        return result;
      }
      #endregion

      #region properties
      /// <summary>
      /// Gets/Sets the focus
      /// </summary>
      public bool Focus
      {
        set
        {
          if (_focus != value)
          {
            needUpdate = true;
          }
          _focus = value;
        }
      }
      #endregion

      #region public methods
      /// <summary>
      /// Sets the position
      /// </summary>
      /// <param name="x">X Position</param>
      /// <param name="y">Y Position</param>
      public void SetPosition(int x, int y)
      {
        _positionX = x;
        _positionY = y;
      }
      #endregion

    }

    /// <summary>
    /// Internal class for ListLabels
    /// </summary>
    private class ListLabelElement : BaseElement
    {
      #region variables
      /// <summary>
      /// Alignment
      /// </summary>
      private GUIControl.Alignment _alignment;

      /// <summary>
      /// X Position
      /// </summary>
      private float _xPosition;

      /// <summary>
      /// Y Position
      /// </summary>
      private float _yPosition;

      /// <summary>
      /// Width
      /// </summary>
      private float _width;

      /// <summary>
      /// Height
      /// </summary>
      private readonly float _height;

      /// <summary>
      /// Font
      /// </summary>
      private Font _font;

      /// <summary>
      /// Brush
      /// </summary>
      private SolidBrush _brush;

      /// <summary>
      /// Label
      /// </summary>
      private String _label;
      #endregion

      #region ctor
      /// <summary>
      /// Creates the element
      /// </summary>
      public ListLabelElement()
        : base(null)
      {
        _xPosition = 0f;
        _yPosition = 0f;
        _width = 0f;
        _height = 0f;
        _label = String.Empty;
      }
      #endregion

      #region implmenented abstract method
      /// <summary>
      /// Draws the element on the given graphics
      /// </summary>
      /// <param name="graph">Graphics</param>
      public override void DrawElement(Graphics graph)
      {
        RectangleF rectangle;
        SizeF stringSize = graph.MeasureString(_label, _font);
        if (_alignment == GUIControl.Alignment.ALIGN_LEFT)
        {
          rectangle = new RectangleF(_xPosition, _yPosition, _width, Math.Max(stringSize.Height, _height));
        }
        else rectangle = _alignment == GUIControl.Alignment.ALIGN_RIGHT ? new RectangleF(_xPosition - stringSize.Width, _yPosition, _width, Math.Max(stringSize.Height, _height)) : new RectangleF(_xPosition - (stringSize.Width / 2), _yPosition - (stringSize.Height / 2), _width, Math.Max(stringSize.Height, _height));
        graph.DrawString(GUIPropertyManager.Parse(_label), _font, _brush, rectangle, StringFormat.GenericTypographic);
      }

      /// <summary>
      /// Disposes the object
      /// </summary>
      public override void Dispose()
      {
        _brush.Dispose();
        _font.Dispose();
      }

      /// <summary>
      /// Checks, if an update for the element is needed
      /// </summary>
      /// <returns>true, if an update is needed</returns>
      protected override bool CheckElementSpecificForUpdate()
      {
        return false;
      }
      #endregion

      #region properties
      /// <summary>
      /// Gets/Sets the alignment
      /// </summary>
      public GUIControl.Alignment Alignment
      {
        set { _alignment = value; }
      }

      /// <summary>
      /// Gets/Sets the x position
      /// </summary>
      public float XPosition
      {
        get { return _xPosition; }
        set { _xPosition = value; }
      }

      /// <summary>
      /// Gets/Sets the y position
      /// </summary>
      public float YPosition
      {
        set { _yPosition = value; }
      }

      /// <summary>
      /// Gets/Sets the width
      /// </summary>
      public float Width
      {
        set { _width = value; }
      }

      /// <summary>
      /// Gets/Sets the label
      /// </summary>
      public String Label
      {
        set { _label = value; }
      }

      /// <summary>
      /// Gets/Sets the font
      /// </summary>
      public Font Font
      {
        set { _font = value; }
      }

      /// <summary>
      /// Gets/Sets the brush
      /// </summary>
      public SolidBrush Brush
      {
        set { _brush = value; }
      }
      #endregion

      #region public methods
      /// <summary>
      /// Returns the size of the string of this listlabel element
      /// </summary>
      /// <param name="graph">Graphics</param>
      /// <returns>Size of the label</returns>
      public SizeF GetStringSize(Graphics graph)
      {
        return graph.MeasureString(_label, _font);
      }
      #endregion
    }
    #endregion

    #region variables
    /// <summary>
    /// GUIListControl
    /// </summary>
    private readonly GUIListControl _list;

    /// <summary>
    /// Offset
    /// </summary>
    private int _offset;

    /// <summary>
    /// Cursor position
    /// </summary>
    private int _cursorX;

    /// <summary>
    /// List of the buttons
    /// </summary>
    private readonly List<ListButtonElement> _listButtons;

    /// <summary>
    /// List of the items
    /// </summary>
    private List<GUIListItem> _listItems;

    /// <summary>
    /// List of the Label1's
    /// </summary>
    private readonly List<ListLabelElement> _labelControls1;

    /// <summary>
    /// List of the Label2's
    /// </summary>
    private readonly List<ListLabelElement> _labelControls2;

    /// <summary>
    /// List of the Label3's
    /// </summary>
    private readonly List<ListLabelElement> _labelControls3;

    /// <summary>
    /// Cached Images
    /// </summary>
    private readonly Dictionary<String, Bitmap> _cachedBitmaps;

    /// <summary>
    /// Text line
    /// </summary>
    private String _textLine;

    /// <summary>
    /// VerticalScrollbarElement
    /// </summary>
    private readonly VerticalScrollBarElement _verticalScrollBarElement;

    /// <summary>
    /// Indicates, if the list is focused
    /// </summary>
    private bool _focus;
    #endregion

    #region ctor
    /// <summary>
    /// Creates the element and retrieves all information from the control
    /// </summary>
    /// <param name="control">GUIControl</param>
    public ListElement(GUIControl control)
      : base(control)
    {
      _list = control as GUIListControl;
      if (_list != null)
      {
        _cursorX = _list.CursorX;
        _offset = _list.Offset;
        _listButtons = new List<ListButtonElement>();
        _labelControls1 = new List<ListLabelElement>();
        _labelControls2 = new List<ListLabelElement>();
        _labelControls3 = new List<ListLabelElement>();
        _cachedBitmaps = new Dictionary<String, Bitmap>();
        AllocButtons(_list.SpinX, _list.SpinY);
        initializeLabels();
        _verticalScrollBarElement = new VerticalScrollBarElement(_list.Scrollbar);
        _focus = _list.IsFocused;
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
        _listItems = _list.ListItems;
        int dwPosY = _list.YPosition;
        // Render the buttons first.
        for (int i = 0; i < _list.ItemsPerPage; i++)
        {
          if (i + _offset < _listItems.Count)
          {
            // render item
            bool gotFocus = false;
            if (_list.DrawFocus && i == _cursorX && _list.IsFocused && _list.TypeOfList == GUIListControl.ListType.CONTROL_LIST)
              gotFocus = true;
            RenderButton(graph, i, _list.XPosition, dwPosY, gotFocus);
          }
          dwPosY += _list.ItemHeight + _list.Space;
        }

        // Render new item list
        dwPosY = _list.YPosition;
        for (int i = 0; i < _list.ItemsPerPage; i++)
        {
          int dwPosX = _list.XPosition;
          if (i + _offset < _listItems.Count)
          {
            int iconX;
            int labelX;
            int pinX;

            int ten = 10;
            GUIGraphicsContext.ScaleHorizontal(ref ten);
            switch (_list.TextAlignment)
            {
              case GUIControl.Alignment.ALIGN_RIGHT:
                iconX = dwPosX + _list.Width - _list.IconOffsetX - _list.ImageWidth;
                labelX = dwPosX;
                pinX = dwPosX + _list.Width - _list.PinIconWidth;
                break;
              default:
                iconX = dwPosX + _list.IconOffsetX;
                labelX = dwPosX + _list.ImageWidth + ten;
                pinX = dwPosX;
                break;
            }

            // render the icon
            RenderIcon(graph, i, iconX, dwPosY + _list.IconOffsetY);

            // render the text
            RenderLabel(graph, i, labelX, dwPosY);

            RenderPinIcon(graph, i, pinX, dwPosY);

            dwPosY += _list.ItemHeight + _list.Space;
          } //if (i + _offset < _listItems.Count)
        } //for (int i = 0; i < _itemsPerPage; i++)

        RenderScrollbar(graph);
      }
    }

    /// <summary>
    /// Disposes the object
    /// </summary>
    public override void Dispose()
    {
      foreach (ListButtonElement element in _listButtons)
      {
        element.Dispose();
      }
      foreach (ListLabelElement element in _labelControls1)
      {
        element.Dispose();
      }
      foreach (ListLabelElement element in _labelControls2)
      {
        element.Dispose();
      }
      foreach (ListLabelElement element in _labelControls3)
      {
        element.Dispose();
      }
    }

    /// <summary>
    /// Checks, if an update for the element is needed
    /// </summary>
    /// <returns>true, if an update is needed</returns>
    protected override bool CheckElementSpecificForUpdate()
    {
      bool result = false;
      int newOffset = _list.Offset;
      int newCursorX = _list.CursorX;
      if (_offset != newOffset)
      {
        _offset = newOffset;
        result = true;
      }
      if (newCursorX != _cursorX)
      {
        _cursorX = newCursorX;
        result = true;
      }
      if (_list.IsFocused != _focus)
      {
        _focus = _list.IsFocused;
        result = true;
      }
      return result;
    }
    #endregion

    #region private methods
    /// <summary>
    /// Allocates the listbuttons
    /// </summary>
    /// <param name="spinControlPositionX">X Position</param>
    /// <param name="spinControlPositionY">Y Position</param>
    private void AllocButtons(int spinControlPositionX, int spinControlPositionY)
    {
      for (int i = 0; i < _list.ItemsPerPage; ++i)
      {
        ListButtonElement cntl = new ListButtonElement(spinControlPositionX, spinControlPositionY, _list.Width, _list.ItemHeight, _list.ButtonFocusName, _list.ButtonNoFocusName);
        _listButtons.Add(cntl);
      }
    }

    /// <summary>
    /// Initialize the labels
    /// </summary>
    private void initializeLabels()
    {
      for (int i = 0; i < _list.ItemsPerPage; ++i)
      {
        ListLabelElement cntl1 = new ListLabelElement();
        cntl1.Font = getFont(_list.FontName);
        cntl1.Brush = new SolidBrush(GetColor(_list.TextColor));
        cntl1.Alignment = GUIControl.Alignment.ALIGN_LEFT;
        ListLabelElement cntl2 = new ListLabelElement();
        cntl2.Font = getFont(_list.FontName2);
        cntl2.Brush = new SolidBrush(GetColor(_list.TextColor2));
        cntl2.Alignment = GUIControl.Alignment.ALIGN_LEFT;
        ListLabelElement cntl3 = new ListLabelElement();
        cntl3.Font = getFont(_list.Font3);
        cntl3.Brush = new SolidBrush(GetColor(_list.TextColor3));
        cntl3.Alignment = GUIControl.Alignment.ALIGN_RIGHT;
        _labelControls1.Add(cntl1);
        _labelControls2.Add(cntl2);
        _labelControls3.Add(cntl3);
      }
    }

    /// <summary>
    /// Renders the label
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="buttonNr">Number of the button</param>
    /// <param name="dwPosX">X Position</param>
    /// <param name="dwPosY">Y Position</param>
    private void RenderLabel(Graphics graph, int buttonNr, int dwPosX, int dwPosY)
    {
      GUIListItem pItem = _listItems[buttonNr + _offset];
      long dwColor;
      if (pItem.Shaded)
        dwColor = _list.ShadedColor;

      if (pItem.Selected)
        dwColor = _list.SelectedColor;

      dwPosX += _list.TextOffsetX;
      int dMaxWidth = (_list.Width - _list.TextOffsetX - _list.ImageWidth - GUIGraphicsContext.ScaleHorizontal(20));
      if ((_list.TextVisible2 && pItem.Label2.Length > 0) &&
        (_list.TextOffsetY == _list.TextOffsetY2))
      {
        dwColor = _list.TextColor2;

        if (pItem.Selected)
          dwColor = _list.SelectedColor2;

        if (pItem.IsPlayed)
          dwColor = _list.PlayedColor;

        if (pItem.IsRemote)
        {
          dwColor = _list.RemoteColor;
          if (pItem.IsDownloading)
            dwColor = _list.DownloadColor;
        }

        int xpos = dwPosX;
        int ypos = dwPosY;

        if (0 == _list.TextOffsetX2)
          xpos = _list.XPosition + _list.Width - GUIGraphicsContext.ScaleHorizontal(16);
        else
          xpos = _list.XPosition + _list.TextOffsetX2;

        if ((_labelControls2 != null) &&
          (buttonNr >= 0) && (buttonNr < _labelControls2.Count))
        {
          ListLabelElement label2 = _labelControls2[buttonNr];
          if (label2 != null)
          {
            label2.XPosition = xpos;
            label2.YPosition = ypos + GUIGraphicsContext.ScaleVertical(2) + _list.TextOffsetY2;

            label2.Label = pItem.Label2;
            label2.Alignment = GUIControl.Alignment.ALIGN_RIGHT;
            label2.Font = getFont(_list.FontName2);
            SizeF stringSize = label2.GetStringSize(graph);
            dMaxWidth = (int)label2.XPosition - dwPosX - (int)stringSize.Width - GUIGraphicsContext.ScaleHorizontal(20);
          }
        }
      }

      _textLine = pItem.Label;
      if (_list.TextVisible1)
      {
        dwColor = _list.TextColor;

        if (pItem.Selected)
          dwColor = _list.SelectedColor;

        if (pItem.IsPlayed)
          dwColor = _list.PlayedColor;

        if (pItem.IsRemote)
        {
          dwColor = _list.RemoteColor;
          if (pItem.IsDownloading)
            dwColor = _list.DownloadColor;
        }

        RenderText(graph, buttonNr, (float)dwPosX, (float)dwPosY + GUIGraphicsContext.ScaleVertical(2) + _list.TextOffsetY, (float)dMaxWidth, dwColor, _textLine);
      }

      if (pItem.Label2.Length > 0)
      {
        dwColor = _list.TextColor2;

        if (pItem.Selected)
          dwColor = _list.SelectedColor2;

        if (pItem.IsPlayed)
          dwColor = _list.PlayedColor;

        if (pItem.IsRemote)
        {
          dwColor = _list.RemoteColor;
          if (pItem.IsDownloading)
            dwColor = _list.DownloadColor;
        }
        if (0 == _list.TextOffsetX2)
          dwPosX = _list.XPosition + _list.Width - GUIGraphicsContext.ScaleHorizontal(16);
        else
          dwPosX = _list.XPosition + _list.TextOffsetX2;

        _textLine = pItem.Label2;

        if (_list.TextVisible2 &&
          (_labelControls2 != null) &&
          (buttonNr >= 0) && (buttonNr < _labelControls2.Count))
        {
          ListLabelElement label2 = _labelControls2[buttonNr];
          if (label2 != null)
          {
            label2.XPosition = dwPosX - GUIGraphicsContext.ScaleHorizontal(6);
            label2.YPosition = dwPosY + GUIGraphicsContext.ScaleVertical(2) + _list.TextOffsetY2;
            label2.Label = _textLine;
            label2.Alignment = GUIControl.Alignment.ALIGN_RIGHT;
            label2.Font = getFont(_list.FontName2);
            label2.DrawElement(graph);
            label2 = null;
          }
        }
      }

      if (pItem.Label3.Length > 0)
      {
        dwColor = _list.TextColor3;

        if (pItem.Selected)
          dwColor = _list.SelectedColor3;

        if (pItem.IsPlayed)
          dwColor = _list.PlayedColor;

        if (pItem.IsRemote)
        {
          dwColor = _list.RemoteColor;
          if (pItem.IsDownloading)
            dwColor = _list.DownloadColor;
        }

        if (0 == _list.TextColor3)
          dwPosX = _list.XPosition + _list.TextOffsetX;
        else
          dwPosX = _list.XPosition + _list.TextOffsetX3;

        int ypos = dwPosY;

        if (0 == _list.TextOffsetY3)
          ypos += _list.TextOffsetY2;
        else
          ypos += _list.TextOffsetY3;

        if (_list.TextVisible3 &&
          (_labelControls3 != null) &&
          (buttonNr >= 0) && (buttonNr < _labelControls3.Count))
        {
          ListLabelElement label3 = _labelControls3[buttonNr];

          if (label3 != null)
          {
            label3.XPosition = dwPosX;
            label3.YPosition = ypos;
            label3.Label = pItem.Label3;
            label3.Alignment = GUIControl.Alignment.ALIGN_LEFT;
            label3.Font = getFont(_list.Font3);
            label3.Width = (_list.Width - _list.TextOffsetX - _list.ImageWidth - GUIGraphicsContext.ScaleHorizontal(34));
            label3.DrawElement(graph);
          }
        }
      }
    }

    /// <summary>
    /// Renders the text
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="Item">Item number</param>
    /// <param name="fPosX">X Position</param>
    /// <param name="fPosY">Y Position</param>
    /// <param name="fMaxWidth">Width</param>
    /// <param name="dwTextColor">Text Color</param>
    /// <param name="strTextToRender">Text to render</param>
    private void RenderText(Graphics graph, int Item, float fPosX, float fPosY, float fMaxWidth, long dwTextColor, string strTextToRender)
    {
      if (_labelControls1 == null)
        return;
      if (Item < 0 || Item >= _labelControls1.Count)
        return;

      ListLabelElement label = _labelControls1[Item];

      if (label == null)
        return;
      SizeF stringSize = label.GetStringSize(graph);
      if (_list.TextAlignment == GUIControl.Alignment.ALIGN_RIGHT && stringSize.Width < fMaxWidth)
      {
        label.XPosition = fPosX + fMaxWidth;
        label.YPosition = fPosY;
      }
      else
      {
        label.XPosition = fPosX;
        label.YPosition = fPosY;
      }
      label.Brush = new SolidBrush(GetColor(dwTextColor));
      label.Label = strTextToRender;
      label.Width = fMaxWidth;
      label.Alignment = stringSize.Width < fMaxWidth ? _list.TextAlignment : GUIControl.Alignment.ALIGN_LEFT;
      label.Font = getFont(_list.FontName);
      label.DrawElement(graph);
    }

    /// <summary>
    /// Render the button
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="buttonNr">Number of the button</param>
    /// <param name="x">X Position</param>
    /// <param name="y">Y Position</param>
    /// <param name="gotFocus">true, when focus; false otherwise</param>
    private void RenderButton(Graphics graph, int buttonNr, int x, int y, bool gotFocus)
    {
      if (_listButtons != null)
      {
        if (buttonNr >= 0 && buttonNr < _listButtons.Count)
        {
          ListButtonElement btn = _listButtons[buttonNr];
          if (btn != null)
          {
            btn.Focus = gotFocus;
            btn.SetPosition(x, y);
            btn.DrawElement(graph);
          }
        }
      }
    }

    /// <summary>
    /// Render icon
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="buttonNr">Button number</param>
    /// <param name="x">X Position</param>
    /// <param name="y">Y Position</param>
    private void RenderIcon(Graphics graph, int buttonNr, int x, int y)
    {
      GUIListItem pItem = _listItems[buttonNr + _offset];
      if (pItem.HasIcon)
      {
        // show icon
        GUIImage pImage = pItem.Icon;
        if (null == pImage)
        {
          return;
        }
        Bitmap bitmap;
        if (_cachedBitmaps.ContainsKey(pImage.FileName))
        {
          bitmap = _cachedBitmaps[pImage.FileName];
        }
        else
        {
          bitmap = loadBitmap(pImage.FileName);
          if (bitmap != null)
          {
            _cachedBitmaps.Add(pImage.FileName, bitmap);
          }
        }
        if (bitmap != null)
        {
          graph.DrawImage(bitmap, x, y, _list.ImageWidth, _list.ImageHeight);
        }
      }
    }

    /// <summary>
    /// Render pin icon
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="buttonNr">Button number</param>
    /// <param name="x">X Position</param>
    /// <param name="y">Y Position</param>
    private void RenderPinIcon(Graphics graph, int buttonNr, int x, int y)
    {
      GUIListItem pItem = _listItems[buttonNr + _offset];
      if (pItem.HasPinIcon)
      {
        GUIImage pinImage = pItem.PinIcon;
        if (null == pinImage)
        {
          return;
        }
        Bitmap bitmap;
        if (_cachedBitmaps.ContainsKey(pinImage.FileName))
        {
          bitmap = _cachedBitmaps[pinImage.FileName];
        }
        else
        {
          bitmap = loadBitmap(pinImage.FileName);
          if (bitmap != null)
          {
            _cachedBitmaps.Add(pinImage.FileName, bitmap);
          }
        }
        if (bitmap != null)
        {
          Point position = new Point();
          if (_list.PinIconOffsetY < 0 || _list.PinIconOffsetX < 0)
          {
            position.X = x + (_list.Width) - (pinImage.TextureWidth + pinImage.TextureWidth / 2);
            position.Y = y + (_list.Height / 2) - (pinImage.TextureHeight / 2);
          }
          else
          {
            position.X = x + _list.PinIconOffsetX;
            position.Y = y + _list.PinIconOffsetY;
          }
          graph.DrawImage(bitmap, position.X, position.Y, _list.PinIconWidth, _list.PinIconHeight);
        }
      }
    }

    /// <summary>
    /// Render the scroll bar
    /// </summary>
    /// <param name="graph">Graphics</param>
    private void RenderScrollbar(Graphics graph)
    {
      if (_listItems.Count > _list.ItemsPerPage)
      {
        if (_verticalScrollBarElement != null)
        {
          _verticalScrollBarElement.DrawElement(graph);
        }
      }
    }
    #endregion
  }
}
