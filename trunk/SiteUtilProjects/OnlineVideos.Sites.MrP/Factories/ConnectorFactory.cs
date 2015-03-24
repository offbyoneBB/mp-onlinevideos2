using OnlineVideos.Sites.WebAutomation.ConnectorImplementations._4OD;
using OnlineVideos.Sites.WebAutomation.ConnectorImplementations._4OD.Connectors;
using OnlineVideos.Sites.WebAutomation.ConnectorImplementations.AmazonPrime.Connectors;
using OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo;
using OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Connectors;
using OnlineVideos.Sites.WebAutomation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebAutomation.Factories
{
    /// <summary>
    /// Static factory pattern
    /// </summary>
    public static class ConnectorFactory
    {
        public static IInformationConnector GetInformationConnector(ConnectorType connectorType, SiteUtilBase siteUtil)
        {     
            if (connectorType == ConnectorType.SkyGo)
                return new SkyGoInformationConnector(siteUtil);
            if (connectorType == ConnectorType._4oD)
                return new _4ODInformationConnector(siteUtil);
            if (connectorType == ConnectorType.AmazonPrime)
                return new AmazonPrimeInformationConnector(siteUtil);
            if (connectorType == ConnectorType.AmazonPrimeDe)
                return new AmazonPrimeInformationConnector(siteUtil);
            return null;
        }
    }
}
