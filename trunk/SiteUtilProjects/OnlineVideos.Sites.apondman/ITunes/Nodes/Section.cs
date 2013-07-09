using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Web;
using OnlineVideos.Sites.Pondman.Interfaces;
using OnlineVideos.Sites.Pondman.Nodes;

namespace OnlineVideos.Sites.Pondman.ITunes.Nodes {
    
    public class Section : ExternalContentNodeBase {

        public string Name { get; set; }

        public List<Section> Sections {
            get {
                if (_sections == null) {
                    _sections = new List<Section>();
                }
                return _sections;
            }
        } List<Section> _sections;

        public List<Movie> Movies {
            get {
                if (_movies == null) {
                    _movies = new List<Movie>();
                }
                return _movies;
            }
        } List<Movie> _movies;

        public static string RootUri {
            get { return "urn://itunes/root"; }
        }

        public static string FeaturedUri {
            get { return "urn://itunes/featured"; }
        }

        public static string GenresUri {
            get { return "urn://itunes/genres"; }
        }

        public static string StudiosUri {
            get { return "urn://itunes/studios"; }
        }

        internal static Section Root(ISession session) 
        {
            //Section root = session.Get<Section>(Section.RootUri);
            //root.Name = session.Config.RootTitle;

            Section root = session.Get<Section>(Section.FeaturedUri);
            root.Name = "Featured";

            return root;
        }

        /// <summary>
        /// Updates all subsections and/or movies contained in this section
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public override NodeResult Update()
        {
            string sectionUri = this.Uri;

            Configuration config = this.session.Config;

            #region Root

            if (sectionUri == Section.RootUri)
            {
                //Sections.Clear();

                //Section subSection = this.session.Get<Section>(Section.FeaturedUri);
                //subSection.Name = "Featured";
                //this.Sections.Add(subSection);

                this.state = NodeState.Complete;
                return NodeResult.Success;
            }

            #endregion

            #region Featured

            if (sectionUri == Section.FeaturedUri)
            {

                Sections.Clear();

                Section subSection = this.session.Get<Section>(config.FeaturedJustAddedUri);
                subSection.Name = "Just Added";
                Sections.Add(subSection);

                subSection = this.session.Get<Section>(config.FeaturedMostPopularUri);
                subSection.Name = "Most Popular";
                Sections.Add(subSection);
                
                subSection = this.session.Get<Section>(config.FeaturedExclusiveUri);
                subSection.Name = "Exclusive";
                Sections.Add(subSection);
                
                subSection = this.session.Get<Section>(config.FeaturedGenresUri);
                subSection.Name = "Genre";
                Sections.Add(subSection);

                subSection = this.session.Get<Section>(config.FeaturedStudiosUri);
                subSection.Name = "Movie Studio";
                Sections.Add(subSection);

                this.state = NodeState.Complete;
                return NodeResult.Success;
            }

            #endregion

            bool IsFtGenre = sectionUri.StartsWith("urn://itunes/featured/genre/");
            bool IsFtStudio = sectionUri.StartsWith("urn://itunes/featured/studio/");

            string requestUri;
            if (IsFtGenre)
                requestUri = config.FeaturedGenresUri;
            else if (IsFtStudio)
                requestUri = config.FeaturedStudiosUri;
            else
                requestUri = Uri;

            string data = this.session.MakeRequest(requestUri);
            Sections.Clear();
            Movies.Clear();

            List<Movie> movies = new List<Movie>();
            movies = Movie.GetMoviesFromJsonData(session, data);

            #region Featured Genres

            if (Uri == config.FeaturedGenresUri)
            {

                var genres = (
                    from m in movies
                    select m.Genres[0]
                    ).Distinct();

                foreach (string genre in genres)
                {
                    string genreUrn = "urn://itunes/featured/genre/" + genre.ToLower().Replace(" ", "");
                    Section genreSection = this.session.Get<Section>(genreUrn);

                    if (genreSection.State == NodeState.Complete)
                        genreSection.Movies.Clear();

                    genreSection.Name = genre;
                    List<Movie> genresMovies = movies.FindAll(m => m.Genres[0] == genre);
                    genreSection.Movies.AddRange(genresMovies);
                    genreSection.state = NodeState.Complete;
                    Sections.Add(genreSection);
                }
                this.state = NodeState.Complete;
                return NodeResult.Success;
            }

            #endregion

            #region Featured Studios

            else if (this.uri == config.FeaturedStudiosUri)
            {

                var studios = (
                    from m in movies
                    select m.Studio
                    ).Distinct();

                foreach (string studio in studios)
                {
                    string studioUrn = "urn://itunes/featured/studio/" + studio.ToLower().Replace(" ", "");
                    Section studioSection = this.session.Get<Section>(studioUrn);
                    studioSection.Name = studio;
                    List<Movie> studiosMovies = movies.FindAll(m => m.Studio == studio);
                    studioSection.Movies.AddRange(studiosMovies);
                    studioSection.state = NodeState.Complete;
                    Sections.Add(studioSection);
                }

                this.state = NodeState.Complete;
                return NodeResult.Success;
            }
            #endregion

            #region Featured Subitems

            else if (sectionUri.StartsWith("urn://itunes/featured/genre/"))
            {
                movies = movies.FindAll(m => m.Genres[0] == this.Name);
            }
            else if (sectionUri.StartsWith("urn://itunes/featured/studio/"))
            {
                movies = movies.FindAll(m => m.Studio == this.Name);
            }

            #endregion

            Movies.AddRange(movies);
            this.state = NodeState.Complete;

            return NodeResult.Success;
        }

    }
}
