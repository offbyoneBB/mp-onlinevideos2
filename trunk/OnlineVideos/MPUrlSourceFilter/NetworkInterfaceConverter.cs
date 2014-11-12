using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represents class for network interface converter.
    /// </summary>
    public class NetworkInterfaceConverter : StringConverter
    {
        #region Methods

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<String> nics = new List<String>();
            System.Net.NetworkInformation.NetworkInterface[] networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

            nics.Add(OnlineVideoSettings.NetworkInterfaceSystemDefault);
            foreach (var networkInterface in networkInterfaces)
            {
                nics.Add(networkInterface.Name);
            }

            return new StandardValuesCollection(nics);
        }

        #endregion
    }
}
