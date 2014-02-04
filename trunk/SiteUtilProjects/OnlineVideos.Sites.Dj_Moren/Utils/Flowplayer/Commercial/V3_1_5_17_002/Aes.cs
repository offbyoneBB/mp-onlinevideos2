using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flowplayer.Commercial.V3_1_5_17_002
{
    public class Aes
    {
        public enum KeyType
        {
            Key128 = 128,
            Key192 = 192,
            Key256 = 256
        }

        internal static int[] RotWord(int[] word)
        {
            // rotate 4-byte word w left by one byte
            int temp = word[0];
            for (int i = 0; i < 3; i++)
            {
                word[i] = word[i + 1];
            }
            word[3] = temp;

            return word;
        }

        internal static int[] SubWord(int[] word)
        {
            // apply SBox to 4-byte word word
            for (int i = 0; i < 4; i++)
            {
                word[i] = SBOX[word[i]];
            }

            return word;
        }

        internal static int[,] KeyExpansion(int[] key)
        {
            // generate Key Schedule (byte-array Nr+1 x Nb) from Key
            int blockSize = 4;                          // block size (in words): no of columns in state (fixed at 4 for AES)
            int keyLengthInWords = key.Length / 4;      // key length (in words): 4/6/8 for 128/192/256-bit keys
            int roundsNumber = keyLengthInWords + 6;    // no of rounds: 10/12/14 for 128/192/256-bit keys

            int[,] w = new int[blockSize * (roundsNumber + 1), 4];

            for (int i = 0; i < keyLengthInWords; i++)
            {
                w[i, 0] = key[4 * i];
                w[i, 1] = key[4 * i + 1];
                w[i, 2] = key[4 * i + 2];
                w[i, 3] = key[4 * i + 3];
            }

            for (int i = keyLengthInWords; i < (blockSize * (roundsNumber + 1)); i++)
            {
                w[i, 0] = 0;
                w[i, 1] = 0;
                w[i, 2] = 0;
                w[i, 3] = 0;

                int[] temp = new int[4] { 0, 0, 0, 0 };
                for (int j = 0; j < 4; j++)
                {
                    temp[j] = w[i - 1, j];
                }

                if ((i % keyLengthInWords) == 0)
                {
                    temp = SubWord(RotWord(temp));
                    for (int j = 0; j < 4; j++)
                    {
                        temp[j] = temp[j] ^ RCON[i / keyLengthInWords, j];
                    }
                }
                else if ((keyLengthInWords > 6) && ((i % keyLengthInWords) == 4))
                {
                    temp = SubWord(temp);
                }

                for (int j = 0; j < 4; j++)
                {
                    w[i, j] = w[i - keyLengthInWords, j] ^ temp[j];
                }
            }

            return w;
        }

        internal static int[,] MixColumns(int[,] s)
        {
            // combine bytes of each column of state S
            for (int i = 0; i < 4; i++)
            {
                int[] a = new int[4];           // 'a' is a copy of the current column from 's'
                int[] b = new int[4];           // 'b' is a•{02} in GF(2^8)
                for (int j = 0; j < 4; j++)
                {
                    a[j] = s[j, i];
                    b[j] = ((s[j, i] & 0x80) != 0) ? ((s[j, i] << 1) ^ 0x11B) : (s[j, i] << 1);
                }

                // a[n] ^ b[n] is a•{03} in GF(2^8)
                s[0, i] = b[0] ^ a[1] ^ b[1] ^ a[2] ^ a[3]; // 2*a0 + 3*a1 + a2 + a3
                s[1, i] = a[0] ^ b[1] ^ a[2] ^ b[2] ^ a[3]; // a0 * 2*a1 + 3*a2 + a3
                s[2, i] = a[0] ^ a[1] ^ b[2] ^ a[3] ^ b[3]; // a0 + a1 + 2*a2 + 3*a3
                s[3, i] = a[0] ^ b[0] ^ a[1] ^ a[2] ^ b[3]; // 3*a0 + a1 + a2 + 2*a3
            }

            return s;
        }

        internal static int[,] AddRoundKey(int[,] state, int[,] w, int rnd, int nb)
        {
            // xor Round Key into state S
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < nb; j++)
                {
                    state[i, j] = state[i, j] ^ w[rnd * 4 + j, i];
                }
            }

            return state;
        }

        internal static int[,] SubBytes(int[,] s, int nb)
        {
            // apply SBox to state S
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < nb; j++)
                {
                    s[i, j] = SBOX[s[i, j]];
                }
            }
            return s;
        }

        internal static int[,] ShiftRows(int[,] s, int nb)
        {
            // shift row r of state S left by r bytes
            for (int i = 1; i < 4; i++)
            {
                int[] temp = new int[4];
                // shift into temp copy
                for (int j = 0; j < 4; j++)
                {
                    temp[j] = s[i, (j + i) % nb];
                }
                // and copy back
                for (int j = 0; j < 4; j++)
                {
                    s[i, j] = temp[j];
                }
                // note that this will work for Nb=4,5,6, but not 7,8 (always 4 for AES)
            }

            return s;
        }

        internal static int[] Cipher(int[] input, int[,] w)
        {
            // main cipher function
            int blockSize = 4;                              // block size (in words): no of columns in state (fixed at 4 for AES)
            //int roundsNumber = w.Length / blockSize - 1;    // no of rounds: 10/12/14 for 128/192/256-bit keys
            int roundsNumber = (w.GetUpperBound(0) + 1) / blockSize - 1;    // no of rounds: 10/12/14 for 128/192/256-bit keys

            int[,] state = new int[4, 4 * blockSize];
            // initialise 4 * blockSize byte-array 'state' with input
            for (int i = 0; i < (4 * blockSize); i++)
            {
                state[i % 4, i / 4] = input[i];
            }
            state = AddRoundKey(state, w, 0, blockSize);

            for (int i = 1; i < roundsNumber; i++)
            {
                state = SubBytes(state, blockSize);
                state = ShiftRows(state, blockSize);
                state = MixColumns(state);
                state = AddRoundKey(state, w, i, blockSize);
            }
            state = SubBytes(state, blockSize);
            state = ShiftRows(state, blockSize);
            state = AddRoundKey(state, w, roundsNumber, blockSize);

            int[] output = new int[4 * blockSize];      // convert state to 1-d array before returning
            for (int i = 0; i < (4 * blockSize); i++)
            {
                output[i] = state[i % 4, i / 4];
            }

            return output;
        }

        public static String Decrypt(String cipherText, String password, KeyType keyType)
        {
            int blockSize = 16;  // block size fixed at 16 bytes / 128 bits (Nb=4) for AES
            int bits = (int)keyType;
            cipherText = Base64.Decode(cipherText, false);
            password = Utf8.Encode(password);

            // use AES to encrypt password (mirroring encrypt routine)
            int bytes = bits / 8;  // no bytes in key
            int[] pwBytes = new int[bytes];
            for (int i = 0; i < bytes; i++)
            {
                if (i + 1 > password.Length)
                {
                    pwBytes[i] = 0;
                }
                else
                {
                    pwBytes[i] = password[i];
                }
            }
            int[] temp = Cipher(pwBytes, KeyExpansion(pwBytes));
            int[] key = new int[temp.Length + bytes - 16];
            // expand key to 16/24/32 bytes long
            for (int i = 0; i < temp.Length; i++)
            {
                key[i] = temp[i];
            }
            for (int i = temp.Length; i < (temp.Length + bytes - 16); i++)
            {
                key[i] = temp[i - temp.Length];
            }

            // recover nonce from 1st 8 bytes of ciphertext
            int[] counterBlock = new int[16];
            String ctrTxt = cipherText.Substring(0, 8);
            for (int i = 0; i < 8; i++)
            {
                counterBlock[i] = ctrTxt[i];
            }

            // generate key schedule
            int[,] keySchedule = KeyExpansion(key);

            // separate ciphertext into blocks (skipping past initial 8 bytes)
            int blocks = (int)Math.Ceiling((double)(cipherText.Length - 8) / (double)blockSize);
            String[] ct = new String[blocks];
            for (int i = 0; i < blocks; i++)
            {
                //ct[i] = cipherText.Substring(8 + i * blockSize, blockSize);
                ct[i] = cipherText.Substring(8 + i * blockSize, Math.Min(cipherText.Length - 8 - i * blockSize, blockSize));
            }
            String[] ciphertextArr = ct;  // ciphertext is now array of block-length strings

            // plaintext will get generated block-by-block into array of block-length strings
            String[] plaintxt = new String[ciphertextArr.Length];

            for (int i = 0; i < blocks; i++)
            {
                // set counter (block #) in last 8 bytes of counter block (leaving nonce in 1st 8 bytes)
                for (int j = 0; j < 4; j++)
                {
                    counterBlock[15 - j] = (i >> (j * 8)) & 0xFF;
                }
                for (int j = 0; j < 4; j++)
                {
                    //counterBlock[15 - j - 4] = (((b + 1) / 0x100000000 - 1) >>> c * 8) & 0xff;
                    counterBlock[15 - j - 4] = 0;
                }

                int[] cipherCntr = Cipher(counterBlock, keySchedule);  // encrypt counter block

                Char[] plaintxtByte = new Char[ciphertextArr[i].Length];
                for (int j = 0; j < ciphertextArr[i].Length; j++)
                {
                    // -- xor plaintxt with ciphered counter byte-by-byte --
                    plaintxtByte[j] = (Char)(cipherCntr[j] ^ ciphertextArr[i][j]);
                    //plaintxtByte[j] = String.fromCharCode(plaintxtByte[j]);
                }
                //plaintxt[i] = plaintxtByte.join('');
                plaintxt[i] = new String(plaintxtByte);
            }

            // join array of blocks into single plaintext string
            //var plaintext : String = plaintxt.join('');
            //plaintext = Utf8.decode(plaintext);  // decode from UTF8 back to Unicode multi-byte chars
            StringBuilder builder = new StringBuilder();
            foreach (var str in plaintxt)
            {
                builder.Append(str);
            }

            return Utf8.Decode(builder.ToString());
        }

        public static String Encrypt(String plainText, String password, KeyType keyType)
        {
            int blockSize = 16; // block size fixed at 16 bytes / 128 bits (Nb=4) for AES
            int bits = (int)keyType;

            plainText = Utf8.Encode(plainText);
            password = Utf8.Encode(password);

            // use AES itself to encrypt password to get cipher key (using plain password as source for key
            // expansion) - gives us well encrypted key

            int bytes = bits / 8;   // no bytes in key
            int[] pwBytes = new int[bytes];
            for (int i = 0; i < bytes; i++)
            {
                if (i + 1 > password.Length)
                {
                    pwBytes[i] = 0;
                }
                else
                {
                    pwBytes[i] = password[i];
                }
            }

            int[] temp = Cipher(pwBytes, KeyExpansion(pwBytes));    // gives us 16-byte key
            int[] key = new int[temp.Length + bytes - 16];          // expand key to 16/24/32 bytes long
            // expand key to 16/24/32 bytes long
            for (int i = 0; i < temp.Length; i++)
            {
                key[i] = temp[i];
            }
            for (int i = temp.Length; i < (temp.Length + bytes - 16); i++)
            {
                key[i] = temp[i - temp.Length];
            }

            // initialise counter block (NIST SP800-38A §B.2): millisecond time-stamp for nonce in 1st 8 bytes,
            // block counter in 2nd 8 bytes
            int[] counterBlock = new int[16];
            float nonce = (int)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;    // timestamp: milliseconds since 1-Jan-1970
            int nonceSec = (int)Math.Floor((double)nonce / (double)1000);
            int nonceMs = (int)(nonce % 1000);

            // encode nonce with seconds in 1st 4 bytes, and (repeated) ms part filling 2nd 4 bytes
            for (int i = 0; i < 4; i++)
            {
                counterBlock[i] = (nonceSec >> i * 8) & 0xFF;
            }
            for (int i = 0; i < 4; i++)
            {
                counterBlock[i + 4] = nonceMs & 0xFF;
            }

            // and convert it to a string to go on the front of the ciphertext
            String ctrTxt = String.Empty;
            for (int i = 0; i < 8; i++)
            {
                ctrTxt += (Char)counterBlock[i];
            }

            // generate key schedule - an expansion of the key into distinct Key Rounds for each round
            int[,] keySchedule = KeyExpansion(key);
            int blockCount = (int)Math.Ceiling((double)plainText.Length / (double)blockSize);
            String[] cipherTxt = new String[blockCount];    // ciphertext as array of strings


            for (int i = 0; i < blockCount; i++)
            {
                // set counter (block #) in last 8 bytes of counter block (leaving nonce in 1st 8 bytes)
                // done in two stages for 32-bit ops: using two words allows us to go past 2^32 blocks (68GB)

                for (int j = 0; j < 4; j++)
                {
                    counterBlock[15 - j] = (i >> j * 8) & 0xFF;
                }

                for (int j = 0; j < 4; j++)
                {
                    //counterBlock[15 - c - 4] = (b / 0x100000000 >>> c * 8);
                    counterBlock[15 - j - 4] = 0;
                }

                int[] cipherCntr = Cipher(counterBlock, keySchedule);   // -- encrypt counter block --
                // block size is reduced on final block
                int blockLength = i < (blockCount - 1) ? blockSize : (plainText.Length - i * blockSize);

                Char[] cipherChar = new Char[blockLength];
                for (int j = 0; j < blockLength; j++)
                {
                    // -- xor plaintext with ciphered counter char-by-char --
                    cipherChar[j] = (Char)(cipherCntr[j] ^ plainText[i * blockSize + j]);
                }

                // ciphertxt[b] = cipherChar.join('');
                cipherTxt[i] = new String(cipherChar);
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(ctrTxt);
            foreach (var str in cipherTxt)
            {
                builder.Append(str);
            }

            return Base64.Encode(builder.ToString(), false);
        }
        
        public const String Key = "xo85kT+QHz3fRMcHMXp9cA";

        // Sbox is pre-computed multiplicative inverse in GF(2^8) used in subBytes and keyExpansion [§5.1.1]
        private static int[] SBOX = new int[] {
            0x63, 0x7C, 0x77, 0x7B, 0xF2, 0x6B, 0x6F, 0xC5, 0x30, 0x01, 0x67, 0x2B, 0xFE, 0xD7, 0xAB, 0x76,
            0xCA, 0x82, 0xC9, 0x7D, 0xFA, 0x59, 0x47, 0xF0, 0xAD, 0xD4, 0xA2, 0xAF, 0x9C, 0xA4, 0x72, 0xC0,
            0xB7, 0xFD, 0x93, 0x26, 0x36, 0x3F, 0xF7, 0xCC, 0x34, 0xA5, 0xE5, 0xF1, 0x71, 0xD8, 0x31, 0x15,
            0x04, 0xC7, 0x23, 0xC3, 0x18, 0x96, 0x05, 0x9A, 0x07, 0x12, 0x80, 0xE2, 0xEB, 0x27, 0xB2, 0x75,
            0x09, 0x83, 0x2C, 0x1A, 0x1B, 0x6E, 0x5A, 0xA0, 0x52, 0x3B, 0xD6, 0xB3, 0x29, 0xE3, 0x2F, 0x84,
            0x53, 0xD1, 0x00, 0xED, 0x20, 0xFC, 0xB1, 0x5B, 0x6A, 0xCB, 0xBE, 0x39, 0x4A, 0x4C, 0x58, 0xCF,
            0xD0, 0xEF, 0xAA, 0xFB, 0x43, 0x4D, 0x33, 0x85, 0x45, 0xF9, 0x02, 0x7F, 0x50, 0x3C, 0x9F, 0xA8,
            0x51, 0xA3, 0x40, 0x8F, 0x92, 0x9D, 0x38, 0xF5, 0xBC, 0xB6, 0xDA, 0x21, 0x10, 0xFF, 0xF3, 0xD2,
            0xCD, 0x0C, 0x13, 0xEC, 0x5F, 0x97, 0x44, 0x17, 0xC4, 0xA7, 0x7E, 0x3D, 0x64, 0x5D, 0x19, 0x73,
            0x60, 0x81, 0x4F, 0xDC, 0x22, 0x2A, 0x90, 0x88, 0x46, 0xEE, 0xB8, 0x14, 0xDE, 0x5E, 0x0B, 0xDB,
            0xE0, 0x32, 0x3A, 0x0A, 0x49, 0x06, 0x24, 0x5C, 0xC2, 0xD3, 0xAC, 0x62, 0x91, 0x95, 0xE4, 0x79,
            0xE7, 0xC8, 0x37, 0x6D, 0x8D, 0xD5, 0x4E, 0xA9, 0x6C, 0x56, 0xF4, 0xEA, 0x65, 0x7A, 0xAE, 0x08,
            0xBA, 0x78, 0x25, 0x2E, 0x1C, 0xA6, 0xB4, 0xC6, 0xE8, 0xDD, 0x74, 0x1F, 0x4B, 0xBD, 0x8B, 0x8A,
            0x70, 0x3E, 0xB5, 0x66, 0x48, 0x03, 0xF6, 0x0E, 0x61, 0x35, 0x57, 0xB9, 0x86, 0xC1, 0x1D, 0x9E,
            0xE1, 0xF8, 0x98, 0x11, 0x69, 0xD9, 0x8E, 0x94, 0x9B, 0x1E, 0x87, 0xE9, 0xCE, 0x55, 0x28, 0xDF,
            0x8C, 0xA1, 0x89, 0x0D, 0xBF, 0xE6, 0x42, 0x68, 0x41, 0x99, 0x2D, 0x0F, 0xB0, 0x54, 0xBB, 0x16
        };

        // Rcon is Round Constant used for the Key Expansion [1st col is 2^(r-1) in GF(2^8)] [§5.2]
        private static int[,] RCON = new int[11, 4] {
            {0x00, 0x00, 0x00, 0x00},
            {0x01, 0x00, 0x00, 0x00},
            {0x02, 0x00, 0x00, 0x00},
            {0x04, 0x00, 0x00, 0x00},
            {0x08, 0x00, 0x00, 0x00},
            {0x10, 0x00, 0x00, 0x00},
            {0x20, 0x00, 0x00, 0x00},
            {0x40, 0x00, 0x00, 0x00},
            {0x80, 0x00, 0x00, 0x00},
            {0x1b, 0x00, 0x00, 0x00},
            {0x36, 0x00, 0x00, 0x00}
        };

    }
}