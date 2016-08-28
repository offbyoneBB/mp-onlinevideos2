using OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors;
using OnlineVideos.Sites.JSurf.Interfaces;

namespace OnlineVideos.Sites.JSurf.Factories
{
    /// <summary>
    /// Static factory pattern
    /// </summary>
    public static class ConnectorFactory
    {
        public static IInformationConnector GetInformationConnector(ConnectorType connectorType, SiteUtilBase siteUtil)
        {     
            if (connectorType == ConnectorType.AmazonPrime)
                return new AmazonPrimeInformationConnector(siteUtil);
            if (connectorType == ConnectorType.AmazonPrimeDe)
                return new AmazonPrimeInformationConnector(siteUtil);
            if (connectorType == ConnectorType.AmazonPrimeUs)
                return new AmazonPrimeInformationConnector(siteUtil);
            return null;
        }
    }
}
