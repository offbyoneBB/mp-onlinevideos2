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
using System.Windows.Forms;
using MediaPortal.GUI.Library;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// This class is a windows form on which the osd is displayed
  /// </summary>
  public class OSDForm : FloatingWindow
  {
    #region variables    
    /// <summary>
    /// Image to be displayed
    /// </summary>
    private Bitmap _image;
    #endregion

    #region ctor
    /// <summary>
    /// Constructor, which registers the event handler
    /// </summary>
    public OSDForm()
    {
        GUIGraphicsContext.form.LocationChanged += MePoLocationOrSizeChanged;
        GUIGraphicsContext.form.SizeChanged += MePoLocationOrSizeChanged;
    }    
    #endregion

    #region properties
    /// <summary>
    /// Gets/Sets the image, which should be displayed
    /// </summary>
    public Bitmap Image
    {
      get { return _image; }
        set 
        { 
            if (_image != null) _image.Dispose(); 
            _image = value; 
            Invalidate(); 
        }
    }
    #endregion

    #region private methods

    /// <summary>
    /// Event handler to adjust this form to the new location/size of the parent
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void MePoLocationOrSizeChanged(Object sender, EventArgs args)
    {
        Location = GUIGraphicsContext.form.PointToScreen(new Point(0, 0));
        Size = GUIGraphicsContext.form.ClientSize;
    }
    #endregion

    #region protected methods
    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      try
      {
          GUIGraphicsContext.form.LocationChanged -= MePoLocationOrSizeChanged;
          GUIGraphicsContext.form.SizeChanged -= MePoLocationOrSizeChanged;
      } catch (Exception ex)
      {
        Log.Error(ex);
      }
      base.Dispose(disposing);
    }

    protected override void PerformPaint(PaintEventArgs e)
    {
        try
      {
        if (_image != null)
        {
          Graphics graph = e.Graphics;
          graph.SmoothingMode = SmoothingMode.AntiAlias;
          graph.DrawImage(_image, 0, 0, Size.Width, Size.Height);
        }
      } catch (Exception ex)
      {
        Log.Error(ex);
      }
    }    
    #endregion

    #region public methods
    /// <summary>
    /// Shows the form correctly
    /// </summary>
    public void ShowForm()
    {
      Show();
      MePoLocationOrSizeChanged(null, null);
      GUIGraphicsContext.form.Focus();
    }
    #endregion
  }
}
