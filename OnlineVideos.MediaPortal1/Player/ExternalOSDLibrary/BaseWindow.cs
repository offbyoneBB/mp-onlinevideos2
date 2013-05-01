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
using System.Windows;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// Base class for all MP windows that can be handled by this library
  /// </summary>
  public abstract class BaseWindow : IDisposable
  {
    #region variables
    /// <summary>
    /// List of all elements of the window
    /// </summary>
    protected List<BaseElement> _elementList;

    /// <summary>
    /// List of all eimage elements of the window
    /// </summary>
    protected List<BaseElement> _imageElementList;

    /// <summary>
    /// Indicates, if the window is visible
    /// </summary>
    private bool _visible;

    /// <summary>
    /// Indicates, if the visibility of the window has changed
    /// </summary>
    protected bool _visibleChanged;

    /// <summary>
    /// List of all elements of the window
    /// </summary>
    protected GUIControlCollection _controlList;
    #endregion

    #region protected methods
    /// <summary>
    /// Generates the elements of a window, which are stored in the UIElementCollection
    /// </summary>
    protected void GenerateElements()
    {
      _imageElementList = new List<BaseElement>();
      _elementList = new List<BaseElement>();
      GUIControl controlElement;
      GUIGroup groupElement;
      foreach (var uiElement in _controlList)
      {
        controlElement = uiElement as GUIControl;
        if (controlElement != null)
        {
          if (controlElement.GetType() == typeof(GUIGroup))
          {
            groupElement = controlElement as GUIGroup;
            if (groupElement != null)
              foreach (var uiElement2 in groupElement.Children)
              {
                GUIControl groupControlElement = uiElement2 as GUIControl;
                if (groupControlElement != null)
                {
                  AnalyzeElement(groupControlElement);
                }
              }
          }
          else
          {
            AnalyzeElement(controlElement);
          }
        }
      }
    }

    /// <summary>
    /// Analyzes the found element and creates the corresponding element of this osd.
    /// </summary>
    /// <param name="guiControlElement">Control element to analyze</param>
    private void AnalyzeElement(GUIControl guiControlElement)
    {
      BaseElement element = GenerateElement(guiControlElement);
      if (element != null)
      {
        if (element.GetType() == typeof(ImageElement))
        {
          _imageElementList.Add(element);
        }
        else
        {
          _elementList.Add(element);
        }
      }
    }

    /// <summary>
    /// Generates a single element based on the type of the GUIControl
    /// Supported Types:
    /// - GUIListControl
    /// - GUITextScrollUpControl
    /// - GUICheckMarkControl
    /// - GUISliderControl
    /// - GUIToggleButtonControl
    /// - GUIButtonControl
    /// - GUIFadeLabel
    /// - GUIProgressControl
    /// - GUITVProgressControl
    /// - GUIVolumeBar
    /// - GUILabelControl
    /// - GUIImage
    /// - GUIGroup are handled directly
    /// </summary>
    /// <param name="control">Control</param>
    /// <returns>Element based on the GUIControl</returns>
    public static BaseElement GenerateElement(GUIControl control)
    {
      if (control.GetType() == typeof(GUIImage))
      {
        return new ImageElement(control);
      }
      if (control.GetType() == typeof(GUILabelControl))
      {
        return new LabelElement(control);
      }
      if (control.GetType() == typeof(GUIVolumeBar))
      {
        return new VolumeBarElement(control);
      }
      if (control.GetType() == typeof(GUIProgressControl))
      {
        return new ProgressControlElement(control);
      }
      if (control.GetType() == typeof(GUITVProgressControl))
      {
          return new TVProgressControlElement(control);
      }
      if (control.GetType() == typeof(GUIFadeLabel))
      {
        return new FadeLabelElement(control);
      }
      if (control.GetType() == typeof(GUIButtonControl))
      {
        return new ButtonElement(control);
      }
      if (control.GetType() == typeof(GUICheckButton))
      {
        return new CheckButtonElement(control);
      }
      if (control.GetType() == typeof(GUISliderControl))
      {
        return new SliderElement(control);
      }
      if (control.GetType() == typeof(GUICheckMarkControl))
      {
        return new CheckMarkElement(control);
      }
      if (control.GetType() == typeof(GUITextScrollUpControl))
      {
        return new TextScrollUpElement(control);
      }
      if (control.GetType() == typeof(GUIListControl))
      {
        return new ListElement(control);
      }
      Log.Debug("VIDEOPLAYER_OSD FOUND UNEXPECTED TYPE: " + control.GetType());
      return null;
    }
    #endregion

    #region public methods
    /// <summary>
    /// Draws the window on the given graphics
    /// </summary>
    /// <param name="graph">Graphics of the bitmap</param>
    public void DrawWindow(Graphics graph)
    {
      try
      {
        if (_visible)
        {
          foreach (BaseElement element in _imageElementList)
          {
            element.DrawElement(graph);
          }
          foreach (BaseElement element in _elementList)
          {
            element.DrawElement(graph);
          }
        }
      } catch (Exception ex)
      {
        Log.Error(ex);
      }
    }

    /// <summary>
    /// Indicates if the window is currently visible
    /// </summary>
    /// <returns>true, if window is visible; false otherwise</returns>
    public bool CheckVisibility()
    {
      bool result = CheckSpecificVisibility();
      if (result != _visible)
      {
        _visible = result;
        _visibleChanged = true;
        if(_visibleChanged)
        {
          if (_visible)
          {
            BaseInit();
            GenerateElements();
          }
          else
          {
            Dispose();
          }
        }
      }
      return result;
    }

    /// <summary>
    /// Checks, if an update is needed for this window
    /// </summary>
    /// <returns>true, if an update is needed; false otherwise</returns>
    public bool CheckForUpdate()
    {
      CheckVisibility();
      bool result = false;
      if (_visibleChanged)
      {
        _visibleChanged = false;
        result = true;
      }
      if (_visible)
      {
        foreach (BaseElement element in _imageElementList)
        {
          result = result | element.CheckForUpdate();
        }
        foreach (BaseElement element in _elementList)
        {
          result = result | element.CheckForUpdate();
        }
      }
      return result;
    }
    #endregion

    #region abstract methods
    /// <summary>
    /// Indicates if the window is currently visible
    /// </summary>
    /// <returns>true, if window is visible; false otherwise</returns>
    protected abstract bool CheckSpecificVisibility();

    /// <summary>
    /// Performs a base uinut if the window. This includes the following tasks
    /// - Setting the reference to the window in MP
    /// - Setting the reference to the control list of the MP window
    /// </summary>
    protected abstract void BaseInit();
    #endregion

    #region IDisposable Member
    /// <summary>
    /// Disposes the object
    /// </summary>
    public virtual void Dispose()
    {
      foreach (BaseElement element in _imageElementList)
      {
        element.Dispose();
      }
      foreach (BaseElement element in _elementList)
      {
        element.Dispose();
      }
      _elementList.Clear();
      _imageElementList.Clear();
    }
    #endregion
  }
}
