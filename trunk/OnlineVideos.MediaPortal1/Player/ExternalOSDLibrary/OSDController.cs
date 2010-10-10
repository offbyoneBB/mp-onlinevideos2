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
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Configuration;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// Controller for the ExternalOSDLibrary. This is the main entry point for usage in an external player
  /// </summary>
  public class OSDController : IDisposable
  {
    #region variables
    /// <summary>
    /// Singleton instance
    /// </summary>
    private static OSDController singleton;

    /// <summary>
    /// Fullscreen window
    /// </summary>
    private readonly FullscreenWindow _fullscreenWindow;

    /// <summary>
    /// Video OSD window
    /// </summary>
    private readonly VideoOSDWindow _videoOSDWindow;

    /// <summary>
    /// Dialog (Context) window
    /// </summary>
    private readonly DialogWindow _dialogWindow;

    /// <summary>
    /// Form of the osd
    /// </summary>
    private readonly OSDForm _osdForm;
    
    /// <summary>
    /// Indicates, if additional osd information is displayed
    /// </summary>
    private bool _showAdditionalOSD;

    /// <summary>
    /// Label of the additional osd
    /// </summary>
    private String _label;

    /// <summary>
    /// Strikeout the label of the addional osd
    /// </summary>
    private bool _strikeOut;

    /// <summary>
    /// Time of the last update
    /// </summary>
    private DateTime _lastUpdate;

    /// <summary>
    /// Satus of the cache
    /// </summary>
    private float _cacheFill;

    /// <summary>
    /// Indicates, if the cache status should be displayed
    /// </summary>
    private bool _showCacheStatus;

    /// <summary>
    /// Indicates, if the init label should be displayed
    /// </summary>
    private bool _showInit;

    /// <summary>
    /// Indicates, if an update is needed
    /// </summary>
    private bool _needUpdate;

    /// <summary>
    /// Indicates if MP is minimized
    /// </summary>
    private bool _minimized;

    /// <summary>
    /// Indicates if the screen should be blanked in fullscreen
    /// </summary>
    private readonly bool _blankScreen;

    /// <summary>
    /// Indicates if player is in fullscreen;
    /// </summary>
    private bool _fullscreen;

    /// <summary>
    /// Rectangle of the video window
    /// </summary>
    private Rectangle _videoRectangle;
    #endregion

    #region ctor
    /// <summary>
    /// Returns the singleton instance
    /// </summary>
    /// <returns>Singleton instance</returns>
    public static OSDController getInstance()
    {
      if (singleton == null)
      {
        singleton = new OSDController();
      }
      return singleton;
    }

    /// <summary>
    /// Constructor which initializes the osd controller
    /// </summary>
    private OSDController()
    {
      _fullscreenWindow = new FullscreenWindow();
      _videoOSDWindow = new VideoOSDWindow();
      _dialogWindow = new DialogWindow();
      _osdForm = new OSDForm();
      GUIGraphicsContext.form.SizeChanged += parent_SizeChanged;
      using (MediaPortal.Profile.Settings settings = new MediaPortal.Profile.MPSettings())
      {
        _blankScreen = settings.GetValueAsBool("externalOSDLibrary", "blankScreen", false);
      }
    }
    #endregion

    #region public methods
    /// <summary>
    /// Activates the osd. This methods must be called first, otherwise nothing will displayed
    /// </summary>
    public void Activate()
    {
      _fullscreen = g_Player.Player != null && g_Player.FullScreen;
      if (GUIGraphicsContext.VideoWindow != null)
      {
        _videoRectangle = GUIGraphicsContext.VideoWindow;
      }
      _osdForm.ShowForm();
      _needUpdate = true;
      UpdateGUI();
    }

    /// <summary>
    /// Performs an update on the osd, should be called from the process method of the player
    /// </summary>
    public void UpdateGUI()
    {
      bool a1 = _needUpdate;
      bool a2 = _videoOSDWindow.CheckForUpdate();
      bool a3 = _dialogWindow.CheckForUpdate();
      bool a4 = _fullscreenWindow.CheckForUpdate();

      bool update = a1 | a2 | a3 | a4;

      if (_needUpdate)
      {
        _needUpdate = false;
      }
      else
      {
        if (_showAdditionalOSD)
        {
          TimeSpan ts = DateTime.Now - _lastUpdate;
          if (ts.Seconds >= 3)
          {
            _showAdditionalOSD = false;
            update = true;
          }
        }
      }
      if (g_Player.Player != null && _fullscreen != g_Player.FullScreen)
      {
        _fullscreen = g_Player.FullScreen;
        update = true;
      }
      if (GUIGraphicsContext.VideoWindow != null && !GUIGraphicsContext.VideoWindow.Equals(_videoRectangle))
      {
        _videoRectangle = GUIGraphicsContext.VideoWindow;
        update = true;
      }
      if (update && osdForm.Width > 0 && _osdForm.Height > 0)
      {
        Bitmap image = new Bitmap(_osdForm.Width, _osdForm.Height);
        Graphics graph = Graphics.FromImage(image);
        if (_blankScreen && GUIGraphicsContext.Fullscreen)
        {
          if (_fullscreen)
          {
            graph.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 0)), new Rectangle(0, 0, _osdForm.Size.Width, _osdForm.Size.Height));
          }
          else
          {
            graph.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 0)), _videoRectangle);
          }
        }
        graph.TextRenderingHint = TextRenderingHint.AntiAlias;
        graph.SmoothingMode = SmoothingMode.AntiAlias;
        if (_showAdditionalOSD)
        {
          _fullscreenWindow.DrawAlternativeOSD(graph, _label, _strikeOut);
        }
        if (_showInit)
        {
          _fullscreenWindow.DrawAlternativeOSD(graph, _label, false);
        }
        if (_showCacheStatus)
        {
          _fullscreenWindow.DrawCacheStatus(graph, _cacheFill);
        }
        _videoOSDWindow.DrawWindow(graph); 
        _fullscreenWindow.DrawWindow(graph);       
        _dialogWindow.DrawWindow(graph);
        graph.Dispose();
        GC.Collect();
        _osdForm.Image = image;
      }
    }

    /// <summary>
    /// Deactivates the osd. Nothing will be displayed until it will be reactivated.
    /// </summary>
    public void Deactivate()
    {
      _osdForm.Hide();
    }
    
    /// <summary>
    /// Shows additional osd information
    /// </summary>
    /// <param name="label">Label content</param>
    /// <param name="strikeOut">srikeout the label, if true</param>
    public void ShowAlternativeOSD(String label, bool strikeOut)
    {
      _label = label;
      _strikeOut = strikeOut;
      _lastUpdate = DateTime.Now;
      _showAdditionalOSD = true;
      _showCacheStatus = false;
      _needUpdate = true;
    }

    /// <summary>
    /// Shows the cache status
    /// </summary>
    /// <param name="cacheFill"></param>
    public void ShowCacheStatus(float cacheFill)
    {
      _cacheFill = cacheFill;
      _showAdditionalOSD = false;
      _showCacheStatus = true;
      _needUpdate = true;
    }

    /// <summary>
    /// Hides the cache status
    /// </summary>
    public void HideCacheStatus()
    {
      _showCacheStatus = false;
      _needUpdate = true;
    }

    /// <summary>
    /// Shows the init message
    /// </summary>
    /// <param name="label">Label of the init</param>
    public void ShowInit(String label)
    {
      _label = label;
      _showInit = true;
      _showCacheStatus = false;
      _showAdditionalOSD = false;
      _needUpdate = true;
    }

    /// <summary>
    /// Hides the init message
    /// </summary>
    public void HideInit()
    {
      _showInit = false;
      _needUpdate = true;
    }
    #endregion

    #region private methods
    /// <summary>
    /// Event handler to adjust this form to the new location/size of the parent
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void parent_SizeChanged(Object sender, EventArgs args)
    {
        if (GUIGraphicsContext.form.WindowState == FormWindowState.Minimized)
      {
        _minimized = true;
        return;
      }
      if (!_minimized)
      {
        singleton.Dispose();
      }
      _minimized = false;
    }
    #endregion

    #region IDisposable Member
    /// <summary>
    /// Disposes the osd controller
    /// </summary>
    public void Dispose()
    {
      _fullscreenWindow.CompleteDispose();
      _dialogWindow.Dispose();
      _videoOSDWindow.Dispose();
      _osdForm.Dispose();
      GUIGraphicsContext.form.SizeChanged -= parent_SizeChanged;
      ImageCache.Dispose();
      singleton = null;
    }
    #endregion
  }
}
