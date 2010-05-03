/*
 * Created by: 
 * Created: 5. september 2008
 */

namespace Vattenmelon.Nrk.Domain
{
    public abstract class Item
    {
        private string _id;
        private string _title;
        private string _description = "";
        private string _bilde = "";

        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        public string ID
        {
            get { return _id; }
            set { _id = value; }
        }

        public virtual bool Playable
        {
            get { return false; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string Bilde
        {
            get { return _bilde; }
            set { _bilde = value; }
        }

        public override string ToString()
        {
            return "(" + GetType().Name + ") " + Title;
        }

        public Item(string id, string title)
        {
            _id = id;
            _title = title;
        }
    }
}