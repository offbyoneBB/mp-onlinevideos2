namespace ZDF.BL
{
    using System;

    public class Sprungmarke
    {
        private string _position;
        private string _titel;

        public string Position
        {
            get
            {
                return this._position;
            }
            set
            {
                this._position = value;
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
    }
}

