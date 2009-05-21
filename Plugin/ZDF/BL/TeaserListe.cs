namespace ZDF.BL
{
    using System;

    public class TeaserListe : TeaserListeBase
    {
        public TeaserListe()
        {
        }

        public TeaserListe(int count)
        {
            base.Teasers = new Teaser[count];
            for (int i = 0; i < count; i++)
            {
                base.Teasers[i] = new Teaser();
            }
            base.Gesamtanzahl = new int?(count);
        }
    }
}

