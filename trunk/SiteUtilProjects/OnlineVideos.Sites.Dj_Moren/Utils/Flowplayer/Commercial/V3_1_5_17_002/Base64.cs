using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flowplayer.Commercial.V3_1_5_17_002
{
    internal class Base64
    {
        public static String Decode(String content, Boolean force)
        {
            StringBuilder builder = new StringBuilder();
            content = force ? Utf8.Decode(content) : content;
            int i = 0;
            List<String> strings = new List<String>(content.Length);

            while (i < content.Length)
            {
                uint a = (uint)Base64.Code.IndexOf(content[i]);
                uint b = (uint)Base64.Code.IndexOf(content[i + 1]);
                uint c = (uint)Base64.Code.IndexOf(content[i + 2]);
                uint d = (uint)Base64.Code.IndexOf(content[i + 3]);

                uint e = ((a << 18) | (b << 12) | (c << 6) | d);

                uint f = ((e >> 16) & 0xFF);
                uint g = ((e >> 8) & 0xFF);
                uint h = (e & 0xFF);

                strings.Add(new String(new Char[] { (Char)f, (Char)g, (Char)h }));

                if (d == 64)
                {
                    strings[i / 4] = new String(new Char[] { (Char)f, (Char)g });
                }

                if (c == 64)
                {
                    strings[i / 4] = new String(new Char[] { (Char)f });
                }

                i += 4;
            }

            foreach (var str in strings)
            {
                builder.Append(str);
            }

            return (force) ? Utf8.Decode(builder.ToString()) : builder.ToString();
        }

        public static String Encode(String content, Boolean force)
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder temp = new StringBuilder();
            StringBuilder temp2 = new StringBuilder();
            content = (force) ? Utf8.Encode(content) : content;
            
            int i = content.Length % 3;

            if (i > 0)
            {
                while (i++ < 3)
                {
                    content += "0";
                    temp.Append("=");
                }
            }
            i = 0;
            while (i < content.Length)
            {
                uint a = content[i];
                uint b = content[i + 1];
                uint c = content[i + 2];

                uint d = ((a << 16) | (b << 8) | c);

                uint e = (d >> 18) & 0x3F;
                uint f = (d >> 12) & 0x3F;
                uint g = (d >> 6) & 0x3F;
                uint h = d & 0x3F;

                temp2.Append(new String(new Char[] { Base64.Code[(int)e], Base64.Code[(int)f], Base64.Code[(int)g], Base64.Code[(int)h] }));

                i += 3;
            }

            builder.Append(temp2.ToString().Substring(0, temp2.Length - temp.Length));
            builder.Append(temp);

            return builder.ToString();
        }

        public const String Code = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
    }
}
