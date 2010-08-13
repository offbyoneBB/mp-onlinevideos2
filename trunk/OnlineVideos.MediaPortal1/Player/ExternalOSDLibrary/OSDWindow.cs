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

using MediaPortal.GUI.Library;
using MediaPortal.GUI.Video;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// This class handles all related tasks for the GUIVideoOSD window
  /// </summary>
  public class VideoOSDWindow : BaseWindow
  {
    #region variables
    /// <summary>
    /// Video OSD window
    /// </summary>
    private GUIVideoOSD _osdWindow;
    #endregion

    #region ctor
    /// <summary>
    /// Constructor, which creates all elements
    /// </summary>
    public VideoOSDWindow()
    {
      //_osdWindow = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_OSD) as GUIVideoOSD;
      _osdWindow = GUIWindowManager.GetWindow(OnlineVideos.MediaPortal1.Player.GUIOnlineVideoOSD.WINDOW_ONLINEVIDEOS_OSD) as GUIVideoOSD;
      if (_osdWindow != null) _controlList = _osdWindow.controlList;
      GenerateElements();
    }
    #endregion

    #region implemented abstract methods
    /// <summary>
    /// Indicates if the window is currently visible
    /// </summary>
    /// <returns>true, if window is visible; false otherwise</returns>
    protected override bool CheckSpecificVisibility()
    {
        return (int)GUIWindowManager.VisibleOsd == _osdWindow.GetID;
    }

    /// <summary>
    /// Performs a base uinut if the window. This includes the following tasks
    /// - Setting the reference to the window in MP
    /// - Setting the reference to the control list of the MP window
    /// </summary>
    protected override void BaseInit()
    {
      //_osdWindow = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_OSD) as GUIVideoOSD;
      _osdWindow = GUIWindowManager.GetWindow(OnlineVideos.MediaPortal1.Player.GUIOnlineVideoOSD.WINDOW_ONLINEVIDEOS_OSD) as GUIVideoOSD;
      if (_osdWindow != null)
        _controlList = _osdWindow.controlList;
    }
    #endregion
  }
}
