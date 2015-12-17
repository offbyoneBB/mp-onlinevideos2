using OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors;
using OnlineVideos.Sites.JSurf.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrimeDe.Connectors
{
    public class AmazonPrimeDeConnector : AmazonPrimeConnector
    {
        public AmazonPrimeDeConnector()
            : base()
        {
            Properties.Resources.ResourceManager = new SingleAssemblyComponentResourceManager(typeof(Resources));
            Properties.Resources.Culture = new System.Globalization.CultureInfo("de");
        }
    }
}
