using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.MediaPortal1
{
    public partial class ImportGlobalSite : Form
    {
        public ImportGlobalSite()
        {
            InitializeComponent();
        }

        private void dgvSites_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string strColumnName = dgvSites.Columns[e.ColumnIndex].DataPropertyName;
            SortOrder strSortOrder = getSortOrder(e.ColumnIndex);
            List<OnlineVideosWebservice.Site> sites = dgvSites.DataSource as List<OnlineVideosWebservice.Site>;
            sites.Sort(new SiteComparer(strColumnName, strSortOrder));
            dgvSites.DataSource = null;
            dgvSites.DataSource = sites;
            dgvSites.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = strSortOrder;
        }

        private SortOrder getSortOrder(int columnIndex)
        {
            if (dgvSites.Columns[columnIndex].HeaderCell.SortGlyphDirection == SortOrder.None ||
                dgvSites.Columns[columnIndex].HeaderCell.SortGlyphDirection == SortOrder.Descending)
            {
                dgvSites.Columns[columnIndex].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                return SortOrder.Ascending;
            }
            else
            {
                dgvSites.Columns[columnIndex].HeaderCell.SortGlyphDirection = SortOrder.Descending;
                return SortOrder.Descending;
            }
        }

        class SiteComparer : IComparer<OnlineVideosWebservice.Site>
        {
            string memberName = string.Empty; // specifies the member name to be sorted
            SortOrder sortOrder = SortOrder.None; // Specifies the SortOrder.
            Type siteType = typeof(OnlineVideosWebservice.Site);
            System.Reflection.PropertyInfo propInfo = null;

            /// <summary>
            /// constructor to set the sort column and sort order.
            /// </summary>
            /// <param name="strMemberName"></param>
            /// <param name="sortingOrder"></param>
            public SiteComparer(string strMemberName, SortOrder sortingOrder)
            {
                memberName = strMemberName;
                propInfo = siteType.GetProperty(memberName);
                sortOrder = sortingOrder;
            }

            public int Compare(OnlineVideosWebservice.Site site1, OnlineVideosWebservice.Site site2)
            {
                if (sortOrder == SortOrder.Ascending)
                    return propInfo.GetValue(site1, null).ToString().CompareTo(propInfo.GetValue(site2, null).ToString());
                else
                    return propInfo.GetValue(site2, null).ToString().CompareTo(propInfo.GetValue(site1, null).ToString());
            }
        }

        private void dgvSites_SelectionChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = dgvSites.SelectedRows.Count > 0;
        }
    }
}
