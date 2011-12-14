using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    class FourodDecrypter
    {
        public FourodDecrypter()
        {
            initTable();
        }

        public string Decode4odToken(string token)
        {
            //get bytes from token
            byte[] encryptedBytes = decodeToByteArray(token);

            //get bytes from decrypt key
            string key = "STINGMIMI";
            byte[] keyBytes = StringToByteArray(key);

            //init decrypter with key bytes
            MyBlowFish bf = new MyBlowFish(keyBytes);

            //decrypt token bytes
            uint[] decryptedBytes = bf.Decrypt(encryptedBytes);

            //convert decrypted bytes back to string
            string s = "";
            foreach (uint c in decryptedBytes)
                s += (char)c;

            return s;
        }

        Dictionary<char, int> table_a2b_base64 = new Dictionary<char, int>();

        void initTable()
        {
            table_a2b_base64.Add('A', 0);
            table_a2b_base64.Add('B', 1);
            table_a2b_base64.Add('C', 2);
            table_a2b_base64.Add('D', 3);
            table_a2b_base64.Add('E', 4);
            table_a2b_base64.Add('F', 5);
            table_a2b_base64.Add('G', 6);
            table_a2b_base64.Add('H', 7);
            table_a2b_base64.Add('I', 8);
            table_a2b_base64.Add('J', 9);
            table_a2b_base64.Add('K', 10);
            table_a2b_base64.Add('L', 11);
            table_a2b_base64.Add('M', 12);
            table_a2b_base64.Add('N', 13);
            table_a2b_base64.Add('O', 14);
            table_a2b_base64.Add('P', 15);
            table_a2b_base64.Add('Q', 16);
            table_a2b_base64.Add('R', 17);
            table_a2b_base64.Add('S', 18);
            table_a2b_base64.Add('T', 19);
            table_a2b_base64.Add('U', 20);
            table_a2b_base64.Add('V', 21);
            table_a2b_base64.Add('W', 22);
            table_a2b_base64.Add('X', 23);
            table_a2b_base64.Add('Y', 24);
            table_a2b_base64.Add('Z', 25);
            table_a2b_base64.Add('a', 26);
            table_a2b_base64.Add('b', 27);
            table_a2b_base64.Add('c', 28);
            table_a2b_base64.Add('d', 29);
            table_a2b_base64.Add('e', 30);
            table_a2b_base64.Add('f', 31);
            table_a2b_base64.Add('g', 32);
            table_a2b_base64.Add('h', 33);
            table_a2b_base64.Add('i', 34);
            table_a2b_base64.Add('j', 35);
            table_a2b_base64.Add('k', 36);
            table_a2b_base64.Add('l', 37);
            table_a2b_base64.Add('m', 38);
            table_a2b_base64.Add('n', 39);
            table_a2b_base64.Add('o', 40);
            table_a2b_base64.Add('p', 41);
            table_a2b_base64.Add('q', 42);
            table_a2b_base64.Add('r', 43);
            table_a2b_base64.Add('s', 44);
            table_a2b_base64.Add('t', 45);
            table_a2b_base64.Add('u', 46);
            table_a2b_base64.Add('v', 47);
            table_a2b_base64.Add('w', 48);
            table_a2b_base64.Add('x', 49);
            table_a2b_base64.Add('y', 50);
            table_a2b_base64.Add('z', 51);
            table_a2b_base64.Add('0', 52);
            table_a2b_base64.Add('1', 53);
            table_a2b_base64.Add('2', 54);
            table_a2b_base64.Add('3', 55);
            table_a2b_base64.Add('4', 56);
            table_a2b_base64.Add('5', 57);
            table_a2b_base64.Add('6', 58);
            table_a2b_base64.Add('7', 59);
            table_a2b_base64.Add('8', 60);
            table_a2b_base64.Add('9', 61);
            table_a2b_base64.Add('+', 62);
            table_a2b_base64.Add('/', 63);
            table_a2b_base64.Add('=', 0);
        }

        //decodes token to bytes and removes any padding
        byte[] decodeToByteArray(string s)
        {
            s = s.Trim();

            int quad_pos = 0;
            int leftbits = 0;
            int leftchar = 0;
            List<byte> res = new List<byte>();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (c > '\x7f' || c == '\n' || c == '\r' || c == ' ')
                    continue;

                if (c == '=')
                {
                    if (quad_pos < 2 || (quad_pos == 2 && next_valid_char(s, i) != '='))
                        continue;
                    else
                    {
                        leftbits = 0;
                        break;
                    }
                }

                int next_c;
                if (table_a2b_base64.ContainsKey(c))
                    next_c = table_a2b_base64[c];
                else
                    continue;

                quad_pos = (quad_pos + 1) & 0x03;
                leftchar = (leftchar << 6) | next_c;
                leftbits += 6;
                if (leftbits >= 8)
                {
                    leftbits -= 8;
                    res.Add((byte)(leftchar >> leftbits & 0xff));
                    leftchar &= ((1 << leftbits) - 1);
                }
            }

            if (leftbits != 0)
                throw new Exception("Incorrect padding");

            return res.ToArray();
        }

        private char next_valid_char(string s, int pos)
        {
            for (int i = pos; i < s.Length; i++)
            {
                char c = s[i];
                if (c < '\x7f')
                {
                    if (table_a2b_base64.ContainsKey(c))
                        return c;
                }
            }

            return '=';
        }

        byte[] StringToByteArray(string s)
        {
            List<byte> b = new List<byte>();
            foreach (char c in s)
            {
                b.Add((byte)c);
            }
            return b.ToArray();
        }
    }
}
