namespace ZDF.BL
{
    using System;

    public class CAE_Exception : Exception
    {
        private Fehler _error;

        public CAE_Exception()
        {
        }

        public CAE_Exception(Fehler error)
        {
            this.Error = error;
        }

        public Fehler Error
        {
            get
            {
                return this._error;
            }
            set
            {
                this._error = value;
            }
        }
    }
}

