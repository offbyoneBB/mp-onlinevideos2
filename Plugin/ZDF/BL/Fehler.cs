namespace ZDF.BL
{
    using System;

    public class Fehler
    {
        private string _beschreibung;
        private string _debugInfo;

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

        public string DebugInfo
        {
            get
            {
                return this._debugInfo;
            }
            set
            {
                this._debugInfo = value;
            }
        }
    }
}

