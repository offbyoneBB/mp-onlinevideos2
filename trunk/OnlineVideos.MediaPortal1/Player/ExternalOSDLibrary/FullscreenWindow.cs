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
using MediaPortal.GUI.Video;
using System.Windows;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// This class handles all related tasks for the GUIVideoFullscreen window
  /// </summary>
  public class FullscreenWindow : BaseWindow
  {
    #region variables
    /// <summary>
    /// Fullscreen window
    /// </summary>
    private GUIVideoFullscreen _fullscreenWindow;

    /// <summary>
    /// Background image
    /// </summary>
    private ImageElement _background;

    /// <summary>
    /// Background image - alternative
    /// </summary>
    private ImageElement _background2;

    /// <summary>
    /// Background image - alternative
    /// </summary>
    private ImageElement _background3;

    /// <summary>
    /// Label for the additional infos
    /// </summary>
    private LabelElement _label;

    /// <summary>
    /// List of all elements for cache informations
    /// </summary>
    private readonly List<BaseElement> _cacheElements;

    /// <summary>
    /// List of all elements for cache informations
    /// </summary>
    private readonly List<BaseElement> _imageCacheElements;

    /// <summary>
    /// ID of the label
    /// </summary>
    private const int LABEL_ID = 10;

    /// <summary>
    /// ID of the background image
    /// </summary>
    private const int BACKGROUND_ID = 0;

    /// <summary>
    /// ID of the background image
    /// </summary>
    private const int BACKGROUND_ID2 = 104;

    /// <summary>
    /// ID of the Progress bar
    /// </summary>
    private const int PROGRESS_ID = 1;

    /// <summary>
    /// Start ID of the additional elements
    /// </summary>
    private const int PANEL_START = 100;

    /// <summary>
    /// End IF of the additional elements
    /// </summary>
    private const int PANEL_END = 150;
    #endregion

    #region ctor
    /// <summary>
    /// Constructor, which creates all elements
    /// </summary>
    public FullscreenWindow()
    {
      //_fullscreenWindow = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO) as GUIVideoFullscreen;
        _fullscreenWindow = GUIWindowManager.GetWindow(OnlineVideos.MediaPortal1.Player.GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO) as GUIVideoFullscreen;
      if (_fullscreenWindow != null) _controlList = _fullscreenWindow.controlList;
      GenerateElements();
      _cacheElements = new List<BaseElement>();
      _imageCacheElements = new List<BaseElement>();
      GUIControl temp;
      GUIGroup help;
      foreach (UIElement element in _controlList)
      {
        temp = element as GUIControl;
        if (temp != null)
        {
          if (temp.GetType() == typeof(GUIGroup))
          {
            help = temp as GUIGroup;
            if (help != null)
              foreach (UIElement uiElement in help.Children)
              {
                GUIControl temp2 = uiElement as GUIControl;
                if (temp2 != null)
                {
                  CheckElement(temp2);
                }
              }
          }
          CheckElement(temp);
        }
      }
      if (_background == null)
      {
        _background = _background2;
      }
      if (_background == null)
      {
        _background = _background3;
      }
    }
    #endregion

    private void CheckElement(GUIControl temp)
    {
      Log.Info(temp.GetType() + " : " + temp.GetID);
      if (temp.GetID == LABEL_ID)
      {
        if (temp.GetType() == typeof(GUILabelControl))
        {
          _label = new LabelElement(temp);
        }
        else
        {
          Log.Info("VIDEO OSD: TYPE LABEL NOT FOUND FOR LABEL_ID=10 IN FULLSCREEN WINDOW. FOUND: " + temp.GetType());
        }
      }
      if (temp.GetID == BACKGROUND_ID)
      {
        if (temp.GetType() == typeof(GUIImage))
        {
          _background = new ImageElement(temp);
        }
        else
        {
          Log.Info("VIDEO OSD: TYPE IMAGE NOT FOUND FOR BACKGROUND_ID=0 IN FULLSCREEN WINDOW. FOUND: " + temp.GetType());
        }
      }
      if (temp.GetID == BACKGROUND_ID2)
      {
        if (temp.GetType() == typeof(GUIImage))
        {
          _background2 = new ImageElement(temp);
        }
        else
        {
          Log.Info("VIDEO OSD: TYPE IMAGE NOT FOUND FOR BACKGROUND_ID=104 IN FULLSCREEN WINDOW. FOUND: " + temp.GetType());
        }
      }
      if (temp.GetType() == typeof(GUIImage))
      {
        GUIImage imageElement = temp as GUIImage;
        if (imageElement != null)
          if (imageElement.FileName.Equals("osd_bg_top.png"))
          {
            _background3 = new ImageElement(temp);
          }
      }
      if ((temp.GetID == PROGRESS_ID) && (temp.GetType() == typeof(GUIProgressControl) || temp.GetType() == typeof(GUILabelControl))
        || (temp.GetID > PANEL_START && temp.GetID < PANEL_END))
      {
        if (temp.GetType() == typeof(GUIImage))
        {
          _imageCacheElements.Add(GenerateElement(temp));
        }
        else
        {
          _cacheElements.Add(GenerateElement(temp));
        }
      }
    }

    #region implemented abstract methods
    /// <summary>
    /// Indicates if the window is currently visible
    /// </summary>
    /// <returns>true, if window is visible; false otherwise</returns>
    protected override bool CheckSpecificVisibility()
    {
      return GUIWindowManager.ActiveWindow == _fullscreenWindow.GetID;
    }

    /// <summary>
    /// Performs a base uinut if the window. This includes the following tasks
    /// - Setting the reference to the window in MP
    /// - Setting the reference to the control list of the MP window
    /// </summary>
    protected override void BaseInit()
    {
      //_fullscreenWindow = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO) as GUIVideoFullscreen;
      _fullscreenWindow = GUIWindowManager.GetWindow(OnlineVideos.MediaPortal1.Player.GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO) as GUIVideoFullscreen;
      if (_fullscreenWindow != null)
        _controlList = _fullscreenWindow.controlList;
    }
    #endregion

    #region public methods
    /// <summary>
    /// Draw additional informations as standard osd
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="label">Label content</param>
    /// <param name="strikeOut">Strikeout the content, if true</param>
    public void DrawAlternativeOSD(Graphics graph, String label, bool strikeOut)
    {
      if (_background != null && _label != null)
      {

        RectangleF imageRectangle = _background.GetImageRectangle();
        RectangleF labelRectangle = _label.GetStringRectangle(graph, label);
        // Is label greater than image?
        if (labelRectangle.Width > (imageRectangle.Width - 40))
        {
          // We must adjust the rectangle and the position of the label
          float diff = labelRectangle.Width - imageRectangle.Width + 40;
          imageRectangle.X -= diff;
          imageRectangle.Width += diff;
          labelRectangle.X -= diff;
          labelRectangle.X += 20;
        }
        else if (labelRectangle.X + labelRectangle.Width > (imageRectangle.X + imageRectangle.Width - 20))
        {
          // It fits, but is it in the right place?
          float diff = labelRectangle.X + labelRectangle.Width - imageRectangle.X - imageRectangle.Width + 20;
          labelRectangle.X -= diff;
        }
        if (labelRectangle.X < 0)
        {
          labelRectangle.X = 0;
        }
        if (imageRectangle.X < 0)
        {
          imageRectangle.X = 0;
        }
        _background.DrawElementAlternative(graph, imageRectangle);
        _label.DrawElementAlternative(graph, label, strikeOut, labelRectangle);
      }
    }

    /// <summary>
    /// Draws the cache status
    /// </summary>
    /// <param name="graph">Graphics</param>
    /// <param name="cacheFill">Status of the cache</param>
    public void DrawCacheStatus(Graphics graph, float cacheFill)
    {
      foreach (BaseElement element in _imageCacheElements)
      {
        element.DrawCacheStatus(graph, cacheFill);
      }
      foreach (BaseElement element in _cacheElements)
      {
        element.DrawCacheStatus(graph, cacheFill);
      }
    }

    /// <summary>
    /// Dispose the object complete
    /// </summary>
    public void CompleteDispose()
    {
      _label.Dispose();
      _background.Dispose();
      foreach (BaseElement element in _cacheElements)
      {
        if (element!= null) element.Dispose();
      }
      base.Dispose();
    }
    #endregion
  }
}
