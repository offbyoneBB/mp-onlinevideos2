using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.MPUrlSourceFilter
{
    public partial class RtspConnectionPreference : UserControl
    {
        public RtspConnectionPreference()
        {
            InitializeComponent();
        }

        private void buttonRtspUp_Click(object sender, EventArgs e)
        {
            if (listBoxRtspConnectionPreference.SelectedIndex > 0)
            {
                String previousItem = (String)listBoxRtspConnectionPreference.Items[listBoxRtspConnectionPreference.SelectedIndex - 1];
                String item = (String)listBoxRtspConnectionPreference.Items[listBoxRtspConnectionPreference.SelectedIndex];

                listBoxRtspConnectionPreference.Items[listBoxRtspConnectionPreference.SelectedIndex - 1] = item;
                listBoxRtspConnectionPreference.Items[listBoxRtspConnectionPreference.SelectedIndex] = previousItem;

                listBoxRtspConnectionPreference.SelectedIndex = listBoxRtspConnectionPreference.SelectedIndex - 1;
            }
        }

        private void buttonRtspDown_Click(object sender, EventArgs e)
        {
            if ((listBoxRtspConnectionPreference.SelectedIndex != (-1)) && (listBoxRtspConnectionPreference.SelectedIndex < (listBoxRtspConnectionPreference.Items.Count - 1)))
            {
                String nextItem = (String)listBoxRtspConnectionPreference.Items[listBoxRtspConnectionPreference.SelectedIndex + 1];
                String item = (String)listBoxRtspConnectionPreference.Items[listBoxRtspConnectionPreference.SelectedIndex];

                listBoxRtspConnectionPreference.Items[listBoxRtspConnectionPreference.SelectedIndex + 1] = item;
                listBoxRtspConnectionPreference.Items[listBoxRtspConnectionPreference.SelectedIndex] = nextItem;

                listBoxRtspConnectionPreference.SelectedIndex = listBoxRtspConnectionPreference.SelectedIndex + 1;
            }
        }
    }
}
