using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace OnlineVideos.Sites.Pondman.ITunes.DTO
{
    internal class MovieDTO
    {
        public string title;

        [OptionalField]
        public DateTime releasedate;

        [OptionalField]
        public string studio;

        [OptionalField]
        public string poster;

        [OptionalField]
        public string moviesite;

        [OptionalField]
        public string location;

        [OptionalField]
        public string rating;

        [OptionalField]
        public string[] genre;

        [OptionalField]
        public string directors;

        [OptionalField]
        public string[] actors;

        [OptionalField]
        public VideoDTO[] trailers;
    }
}
