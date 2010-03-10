using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class VideoResponse
  {
    private VideoEntity video;

    public VideoEntity Video
    {
      get { return video; }
      set { video = value; }
    }

    private ImageEntity image;
    public ImageEntity Image
    {
      get { return image; }
      set { image = value; }
    }

    private ArtistEntity artist;

    public ArtistEntity Artist
    {
      get { return artist; }
      set { artist = value; }
    }

    private List<CategoryEntity> categories;

    public List<CategoryEntity> Categories
    {
      get { return categories; }
      set { categories = value; }
    }

    private List<AlbumEntity> albums;

    public List<AlbumEntity> Albums
    {
      get { return albums; }
      set { albums = value; }
    }

    public VideoResponse()
    {
      this.Albums = new List<AlbumEntity>();
      this.Artist = new ArtistEntity();
      this.Categories = new List<CategoryEntity>();
      this.Image = new ImageEntity();
      this.Video = new VideoEntity();
    }
  }
}
