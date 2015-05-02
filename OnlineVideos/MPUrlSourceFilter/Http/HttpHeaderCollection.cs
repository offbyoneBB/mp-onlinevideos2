using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace OnlineVideos.MPUrlSourceFilter.Http
{
    /// <summary>
    /// Represents class for collection of HTTP headers.
    /// </summary>
    [Serializable]
    public class HttpHeaderCollection : Collection<HttpHeader>
    {
        #region Methods

        protected override void InsertItem(int index, HttpHeader item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, HttpHeader item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            base.SetItem(index, item);
        }

        #endregion
    }
}
