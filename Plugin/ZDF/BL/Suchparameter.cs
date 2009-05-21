namespace ZDF.BL
{
    using System;

    public class Suchparameter
    {
        private bool _aufsteigend;
        private string _bisDatum;
        private bool _mitBildergalerie;
        private bool _mitVideo;
        private string _nurKanal;
        private SortOption _sortiertNach;
        private string _suchbegriff;
        private string _vonDatum;

        public bool Aufsteigend
        {
            get
            {
                return this._aufsteigend;
            }
            set
            {
                this._aufsteigend = value;
            }
        }

        public string BisDatum
        {
            get
            {
                return this._bisDatum;
            }
            set
            {
                this._bisDatum = value;
            }
        }

        public bool MitBildergalerie
        {
            get
            {
                return this._mitBildergalerie;
            }
            set
            {
                this._mitBildergalerie = value;
            }
        }

        public bool MitVideo
        {
            get
            {
                return this._mitVideo;
            }
            set
            {
                this._mitVideo = value;
            }
        }

        public string NurKanal
        {
            get
            {
                return this._nurKanal;
            }
            set
            {
                this._nurKanal = value;
            }
        }

        public SortOption SortiertNach
        {
            get
            {
                return this._sortiertNach;
            }
            set
            {
                this._sortiertNach = value;
            }
        }

        public string Suchbegriff
        {
            get
            {
                return this._suchbegriff;
            }
            set
            {
                this._suchbegriff = value;
            }
        }

        public string VonDatum
        {
            get
            {
                return this._vonDatum;
            }
            set
            {
                this._vonDatum = value;
            }
        }
    }
}

