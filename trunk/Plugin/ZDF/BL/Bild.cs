namespace ZDF.BL
{
    using System;

    public class Bild
    {
        private string _dauer;
        private string _quelle;
        private Bildtyp _typ;
        private string _untertitel;
        private string _url;

        public Bild()
        {
        }

        public Bild(string url, Bildtyp typ)
        {
            this.Typ = typ;
            this.Url = url;
        }

        public string Dauer
        {
            get
            {
                return this._dauer;
            }
            set
            {
                this._dauer = value;
            }
        }

        public string Quelle
        {
            get
            {
                return this._quelle;
            }
            set
            {
                this._quelle = value;
            }
        }

        public Bildtyp Typ
        {
            get
            {
                return this._typ;
            }
            set
            {
                this._typ = value;
            }
        }

        public string Untertitel
        {
            get
            {
                return this._untertitel;
            }
            set
            {
                this._untertitel = value;
            }
        }

        public string Url
        {
            get
            {
                return this._url;
            }
            set
            {
                this._url = value;
            }
        }
    }
}

