using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flowplayer.Commercial.V3_1_5_17_002
{
    internal class Utf8
    {
        public static String Decode(String content)
        {
            StringBuilder builder = new StringBuilder();
            int i = 0;

            while (i < content.Length)
            {
                uint characterCode = content[i];

                if (characterCode < 128)
                {
                    builder.Append((Char)characterCode);
                    ++i;
                }
                else if ((characterCode > 191) && (characterCode < 224))
                {
                    uint characterCode2 = content[i + 1];
                    builder.Append((Char)(((characterCode & 0x1F) << 6) | (characterCode2 & 0x3F)));
                    i += 2;
                }
                else
                {
                    uint characterCode2 = content[i + 1];
                    uint characterCode3 = content[i + 2];
                    builder.Append((Char)(((characterCode & 0x0F) << 12) | ((characterCode2 & 0x3F) << 6) | (characterCode3 & 0x3F)));
                    i += 3;
                }
            }

            return builder.ToString();
        }

        public static String Encode(String content)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < content.Length; i++)
            {
                uint characterCode = content[i];

                if (characterCode < 128)
                {
                    builder.Append((Char)characterCode);
                }
                else if ((characterCode > 127) && (characterCode < 2048))
                {
                    builder.Append((Char)((characterCode >> 6) | 0xC0));
                    builder.Append((Char)((characterCode & 0x3F) | 0x80));
                }
                else
                {
                    builder.Append((Char)((characterCode >> 12) | 0xE0));
                    builder.Append((Char)(((characterCode >> 6) & 0x3F) | 0x80));
                    builder.Append((Char)((characterCode & 0x3F) | 0x80));
                }
            }

            return builder.ToString();
        }
    }
}
