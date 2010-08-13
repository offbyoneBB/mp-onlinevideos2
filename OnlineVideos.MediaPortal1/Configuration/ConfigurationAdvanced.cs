using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.MediaPortal1
{
    public partial class ConfigurationAdvanced : Form
    {
        public ConfigurationAdvanced()
        {
            InitializeComponent();

            propertyGrid.BrowsableAttributes = new AttributeCollection(new CategoryAttribute("OnlineVideosConfiguration"));
        }
    }
}
