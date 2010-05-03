/*
 * Created by: 
 * Created: 5. september 2008
 */

using System;

namespace Vattenmelon.Nrk.Domain
{
    public class Clip : Item
    {
        public enum KlippType
        {
            KLIPP = 0,
            VERDI = 1,
            RSS = 2,
            INDEX = 3,
            DIREKTE = 4,
            NRKBETA = 5,
            KLIPP_CHAPTER = 6,
            PODCAST = 7
        }

        private string verdiLink = string.Empty;
        private string antallGangerVist = string.Empty;
        private string klokkeslett = string.Empty;
        private Double startTime;
        private string mediaType = string.Empty;
        private String duration;
        // sier noe om hvilken måte man finner klipplinken
        private KlippType type = KlippType.KLIPP;
        private int tilhoerendeProsjekt;

        public int TilhoerendeProsjekt
        {
            get { return tilhoerendeProsjekt; }
            set { tilhoerendeProsjekt = value; }
        }

        public Clip(string id, string title)
            : base(id, title)
        {
        }

        public override bool Playable
        {
            get { return true; }
        }

        public string AntallGangerVist
        {
            get { return antallGangerVist; }
            set { this.antallGangerVist = value; }
        }

        public string VerdiLink
        {
            get { return verdiLink; }
            set { this.verdiLink = value; }
        }

        public Double StartTime
        {
            get { return startTime; }
            set { this.startTime = value; }
        }

        public string Klokkeslett
        {
            get { return klokkeslett; }
            set { this.klokkeslett = value; }
        }

        public KlippType Type
        {
            get { return type; }
            set { this.type = value; }
        }

        public string MediaType
        {
            get { return mediaType; }
            set { this.mediaType = value; }
        }
        public string Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        public override string ToString()
        {
            return string.Format("{0}|{1}", ID, Type.ToString());
        }

    }
}