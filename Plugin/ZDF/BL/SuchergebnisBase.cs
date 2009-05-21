namespace ZDF.BL
{
    using System;

    public class SuchergebnisBase : TeaserListe
    {
        private Teaser[] _passendeThemen;

        public Teaser[] PassendeThemen
        {
            get
            {
                return this._passendeThemen;
            }
            set
            {
                this._passendeThemen = value;
            }
        }
    }
}

