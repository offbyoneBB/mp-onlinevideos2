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

using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// This class handles all related tasks for the GUIDialogMenu (Context menu) window
  /// </summary>
  public class DialogWindow : BaseWindow
  {
    #region variables
    /// <summary>
    /// Dialog Menu window
    /// </summary>
    private GUIDialogMenu _dialogWindow;
    #endregion

    #region ctor
    /// <summary>
    /// Constructor, which creates all elements
    /// </summary>
    public DialogWindow()
    {
      _dialogWindow = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU) as GUIDialogMenu;
      if (_dialogWindow != null) _controlList = _dialogWindow.controlList;
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
      return GUIWindowManager.RoutedWindow == _dialogWindow.GetID;
    }

    /// <summary>
    /// Performs a base uinut if the window. This includes the following tasks
    /// - Setting the reference to the window in MP
    /// - Setting the reference to the control list of the MP window
    /// </summary>
    protected override void BaseInit()
    {
      _dialogWindow = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU) as GUIDialogMenu;
      if (_dialogWindow != null)
        _controlList = _dialogWindow.controlList;
    }
    #endregion
  }
}
