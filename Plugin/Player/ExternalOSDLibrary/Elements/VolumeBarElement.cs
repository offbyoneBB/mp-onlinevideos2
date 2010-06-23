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

using System.Drawing;
using MediaPortal.GUI.Library;

namespace ExternalOSDLibrary
{
  /// <summary>
  /// This class represents a GUIVolumeBar
  /// </summary>
  public class VolumeBarElement : BaseElement
  {
    #region variables
    /// <summary>
    /// GUIVolumeBar
    /// </summary>
    private readonly GUIVolumeBar _volumeBar;

    /// <summary>
    /// Bitmap of the volumebar
    /// </summary>
    private readonly Bitmap _bitmap;

    /// <summary>
    /// Alignment of the volume bar
    /// </summary>
    private readonly GUIControl.Alignment _alignment;

    /// <summary>
    /// Index of the first image
    /// </summary>
    private int _image1;

    /// <summary>
    /// Index of the second image
    /// </summary>
    private int _image2;

    /// <summary>
    /// Maximum value of the volume
    /// </summary>
    private int _maximum;

    /// <summary>
    /// Current value of the volume
    /// </summary>
    private int _current;
    #endregion

    #region ctor
    /// <summary>
    /// Creates the element and retrieves all information from the control
    /// </summary>
    /// <param name="control">GUIControl</param>
    public VolumeBarElement(GUIControl control)
      : base(control)
    {
      _volumeBar = control as GUIVolumeBar;
      if (_volumeBar != null)
      {
        _alignment = _volumeBar.TextAlignment;
        _bitmap = loadBitmap(_volumeBar.TextureName);
        _image1 = _volumeBar.Image1;
        _image2 = _volumeBar.Image2;
        _maximum = _volumeBar.Maximum;
        _current = _volumeBar.Current;
      }
    }
    #endregion

    #region implemented abstract method
    /// <summary>
    /// Draws the element on the given graphics
    /// </summary>
    /// <param name="graph">Graphics</param>
    public override void DrawElement(Graphics graph)
    {
        if (_wasVisible)
      {
        int imageHeight = _volumeBar.ImageHeight;
        int realImageHeight = _volumeBar.TextureHeight / imageHeight;
        int image1 = _volumeBar.Image1;
        int image2 = _volumeBar.Image2;
        int maximum = _volumeBar.Maximum;
        int current = _volumeBar.Current;
        Rectangle sourceRectangle = new Rectangle();
        Rectangle destinationRectangle = new Rectangle();
        sourceRectangle.X = 0;
        sourceRectangle.Y = image1 * realImageHeight;
        sourceRectangle.Width = _volumeBar.TextureWidth;
        sourceRectangle.Height = realImageHeight;
        switch (_alignment)
        {
          case GUIControl.Alignment.ALIGN_LEFT:
            destinationRectangle.X = _volumeBar.XPosition;
            break;
          case GUIControl.Alignment.ALIGN_CENTER:
            destinationRectangle.X = _volumeBar.XPosition - (((maximum * _volumeBar.TextureWidth) - _volumeBar.TextureWidth) / 2);
            break;
          case GUIControl.Alignment.ALIGN_RIGHT:
            destinationRectangle.X = _volumeBar.TextureWidth + _volumeBar.XPosition - (maximum * _volumeBar.TextureWidth);
            break;
        }
        destinationRectangle.Y = _volumeBar.YPosition;
        destinationRectangle.Width = _volumeBar.TextureWidth;
        destinationRectangle.Height = realImageHeight;
        for (int index = 0; index < current; ++index)
        {
          graph.DrawImage(_bitmap, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
          destinationRectangle.X += _volumeBar.TextureWidth;
        }
        if (image2 != image1)
        {
          sourceRectangle.Y = image2 * realImageHeight;
        }
        for (int index = current + 1; index < maximum; ++index)
        {
          graph.DrawImage(_bitmap, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
          destinationRectangle.X += _volumeBar.TextureWidth;
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
      if (_volumeBar.Image1 != _image1)
      {
        _image1 = _volumeBar.Image1;
        result = true;
      }
      if (_volumeBar.Image2 != _image2)
      {
        _image2 = _volumeBar.Image2;
        result = true;
      }
      if (_volumeBar.Current != _current)
      {
        _current = _volumeBar.Current;
        result = true;
      }
      if (_volumeBar.Maximum != _maximum)
      {
        _maximum = _volumeBar.Maximum;
        result = true;
      }
      return result;
    }
    #endregion
  }
}
