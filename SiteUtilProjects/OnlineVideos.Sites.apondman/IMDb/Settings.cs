using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb
{
    using OnlineVideos.Sites.Pondman.Interfaces;

    public class Settings : ISessionSettings
    {

        /// <summary>
        /// Gets or sets the base URI for the IMDb API.
        /// </summary>
        /// <value>a string representing the base url.</value>
        public string BaseApiUri
        {
            get
            {
                return this.baseApiUri;
            }
            set
            {
                this.baseApiUri = value;
            }
        } private string baseApiUri = "http://app.imdb.com/{0}?locale={1}{2}";

        /// <summary>
        /// GGets or sets the base URI for the IMDb website.
        /// </summary>
        /// <value>a string representing the base url.</value>
        public string BaseUri
        {
            get
            {
                return this.baseUri;
            }
            set
            {
                this.baseUri = value;
            }
        } private string baseUri = "http://www.imdb.com";

        public string BaseUriMobile
        {
            get
            {
                return this.baseUriMobile;
            }
            set
            {
                this.baseUriMobile = value;
            }
        } private string baseUriMobile = "http://m.imdb.com/{0}{1}";

        /// <summary>
        /// Gets or sets the locale used for requests to IMDb.
        /// </summary>
        /// <value>a string representing the IMDb locale parameter</value>
        public string Locale
        {
            get 
            { 
                return this.locale; 
            }
            set 
            { 
                this.locale = value; 
            }
        } private string locale = "en_US";

        public string TitleDetails
        {
            get
            {
                return this.titleDetails;
            }
            set
            {
                this.titleDetails = value;
            }
        } private string titleDetails = "title/maindetails";

        public string BoxOffice
        {
            get
            {
                return this.boxOffice;
            }
            set
            {
                this.boxOffice = value;
            }
        } private string boxOffice = "boxoffice";

        public string FeatureComingSoon
        {
            get
            {
                return this.featureComingSoon;
            }
            set
            {
                this.featureComingSoon = value;
            }
        } private string featureComingSoon = "feature/comingsoon";

        public string ChartTop250
        {
            get
            {
                return this.chartTop250;
            }
            set
            {
                this.chartTop250 = value;
            }
        } private string chartTop250 = "chart/top";

        public string ChartBottom100
        {
            get
            {
                return this.chartBottom100;
            }
            set
            {
                this.chartBottom100 = value;
            }
        } private string chartBottom100 = "chart/bottom";

        public string ChartMovieMeter
        {
            get
            {
                return this.chartMovieMeter;
            }
            set
            {
                this.chartMovieMeter = value;
            }
        } private string chartMovieMeter = "chart/moviemeter";

        public string PopularTVSeries
        {
            get
            {
                return this.popularTVSeries;
            }
            set
            {
                this.popularTVSeries = value;
            }
        } private string popularTVSeries = "chart/tv";  

        public string VideoGallery 
        {
            get    
            { 
                return this.videoGallery; 
            }
            set 
            { 
                this.videoGallery = value; 
            }
        } private string videoGallery = "http://www.imdb.com/title/{0}/videogallery?sort=1";

        public string VideoInfo
        {
            get
            {
                return this.videoInfo;
            }
            set
            {
                this.videoInfo = value;
            }
        } private string videoInfo = "http://www.imdb.com/video/imdb/{0}/";

        public string TrailersTopHD
        {
            get
            {
                return this.trailersTopHD;
            }
            set
            {
                this.trailersTopHD = value;
            }
        } private string trailersTopHD = "http://www.imdb.com/video/trailers/data/_ajax/adapter/shoveler?list=top_hd";

        public string TrailersRecent
        {
            get
            {
                return this.trailersRecent;
            }
            set
            {
                this.trailersRecent = value;
            }
        } private string trailersRecent = "http://www.imdb.com/video/trailers/data/_ajax/adapter/shoveler?list=recent";

        public string TrailersPopular
        {
            get
            {
                return this.trailersPopular;
            }
            set
            {
                this.trailersPopular = value;
            }
        } private string trailersPopular = "http://www.imdb.com/video/trailers/data/_ajax/adapter/shoveler?list=popular";

        public string FullLengthMovies {
            get
            {
                return this.fullLengthMovies;
            }
            set
            {
                this.fullLengthMovies = value;
            }
        } private string fullLengthMovies = "http://www.imdb.com/features/video/browse/?c={0}";

        public string SearchMobile
        {
            get
            {
                return this.searchMobile;
            }
            set
            {
                this.searchMobile = value;
            }
        } private string searchMobile = "find";

        public string TitleDetailsMobile
        {
            get
            {
                return this.titleDetailsMobile;
            }
            set
            {
                this.titleDetailsMobile = value;
            }
        } private string titleDetailsMobile = "title";

        public string ChartTop250Mobile
        {
            get
            {
                return this.chartTop250Mobile;
            }
            set
            {
                this.chartTop250Mobile = value;
            }
        } private string chartTop250Mobile = "chart/top_json";

        public string ChartBottom100Mobile
        {
            get
            {
                return this.chartBottom100Mobile;
            }
            set
            {
                this.chartBottom100Mobile = value;
            }
        } private string chartBottom100Mobile = "chart/bottom_json";

        public string ChartMovieMeterMobile
        {
            get
            {
                return this.chartMovieMeterMobile;
            }
            set
            {
                this.chartMovieMeterMobile = value;
            }
        } private string chartMovieMeterMobile = "chart/moviemeter_json";

        public string PopularTVSeriesMobile
        {
            get
            {
                return this.popularTVSeriesMobile;
            }
            set
            {
                this.popularTVSeriesMobile = value;
            }
        } private string popularTVSeriesMobile = "chart/tv_json"; 

        public string BoxOfficeMobile
        {
            get
            {
                return this.boxOfficeMobile;
            }
            set
            {
                this.boxOfficeMobile = value;
            }
        } private string boxOfficeMobile = "boxoffice_json";

        public string ComingSoon
        {
            get
            {
                return this.comingSoon;
            }
            set
            {
                this.comingSoon = value;
            }
        } private string comingSoon = "nowplaying_json";

        public string BestPictureWinners
        {
            get
            {
                return this.bestPictureWinners;
            }
            set
            {
                this.bestPictureWinners = value;
            }
        } private string bestPictureWinners = "/feature/bestpicture_json";
        
        
    }
    
}
