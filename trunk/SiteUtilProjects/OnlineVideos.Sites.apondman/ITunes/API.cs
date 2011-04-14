using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.Pondman.Interfaces;

namespace OnlineVideos.Sites.Pondman.ITunes {
    using Nodes;
    using OnlineVideos.Sites.Pondman.Nodes;

    public static class API {

        /// <summary>
        /// Returns a new configurable session you can use to query the api
        /// </summary>
        /// <returns></returns>
        public static ISession GetSession()
        {
            ISession session = new Session();
            session.Config = new Configuration();

            return session;
        }

        /// <summary>
        /// Start browsing the ITunes Movie Trailers content
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static Section Browse(ISession session)
        {
            Section root = Section.Root(session);
            if (root.State == NodeState.Initial)
            {
                root.Update();
            }

            return root;
        }

        /// <summary>
        /// Search request
        /// </summary>
        /// <param name="session"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static List<Movie> Search(ISession session, string query)
        {
            string searchUri = session.Config.SearchUri + HttpUtility.UrlEncode(query);
            string data = session.MakeRequest(searchUri);
            JObject parsedResults = JObject.Parse(data);
            string results = parsedResults["results"].ToString();
            List<Movie> movies = Movie.GetMoviesFromJsonData(session, results);
            return movies;
        }

        public static void Update(ISession session, IExternalContentNode node)
        {
            if (node.Session == null)
            {
                node.Session = session;
            }

            node.Update();
        }

    }

}
