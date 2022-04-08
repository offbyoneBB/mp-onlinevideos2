using System;
using System.Collections.Generic;
using System.Linq;


namespace OnlineVideos.Sites.Ard
{ 
    /*
     *  title="Alle"
     *  href="/ard/">
     * 
     *  title="Das Erste"
     *  href="/daserste/">
     * 
     *  title="BR"
     *  href="/br/">
     * 
     *  title="HR"
     *  href="/hr/">
     * 
     *  title="MDR"
     *  href="/mdr/">
     * 
     *  title="NDR"
     *  href="/ndr/">
     * 
     *  title="RadioBremen"
     *  href="/radiobremen/">
     * 
     *  title="RBB"
     *  href="/rbb/">
     * 
     *  title="SR"
     *  href="/sr/">
     * 
     *  title="SWR"
     *  href="/swr/">
     * 
     *  title="WDR"
     *  href="/wdr/">
     * 
     *  title="ONE"
     *  href="/one/">
     * 
     *  title="ARD-alpha"
     *  href="/alpha/">
     * 
     *  title="tagesschau24"
     *  href="/tagesschau24/">
     * 
     *  title="phoenix"
     *  href="/phoenix/">
     */
    public class ArdPartner : IEquatable<ArdPartner>
    {
        public static readonly ArdPartner Empty = new ArdPartner(string.Empty);

        //public static readonly ArdPartner ALL          = new ArdPartner("ard", "Alle"); // special Partner for all other partners
        public static readonly ArdPartner DasErste     = new ArdPartner("daserste", "Das Erste");
        public static readonly ArdPartner Br           = new ArdPartner("BR");
        public static readonly ArdPartner Hr           = new ArdPartner("HR");
        public static readonly ArdPartner Mdr          = new ArdPartner("MDR");
        public static readonly ArdPartner Ndr          = new ArdPartner("NDR");
        public static readonly ArdPartner RadioBremen  = new ArdPartner("RadioBremen");
        public static readonly ArdPartner Rbb          = new ArdPartner("RBB");
        public static readonly ArdPartner Sr           = new ArdPartner("SR");
        public static readonly ArdPartner Swr          = new ArdPartner("SWR");
        public static readonly ArdPartner Wdr          = new ArdPartner("WDR");
        public static readonly ArdPartner One          = new ArdPartner("ONE");
        public static readonly ArdPartner Alpha        = new ArdPartner("alpha", "ARD-alpha");
        public static readonly ArdPartner Tagesschau24 = new ArdPartner("tagesschau24");
        public static readonly ArdPartner Funk         = new ArdPartner("FUNK");
        public static readonly ArdPartner Phoenix      = new ArdPartner("phoenix");

        public static IEnumerable<ArdPartner> Values
        {
            get
            {
                //yield return ALL;
                yield return DasErste;
                yield return Wdr;
                yield return Br;
                yield return Hr;
                yield return Mdr;
                yield return Ndr;
                yield return RadioBremen;
                yield return Rbb;
                yield return Sr;
                yield return Swr;
                yield return One;
                yield return Alpha;
                yield return Tagesschau24;
                yield return Funk;
                yield return Phoenix;
            }
        }

        private static readonly IEqualityComparer<string> _comparer = StringComparer.OrdinalIgnoreCase;

        private static readonly Dictionary<string, ArdPartner> _lookup = Values.ToDictionary(partner => partner, _comparer);

        public string Name { get; }
        public string DisplayName { get; }

        private ArdPartner(string displayName) : this(displayName.ToLower(), displayName)
        { }

        private ArdPartner(string name, string displayName)
        {
            Name = name;
            DisplayName = displayName;
        }


        /// <inheritdoc />
        public bool Equals(ArdPartner other)
        {
            if (other is null) return false;
            return ReferenceEquals(this, other) || _comparer.Equals(Name, other.Name);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ArdPartner partner && Equals(partner);
        }

        /// <inheritdoc />
        public override int GetHashCode() => Name != null ? _comparer.GetHashCode(Name) : 0;

        public static bool operator ==(ArdPartner left, ArdPartner right) => Equals(left, right);

        public static bool operator !=(ArdPartner left, ArdPartner right) => !Equals(left, right);


        public static implicit operator string(ArdPartner partner) => partner.Name;
        public static explicit operator ArdPartner(string partnerName) => _lookup.GetValueOrDefault(partnerName, Empty);
    }
}
