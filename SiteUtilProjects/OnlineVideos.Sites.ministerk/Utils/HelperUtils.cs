using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.Utils
{

    class HelperUtils
    {
        public static string AaDecode(string encoded)
        {
            string[] delim = new string[] { "(ﾟДﾟ)[ﾟεﾟ]+" };
            Dictionary<string, string> symbol_table = new Dictionary<string, string>()
            {
                {"_", @"(ﾟДﾟ) [ﾟΘﾟ]"},
                {"a", @"(ﾟДﾟ) [ﾟωﾟﾉ]"},
                {"b", @"(ﾟДﾟ) [ﾟΘﾟﾉ]"},
                {"d", @"(ﾟДﾟ) [ﾟｰﾟﾉ]"},
                {"e", @"(ﾟДﾟ) [ﾟДﾟﾉ]"},
                {"f", @"(ﾟДﾟ) [1]"},
                
                {"o", @"(ﾟДﾟ) [""o""]"},
                {"u", @"(oﾟｰﾟo)"},
                
                {"7", @"((ﾟｰﾟ) + (o^_^o))"},
                {"6", @"((o^_^o) +(o^_^o) +(c^_^o))"},
                {"5", @"((ﾟｰﾟ) + (ﾟΘﾟ))"},
                {"4", @"(-~3)"},
                {"3", @"(-~-~1)"},
                {"2", @"(-~1)"},
                {"1", @"(-~0)"},
                {"0", @"((c^_^o)-(c^_^o))"}
            };

            string decoded = "";
            string[] aachars = encoded.Split(delim, StringSplitOptions.None);
            foreach (string tmp in aachars)
            {
                string aachar = tmp;
                foreach (KeyValuePair<string, string> pair in symbol_table)
                    aachar = aachar.Replace(pair.Value, pair.Key);
                aachar = aachar.Replace("+ ", "");
                Regex re = new Regex(@"(?<v>^\d+)");
                Match m = re.Match(aachar);
                if (m.Success)
                    decoded += (char)Convert.ToInt32(m.Groups["v"].Value, 8);
                else
                {
                    re = new Regex(@"^u(?<v>[\da-f]+)");
                    m = re.Match(aachar);
                    if (m.Success)
                        decoded += (char)Convert.ToInt32(m.Groups["v"].Value, 16);
                }
            }
            return decoded;
        }

        public static string GetRandomChars(int amount)
        {
            var random = new Random();
            var sb = new System.Text.StringBuilder(amount);
            for (int i = 0; i < amount; i++) sb.Append(System.Text.Encoding.ASCII.GetString(new byte[] { (byte)random.Next(65, 90) }));
            return sb.ToString();
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
