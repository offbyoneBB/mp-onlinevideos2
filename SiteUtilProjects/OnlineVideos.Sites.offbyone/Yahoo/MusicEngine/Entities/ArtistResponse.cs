using System;
using System.Collections.Generic;
using System.Text;
using YahooMusicEngine.Entities;

namespace YahooMusicEngine.Entities
{
  public class ArtistResponse
  {

    private ArtistEntity artist;

    public ArtistEntity Artist
    {
      get { return artist; }
      set { artist = value; }
    }
    
    private List<ArtistEntity> topSimilarArtists;

    public List<ArtistEntity> TopSimilarArtists
    {
      get { return topSimilarArtists; }
      set { topSimilarArtists = value; }
    }

    private List<VideoEntity> videos;

    public List<VideoEntity> Videos
    {
      get { return videos; }
      set { videos = value; }
    }

    private ImageEntity image;

    public ImageEntity Image
    {
      get { return image; }
      set { image = value; }
    }

    public ArtistResponse()
    {
      Videos = new List<VideoEntity>();
      Image = new ImageEntity();
      TopSimilarArtists = new List<ArtistEntity>();
      Artist = new ArtistEntity();
    }

  }
}
