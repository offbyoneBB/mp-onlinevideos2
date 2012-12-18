using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.apondman.IMDb.DTO
{
    public class Overview
    {
        public string user_rating { get; set; }
        public List<string> stars { get; set; }
        public string runtime { get; set; }
        public List<string> genres { get; set; }
        public string certificate { get; set; }
        public List<string> directors { get; set; }
        public string plot { get; set; }
    }

    public class Availability
    {
    }

    public class Duration
    {
        public string seconds { get; set; }
        public string @string { get; set; }
    }

    public class Video
    {
        public string slateUrl { get; set; }
        public string videoId { get; set; }
        public string title { get; set; }
        public Duration duration { get; set; }
    }

    public class ListMembership
    {
        public bool watchlist { get; set; }
    }

    public class Poster
    {
        public int maxWidth { get; set; }
        public int maxHeight { get; set; }
        public string url { get; set; }
    }

    public class Display
    {
        public string titleId { get; set; }
        public string release_date { get; set; }
        public string text { get; set; }
        public string type { get; set; }
        public string year { get; set; }
        public Poster poster { get; set; }
    }

    public class Item
    {
        public Overview overview { get; set; }
        public Availability availability { get; set; }
        public Video video { get; set; }
        public ListMembership listMembership { get; set; }
        public Display display { get; set; }
    }

    public class Model
    {
        public string next { get; set; }
        public string header { get; set; }
        public List<Item> items { get; set; }
    }

    public class IMDbResponse
    {
        public int status { get; set; }
        public Model model { get; set; }
    }
}
