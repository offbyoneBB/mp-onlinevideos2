namespace ZDF.BL
{
    using System;

    public class BeitragBase : Teaser
    {
        private decimal? _bewertung;
        private Bild[] _bilderserie;
        private Bilderserieart _bilderserieArt;
        private string _dauer;
        private Sprungmarke[] _sprungmarken;
        private string _streamUrl;

        public decimal? Bewertung
        {
            get
            {
                return this._bewertung;
            }
            set
            {
                this._bewertung = value;
            }
        }

        public Bild[] Bilderserie
        {
            get
            {
                return this._bilderserie;
            }
            set
            {
                this._bilderserie = value;
            }
        }

        public Bilderserieart BilderserieArt
        {
            get
            {
                return this._bilderserieArt;
            }
            set
            {
                this._bilderserieArt = value;
            }
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

        public Sprungmarke[] Sprungmarken
        {
            get
            {
                return this._sprungmarken;
            }
            set
            {
                this._sprungmarken = value;
            }
        }

        public string StreamUrl
        {
            get
            {
                return this._streamUrl;
            }
            set
            {
                this._streamUrl = value;
            }
        }
    }
}

