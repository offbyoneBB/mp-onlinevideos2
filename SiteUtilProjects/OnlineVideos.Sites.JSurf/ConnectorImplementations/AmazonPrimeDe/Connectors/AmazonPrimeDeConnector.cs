using OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors;
using OnlineVideos.Sites.JSurf.Properties;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrimeDe.Connectors
{
    public class AmazonPrimeDeConnector : AmazonPrimeConnector
    {
        public AmazonPrimeDeConnector()
        {
            Resources.ResourceManager = new SingleAssemblyComponentResourceManager(typeof(Resources));
            Resources.Culture = new System.Globalization.CultureInfo("de");
        }
    }
}
