using OnlineVideos.Sites.WebAutomation.ConnectorImplementations.AmazonPrime.Connectors;
using OnlineVideos.Sites.WebAutomation.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.AmazonPrimeDe.Connectors
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
