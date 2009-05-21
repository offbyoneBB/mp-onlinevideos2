namespace ZDF.BL
{
    using System;

    public class TrennerSlot
    {
        private Teaser _beitragsteaser;
        private Teaser _neuerKanal;
        private Suchparameter _parameter;
        private Slottyp _typ;

        public TrennerSlot()
        {
            this.Beitragsteaser = new Teaser();
            this.NeuerKanal = new Teaser();
            this.Parameter = new Suchparameter();
        }

        public Teaser Beitragsteaser
        {
            get
            {
                return this._beitragsteaser;
            }
            set
            {
                this._beitragsteaser = value;
            }
        }

        public Teaser NeuerKanal
        {
            get
            {
                return this._neuerKanal;
            }
            set
            {
                this._neuerKanal = value;
            }
        }

        public Suchparameter Parameter
        {
            get
            {
                return this._parameter;
            }
            set
            {
                this._parameter = value;
            }
        }

        public Slottyp Typ
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

