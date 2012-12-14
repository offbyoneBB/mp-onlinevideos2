namespace OnlineVideos.Sites.Pondman.IMDb.Model
{
    using System.Collections.Generic;
        
    public class VideoDetails : VideoReference
    {
        public VideoDetails()
            : base()
        {
            Files = new Dictionary<VideoFormat, string>();
        }

        public Dictionary<VideoFormat, string> Files { get; set; }

        #region internal members
        /*
        internal override void FillFrom(IMDbVideo dto)
        {
            base.FillFrom(dto);
            
            if (dto.Encodings != null)
            {
                foreach (IMDbVideoFormat format in dto.Encodings.Values)
                {
                    if (format.Format.Contains("iPhone"))
                    {
                        continue;
                    }

                    VideoFormat f = VideoFormat.SD;
                    switch (format.Format)
                    {
                        case "HD 480p":
                            f = VideoFormat.HD480;
                            break;
                        case "HD 720p":
                            f = VideoFormat.HD720;
                            break;
                    }

                    Files[f] = format.URL;
                }
            }
        }
        */
        #endregion

    }
}
