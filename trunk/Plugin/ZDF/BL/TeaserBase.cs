namespace ZDF.BL
{
    using System;

    public class TeaserBase
    {
        private string _beschreibung;
        private string _datum;
        private string _endzeit;
        private string _ersterBeitragID;
        private string _heimatkennung;
        private string _id;
        private string _laenge;
        private string _startzeit;
        private Bild[] _teaserbilder;
        private bool _tipp;
        private string _titel;
        private bool? _top10Deaktivieren;
        private Beitragstype _typ;

        public string Beschreibung
        {
            get
            {
                return this._beschreibung;
            }
            set
            {
                this._beschreibung = value;
            }
        }

        public string Datum
        {
            get
            {
                return this._datum;
            }
            set
            {
                this._datum = value;
            }
        }

        public string Endzeit
        {
            get
            {
                return this._endzeit;
            }
            set
            {
                this._endzeit = value;
            }
        }

        public string ErsterBeitragID
        {
            get
            {
                return this._ersterBeitragID;
            }
            set
            {
                this._ersterBeitragID = value;
            }
        }

        public string Heimatkennung
        {
            get
            {
                return this._heimatkennung;
            }
            set
            {
                this._heimatkennung = value;
            }
        }

        public string ID
        {
            get
            {
                return this._id;
            }
            set
            {
                this._id = value;
            }
        }

        public string Laenge
        {
            get
            {
                return this._laenge;
            }
            set
            {
                this._laenge = value;
            }
        }

        public string Startzeit
        {
            get
            {
                return this._startzeit;
            }
            set
            {
                this._startzeit = value;
            }
        }

        public Bild[] Teaserbilder
        {
            get
            {
                return this._teaserbilder;
            }
            set
            {
                this._teaserbilder = value;
            }
        }

        public bool Tipp
        {
            get
            {
                return this._tipp;
            }
            set
            {
                this._tipp = value;
            }
        }

        public string Titel
        {
            get
            {
                return this._titel;
            }
            set
            {
                this._titel = value;
            }
        }

        public bool? Top10Deaktivieren
        {
            get
            {
                return this._top10Deaktivieren;
            }
            set
            {
                this._top10Deaktivieren = value;
            }
        }

        public Beitragstype Typ
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
    }
}

