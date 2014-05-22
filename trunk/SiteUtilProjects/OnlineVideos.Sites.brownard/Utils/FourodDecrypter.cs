using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace OnlineVideos.Sites
{
    static class FourodDecrypter
    {
        const string KEY = "wHcnqpHNN"; // "STINGMIMI";

        public static string Decode4odToken(string token)
        {
            byte[] encryptedBytes = Convert.FromBase64String(token);
            BlowfishEngine bf = new BlowfishEngine();
            bf.Init(false, new KeyParameter(Encoding.ASCII.GetBytes(KEY)));
            byte[] decryptedBytes = decrypt(encryptedBytes, bf);
            return Encoding.ASCII.GetString(decryptedBytes);
        }

        static byte[] decrypt(byte[] byteArray, BlowfishEngine bf)
        {
            int blockSize = 8;
            List<byte> decrypted = new List<byte>();
            for (int i = 0; i < byteArray.Length; i = i + blockSize)
            {
                byte[] blockBytes = new byte[blockSize];
                byte[] outBytes = new byte[blockSize];
                for (int j = 0; j < blockSize; j++)
                {
                    blockBytes[j] = byteArray[i + j];
                }
                bf.ProcessBlock(blockBytes, 0, outBytes, 0);
                decrypted.AddRange(outBytes);
            }
            unpad(decrypted);
            return decrypted.ToArray();
        }

        static void unpad(List<byte> decrypted)
        {
            uint c = decrypted[decrypted.Count - 1];
            for (uint i = c; i > 0; i--)
                decrypted.RemoveAt(decrypted.Count - 1);
        }
    }
}
