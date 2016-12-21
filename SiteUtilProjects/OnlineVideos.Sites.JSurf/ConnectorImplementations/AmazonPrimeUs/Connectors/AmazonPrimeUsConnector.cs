using OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors;
using OnlineVideos.Sites.JSurf.Properties;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrimeUs.Connectors
{
    public class AmazonPrimeUsConnector : AmazonPrimeConnector
    {
        public AmazonPrimeUsConnector()
        {
            Resources.ResourceManager = new SingleAssemblyComponentResourceManager(typeof(Resources));
            Resources.Culture = new System.Globalization.CultureInfo("en-US");
        }
    }
}
