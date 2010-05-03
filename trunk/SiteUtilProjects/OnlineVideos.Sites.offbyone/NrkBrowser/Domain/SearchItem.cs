/*
 * Created by: 
 * Created: 9. september 2008
 */

namespace Vattenmelon.Nrk.Domain
{
    public class SearchItem : Item
    {
        private int indexPage;
        private string keyword;


        public SearchItem(string id, string title, int indexPage) : base(id, title)
        {
            this.indexPage = indexPage;
        }

        public int IndexPage
        {
            get { return indexPage; }
            set { this.indexPage = value; }
        }

        public string Keyword
        {
            get { return keyword; }
            set { this.keyword = value; }
        }
    }
}