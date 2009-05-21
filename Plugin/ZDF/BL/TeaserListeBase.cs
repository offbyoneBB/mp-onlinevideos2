namespace ZDF.BL
{
    using System;
    using System.Xml.Serialization;

    public class TeaserListeBase
    {
        private int? _gesamtanzahl;
        private Teaser[] _teasers;

        public int? Gesamtanzahl
        {
            get
            {
                return this._gesamtanzahl;
            }
            set
            {
                this._gesamtanzahl = value;
            }
        }

        [XmlIgnore]
        public int Length
        {
            get
            {
                if (!this.Gesamtanzahl.HasValue)
                {
                    return this._teasers.Length;
                }
                return this.Gesamtanzahl.Value;
            }
        }

        public Teaser[] Teasers
        {
            get
            {
                return this._teasers;
            }
            set
            {
                this._teasers = value;
            }
        }
    }
}

