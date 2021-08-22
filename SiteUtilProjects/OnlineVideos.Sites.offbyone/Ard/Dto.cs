using OnlineVideos.Sites.Zdf;

using System;

namespace OnlineVideos.Sites.Ard
{

    public class DownloadDetailsDto : IEquatable<DownloadDetailsDto>
    {
        public DownloadDetailsDto(/*string mimeType, string language, */Qualities quality, string url)
        {
            //MimeType = mimeType;
            //Language = language;
            Quality = quality;
            var uriBuilder = new UriBuilder(new Uri(url, true))
            {
                Scheme = Uri.UriSchemeHttps,
                Port = -1, //default port of scheme
            };
            Url = uriBuilder.ToString();
        }

        //public string MimeType { get; }
        //public string Language { get; }
        public Qualities Quality { get; }
        public string Url { get; }

        public bool Equals(DownloadDetailsDto other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return /*string.Equals(Language, other.Language, StringComparison.OrdinalIgnoreCase)*/
                   //&& string.Equals(MimeType, other.MimeType, StringComparison.OrdinalIgnoreCase)
                   /*&&*/ Quality == other.Quality;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals((DownloadDetailsDto)obj);
        }

        public override int GetHashCode()
        {
            return Quality.GetHashCode();
        }

        public static bool operator ==(DownloadDetailsDto left, DownloadDetailsDto right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DownloadDetailsDto left, DownloadDetailsDto right)
        {
            return !Equals(left, right);
        }
    }


    public abstract class ArdInformationDtoBase
    {
        public string Id { get; }

        protected ArdInformationDtoBase(string id) => Id = id;

        public string Title { get; set; }
        public string Description { get; set; }

        public string TargetUrl { get; set; }
        public string ImageUrl { get; set; }
    }

    public class ArdVideoInfoDto : ArdInformationDtoBase, IEquatable<ArdVideoInfoDto> //extends CrawlerUrlDTO
    {
        //public string TargetUrl { get; }
        //public string Id { get; }
        public int NumberOfClips { get; }


        //public string Title { get; set; }
        //public string Description { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime? AvailableUntilDate { get; set; }
        public int? Duration { get; set; }
        //public string ImageUrl { get; set; }
        public bool IsGeoBlocked { get; set; }
        public bool IsFskBlocked { get; set; }


        public ArdVideoInfoDto(string id, int numberOfClips, string url = null) : base(id)
        {
            //super(url);
            TargetUrl = url ?? ArdConstants.ITEM_URL + id;
            //Id = id;
            NumberOfClips = numberOfClips;
        }

        public bool Equals(ArdVideoInfoDto other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return TargetUrl == other.TargetUrl && Id == other.Id && NumberOfClips == other.NumberOfClips;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ArdVideoInfoDto)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (TargetUrl != null ? TargetUrl.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ NumberOfClips;
                return hashCode;
            }
        }

        public static bool operator ==(ArdVideoInfoDto left, ArdVideoInfoDto right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ArdVideoInfoDto left, ArdVideoInfoDto right)
        {
            return !Equals(left, right);
        }
    }


    public class PublicationService
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string PartnerName { get; set; }

        /*
        "name": "BR Fernsehen",
        "logo": {
          "title": "BR Fernsehen",
          "alt": "BR Logo",
          "producerName": "BR",
          "src": "https://img.ardmediathek.de/standard/00/21/51/89/04/-2114473875/16x9/{width}?mandant=ard",
          "aspectRatio": "16x9"
        },
        "publisherType": "TV",
        "partner": "br",
        "id": "b3JnYW5pemF0aW9uX0JS"
         */
    }


    public class ArdCategoryInfoDto : ArdInformationDtoBase, IEquatable<ArdCategoryInfoDto>
    {
        public ArdCategoryInfoDto(string id, string navigationUrl) : base(id)
        {
            //Url = ArdConstants.ITEM_URL + id;
            TargetUrl = navigationUrl;
            //Id = id;
            HasSubCategories = false;
        }

        public bool HasSubCategories { get; set; }

        public PaginationDto Pagination { get; set; }

        public bool Equals(ArdCategoryInfoDto other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return TargetUrl == other.TargetUrl && Id == other.Id && Title == other.Title;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ArdCategoryInfoDto)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (TargetUrl != null ? TargetUrl.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Title != null ? Title.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ArdCategoryInfoDto left, ArdCategoryInfoDto right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ArdCategoryInfoDto left, ArdCategoryInfoDto right)
        {
            return !Equals(left, right);
        }
    }



    public class PaginationDto
    {
        public PaginationDto(int pageSize, int totalElements, int pageNumber = 0)
        {
            PageSize = pageSize;
            TotalElements = totalElements;
            PageNumber = pageNumber;
        }

        public int PageNumber { get; set; } = 0;
        public int PageSize { get; set; }
        public int TotalElements { get; set; }
    }

}
