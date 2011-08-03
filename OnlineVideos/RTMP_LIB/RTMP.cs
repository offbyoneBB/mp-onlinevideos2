using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;

namespace RTMP_LIB
{
    public enum Protocol : short
    {
        UNDEFINED = -1,
        RTMP = 0,
        RTMPT = 1,
        RTMPS = 2,
        RTMPE = 3,
        RTMPTE = 4,
        RTMFP = 5
    };

    public enum PacketType : byte
    {
        Undefined = 0x00,
        ChunkSize = 0x01,
        Abort = 0x02,
        BytesRead = 0x03,
        Control = 0x04,
        ServerBW = 0x05,
        ClientBW = 0x06,
        Audio = 0x08,
        Video = 0x09,
        Metadata_AMF3 = 0x0F,
        SharedObject_AMF3 = 0x10,
        Invoke_AMF3 = 0x11,
        Metadata = 0x12,
        SharedObject = 0x13,
        Invoke = 0x14,
        FlvTags = 0x16
    };

    public enum HeaderType : byte
    {
        Large = 0,
        Medium = 1,
        Small = 2,
        Minimum = 3
    };

    public class RTMP
    {
        #region  Constants

        static readonly byte[] DH_MODULUS_BYTES = new byte[128] 
        { 
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC9, 0x0F, 0xDA, 0xA2, 0x21, 0x68, 0xC2, 0x34, 0xC4, 0xC6, 0x62, 0x8B, 0x80, 0xDC, 0x1C, 0xD1, 0x29, 0x02, 0x4E, 0x08, 0x8A, 0x67, 0xCC, 0x74,
            0x02, 0x0B, 0xBE, 0xA6, 0x3B, 0x13, 0x9B, 0x22, 0x51, 0x4A, 0x08, 0x79, 0x8E, 0x34, 0x04, 0xDD, 0xEF, 0x95, 0x19, 0xB3, 0xCD, 0x3A, 0x43, 0x1B, 0x30, 0x2B, 0x0A, 0x6D, 0xF2, 0x5F, 0x14, 0x37, 
            0x4F, 0xE1, 0x35, 0x6D, 0x6D, 0x51, 0xC2, 0x45, 0xE4, 0x85, 0xB5, 0x76, 0x62, 0x5E, 0x7E, 0xC6, 0xF4, 0x4C, 0x42, 0xE9, 0xA6, 0x37, 0xED, 0x6B, 0x0B, 0xFF, 0x5C, 0xB6, 0xF4, 0x06, 0xB7, 0xED, 
            0xEE, 0x38, 0x6B, 0xFB, 0x5A, 0x89, 0x9F, 0xA5, 0xAE, 0x9F, 0x24, 0x11, 0x7C, 0x4B, 0x1F, 0xE6, 0x49, 0x28, 0x66, 0x51, 0xEC, 0xE6, 0x53, 0x81, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };

        static readonly byte[] GenuineFPKey = new byte[62] 
        {
            0x47,0x65,0x6E,0x75,0x69,0x6E,0x65,0x20,0x41,0x64,0x6F,0x62,0x65,0x20,0x46,0x6C,
            0x61,0x73,0x68,0x20,0x50,0x6C,0x61,0x79,0x65,0x72,0x20,0x30,0x30,0x31, // Genuine Adobe Flash Player 001
            
            0xF0,0xEE,
            0xC2,0x4A,0x80,0x68,0xBE,0xE8,0x2E,0x00,0xD0,0xD1,0x02,0x9E,0x7E,0x57,0x6E,0xEC,
            0x5D,0x2D,0x29,0x80,0x6F,0xAB,0x93,0xB8,0xE6,0x36,0xCF,0xEB,0x31,0xAE
        };

        static readonly byte[] GenuineFMSKey = new byte[68]  
        {
            0x47, 0x65, 0x6e, 0x75, 0x69, 0x6e, 0x65, 0x20, 0x41, 0x64, 0x6f, 0x62, 0x65, 0x20, 0x46, 0x6c,
            0x61, 0x73, 0x68, 0x20, 0x4d, 0x65, 0x64, 0x69, 0x61, 0x20, 0x53, 0x65, 0x72, 0x76, 0x65, 0x72,
            0x20, 0x30, 0x30, 0x31, // Genuine Adobe Flash Media Server 001 

            0xf0, 0xee, 0xc2, 0x4a, 0x80, 0x68, 0xbe, 0xe8, 0x2e, 0x00, 0xd0, 0xd1,
            0x02, 0x9e, 0x7e, 0x57, 0x6e, 0xec, 0x5d, 0x2d, 0x29, 0x80, 0x6f, 0xab, 0x93, 0xb8, 0xe6, 0x36,
            0xcf, 0xeb, 0x31, 0xae 
        };

        static readonly uint[][] rtmpe8_keys = new uint[16][] 
        {
	        new uint[4]{0xbff034b2, 0x11d9081f, 0xccdfb795, 0x748de732},
            new uint[4]{0x086a5eb6, 0x1743090e, 0x6ef05ab8, 0xfe5a39e2},
	        new uint[4]{0x7b10956f, 0x76ce0521, 0x2388a73a, 0x440149a1},
	        new uint[4]{0xa943f317, 0xebf11bb2, 0xa691a5ee, 0x17f36339},
	        new uint[4]{0x7a30e00a, 0xb529e22c, 0xa087aea5, 0xc0cb79ac},
	        new uint[4]{0xbdce0c23, 0x2febdeff, 0x1cfaae16, 0x1123239d},
	        new uint[4]{0x55dd3f7b, 0x77e7e62e, 0x9bb8c499, 0xc9481ee4},
	        new uint[4]{0x407bb6b4, 0x71e89136, 0xa7aebf55, 0xca33b839},
	        new uint[4]{0xfcf6bdc3, 0xb63c3697, 0x7ce4f825, 0x04d959b2},
	        new uint[4]{0x28e091fd, 0x41954c4c, 0x7fb7db00, 0xe3a066f8},
	        new uint[4]{0x57845b76, 0x4f251b03, 0x46d45bcd, 0xa2c30d29},
	        new uint[4]{0x0acceef8, 0xda55b546, 0x03473452, 0x5863713b},
	        new uint[4]{0xb82075dc, 0xa75f1fee, 0xd84268e8, 0xa72a44cc},
	        new uint[4]{0x07cf6e9e, 0xa16d7b25, 0x9fa7ae6c, 0xd92f5629},
	        new uint[4]{0xfeb1eae4, 0x8c8c3ce1, 0x4e0064a7, 0x6a387c2a},
	        new uint[4]{0x893a9427, 0xcc3013a2, 0xf106385b, 0xa829f927}
        };

        static readonly uint[][] bf_sinit = new uint[4][]
        {
          /* S-Box 0 */ 
          new uint[256]{ 0xd1310ba6, 0x98dfb5ac, 0x2ffd72db, 0xd01adfb7, 0xb8e1afed, 0x6a267e96, 
            0xba7c9045, 0xf12c7f99, 0x24a19947, 0xb3916cf7, 0x0801f2e2, 0x858efc16, 
            0x636920d8, 0x71574e69, 0xa458fea3, 0xf4933d7e, 0x0d95748f, 0x728eb658, 
            0x718bcd58, 0x82154aee, 0x7b54a41d, 0xc25a59b5, 0x9c30d539, 0x2af26013, 
            0xc5d1b023, 0x286085f0, 0xca417918, 0xb8db38ef, 0x8e79dcb0, 0x603a180e, 
            0x6c9e0e8b, 0xb01e8a3e, 0xd71577c1, 0xbd314b27, 0x78af2fda, 0x55605c60, 
            0xe65525f3, 0xaa55ab94, 0x57489862, 0x63e81440, 0x55ca396a, 0x2aab10b6, 
            0xb4cc5c34, 0x1141e8ce, 0xa15486af, 0x7c72e993, 0xb3ee1411, 0x636fbc2a, 
            0x2ba9c55d, 0x741831f6, 0xce5c3e16, 0x9b87931e, 0xafd6ba33, 0x6c24cf5c, 
            0x7a325381, 0x28958677, 0x3b8f4898, 0x6b4bb9af, 0xc4bfe81b, 0x66282193, 
            0x61d809cc, 0xfb21a991, 0x487cac60, 0x5dec8032, 0xef845d5d, 0xe98575b1, 
            0xdc262302, 0xeb651b88, 0x23893e81, 0xd396acc5, 0x0f6d6ff3, 0x83f44239, 
            0x2e0b4482, 0xa4842004, 0x69c8f04a, 0x9e1f9b5e, 0x21c66842, 0xf6e96c9a, 
            0x670c9c61, 0xabd388f0, 0x6a51a0d2, 0xd8542f68, 0x960fa728, 0xab5133a3, 
            0x6eef0b6c, 0x137a3be4, 0xba3bf050, 0x7efb2a98, 0xa1f1651d, 0x39af0176, 
            0x66ca593e, 0x82430e88, 0x8cee8619, 0x456f9fb4, 0x7d84a5c3, 0x3b8b5ebe, 
            0xe06f75d8, 0x85c12073, 0x401a449f, 0x56c16aa6, 0x4ed3aa62, 0x363f7706, 
            0x1bfedf72, 0x429b023d, 0x37d0d724, 0xd00a1248, 0xdb0fead3, 0x49f1c09b, 
            0x075372c9, 0x80991b7b, 0x25d479d8, 0xf6e8def7, 0xe3fe501a, 0xb6794c3b, 
            0x976ce0bd, 0x04c006ba, 0xc1a94fb6, 0x409f60c4, 0x5e5c9ec2, 0x196a2463, 
            0x68fb6faf, 0x3e6c53b5, 0x1339b2eb, 0x3b52ec6f, 0x6dfc511f, 0x9b30952c, 
            0xcc814544, 0xaf5ebd09, 0xbee3d004, 0xde334afd, 0x660f2807, 0x192e4bb3, 
            0xc0cba857, 0x45c8740f, 0xd20b5f39, 0xb9d3fbdb, 0x5579c0bd, 0x1a60320a, 
            0xd6a100c6, 0x402c7279, 0x679f25fe, 0xfb1fa3cc, 0x8ea5e9f8, 0xdb3222f8, 
            0x3c7516df, 0xfd616b15, 0x2f501ec8, 0xad0552ab, 0x323db5fa, 0xfd238760, 
            0x53317b48, 0x3e00df82, 0x9e5c57bb, 0xca6f8ca0, 0x1a87562e, 0xdf1769db, 
            0xd542a8f6, 0x287effc3, 0xac6732c6, 0x8c4f5573, 0x695b27b0, 0xbbca58c8, 
            0xe1ffa35d, 0xb8f011a0, 0x10fa3d98, 0xfd2183b8, 0x4afcb56c, 0x2dd1d35b, 
            0x9a53e479, 0xb6f84565, 0xd28e49bc, 0x4bfb9790, 0xe1ddf2da, 0xa4cb7e33, 
            0x62fb1341, 0xcee4c6e8, 0xef20cada, 0x36774c01, 0xd07e9efe, 0x2bf11fb4, 
            0x95dbda4d, 0xae909198, 0xeaad8e71, 0x6b93d5a0, 0xd08ed1d0, 0xafc725e0, 
            0x8e3c5b2f, 0x8e7594b7, 0x8ff6e2fb, 0xf2122b64, 0x8888b812, 0x900df01c, 
            0x4fad5ea0, 0x688fc31c, 0xd1cff191, 0xb3a8c1ad, 0x2f2f2218, 0xbe0e1777, 
            0xea752dfe, 0x8b021fa1, 0xe5a0cc0f, 0xb56f74e8, 0x18acf3d6, 0xce89e299, 
            0xb4a84fe0, 0xfd13e0b7, 0x7cc43b81, 0xd2ada8d9, 0x165fa266, 0x80957705, 
            0x93cc7314, 0x211a1477, 0xe6ad2065, 0x77b5fa86, 0xc75442f5, 0xfb9d35cf, 
            0xebcdaf0c, 0x7b3e89a0, 0xd6411bd3, 0xae1e7e49, 0x00250e2d, 0x2071b35e, 
            0x226800bb, 0x57b8e0af, 0x2464369b, 0xf009b91e, 0x5563911d, 0x59dfa6aa, 
            0x78c14389, 0xd95a537f, 0x207d5ba2, 0x02e5b9c5, 0x83260376, 0x6295cfa9, 
            0x11c81968, 0x4e734a41, 0xb3472dca, 0x7b14a94a, 0x1b510052, 0x9a532915, 
            0xd60f573f, 0xbc9bc6e4, 0x2b60a476, 0x81e67400, 0x08ba6fb5, 0x571be91f, 
            0xf296ec6b, 0x2a0dd915, 0xb6636521, 0xe7b9f9b6, 0xff34052e, 0xc5855664, 
            0x53b02d5d, 0xa99f8fa1, 0x08ba4799, 0x6e85076a, }, 
 
          /* S-Box 1 */ 
          new uint[256]{ 0x4b7a70e9, 0xb5b32944, 0xdb75092e, 0xc4192623, 0xad6ea6b0, 0x49a7df7d, 
            0x9cee60b8, 0x8fedb266, 0xecaa8c71, 0x699a17ff, 0x5664526c, 0xc2b19ee1, 
            0x193602a5, 0x75094c29, 0xa0591340, 0xe4183a3e, 0x3f54989a, 0x5b429d65, 
            0x6b8fe4d6, 0x99f73fd6, 0xa1d29c07, 0xefe830f5, 0x4d2d38e6, 0xf0255dc1, 
            0x4cdd2086, 0x8470eb26, 0x6382e9c6, 0x021ecc5e, 0x09686b3f, 0x3ebaefc9, 
            0x3c971814, 0x6b6a70a1, 0x687f3584, 0x52a0e286, 0xb79c5305, 0xaa500737, 
            0x3e07841c, 0x7fdeae5c, 0x8e7d44ec, 0x5716f2b8, 0xb03ada37, 0xf0500c0d, 
            0xf01c1f04, 0x0200b3ff, 0xae0cf51a, 0x3cb574b2, 0x25837a58, 0xdc0921bd, 
            0xd19113f9, 0x7ca92ff6, 0x94324773, 0x22f54701, 0x3ae5e581, 0x37c2dadc, 
            0xc8b57634, 0x9af3dda7, 0xa9446146, 0x0fd0030e, 0xecc8c73e, 0xa4751e41, 
            0xe238cd99, 0x3bea0e2f, 0x3280bba1, 0x183eb331, 0x4e548b38, 0x4f6db908, 
            0x6f420d03, 0xf60a04bf, 0x2cb81290, 0x24977c79, 0x5679b072, 0xbcaf89af, 
            0xde9a771f, 0xd9930810, 0xb38bae12, 0xdccf3f2e, 0x5512721f, 0x2e6b7124, 
            0x501adde6, 0x9f84cd87, 0x7a584718, 0x7408da17, 0xbc9f9abc, 0xe94b7d8c, 
            0xec7aec3a, 0xdb851dfa, 0x63094366, 0xc464c3d2, 0xef1c1847, 0x3215d908, 
            0xdd433b37, 0x24c2ba16, 0x12a14d43, 0x2a65c451, 0x50940002, 0x133ae4dd, 
            0x71dff89e, 0x10314e55, 0x81ac77d6, 0x5f11199b, 0x043556f1, 0xd7a3c76b, 
            0x3c11183b, 0x5924a509, 0xf28fe6ed, 0x97f1fbfa, 0x9ebabf2c, 0x1e153c6e, 
            0x86e34570, 0xeae96fb1, 0x860e5e0a, 0x5a3e2ab3, 0x771fe71c, 0x4e3d06fa, 
            0x2965dcb9, 0x99e71d0f, 0x803e89d6, 0x5266c825, 0x2e4cc978, 0x9c10b36a, 
            0xc6150eba, 0x94e2ea78, 0xa5fc3c53, 0x1e0a2df4, 0xf2f74ea7, 0x361d2b3d, 
            0x1939260f, 0x19c27960, 0x5223a708, 0xf71312b6, 0xebadfe6e, 0xeac31f66, 
            0xe3bc4595, 0xa67bc883, 0xb17f37d1, 0x018cff28, 0xc332ddef, 0xbe6c5aa5, 
            0x65582185, 0x68ab9802, 0xeecea50f, 0xdb2f953b, 0x2aef7dad, 0x5b6e2f84, 
            0x1521b628, 0x29076170, 0xecdd4775, 0x619f1510, 0x13cca830, 0xeb61bd96, 
            0x0334fe1e, 0xaa0363cf, 0xb5735c90, 0x4c70a239, 0xd59e9e0b, 0xcbaade14, 
            0xeecc86bc, 0x60622ca7, 0x9cab5cab, 0xb2f3846e, 0x648b1eaf, 0x19bdf0ca, 
            0xa02369b9, 0x655abb50, 0x40685a32, 0x3c2ab4b3, 0x319ee9d5, 0xc021b8f7, 
            0x9b540b19, 0x875fa099, 0x95f7997e, 0x623d7da8, 0xf837889a, 0x97e32d77, 
            0x11ed935f, 0x16681281, 0x0e358829, 0xc7e61fd6, 0x96dedfa1, 0x7858ba99, 
            0x57f584a5, 0x1b227263, 0x9b83c3ff, 0x1ac24696, 0xcdb30aeb, 0x532e3054, 
            0x8fd948e4, 0x6dbc3128, 0x58ebf2ef, 0x34c6ffea, 0xfe28ed61, 0xee7c3c73, 
            0x5d4a14d9, 0xe864b7e3, 0x42105d14, 0x203e13e0, 0x45eee2b6, 0xa3aaabea, 
            0xdb6c4f15, 0xfacb4fd0, 0xc742f442, 0xef6abbb5, 0x654f3b1d, 0x41cd2105, 
            0xd81e799e, 0x86854dc7, 0xe44b476a, 0x3d816250, 0xcf62a1f2, 0x5b8d2646, 
            0xfc8883a0, 0xc1c7b6a3, 0x7f1524c3, 0x69cb7492, 0x47848a0b, 0x5692b285, 
            0x095bbf00, 0xad19489d, 0x1462b174, 0x23820e00, 0x58428d2a, 0x0c55f5ea, 
            0x1dadf43e, 0x233f7061, 0x3372f092, 0x8d937e41, 0xd65fecf1, 0x6c223bdb, 
            0x7cde3759, 0xcbee7460, 0x4085f2a7, 0xce77326e, 0xa6078084, 0x19f8509e, 
            0xe8efd855, 0x61d99735, 0xa969a7aa, 0xc50c06c2, 0x5a04abfc, 0x800bcadc, 
            0x9e447a2e, 0xc3453484, 0xfdd56705, 0x0e1e9ec9, 0xdb73dbd3, 0x105588cd, 
            0x675fda79, 0xe3674340, 0xc5c43465, 0x713e38d8, 0x3d28f89e, 0xf16dff20, 
            0x153e21e7, 0x8fb03d4a, 0xe6e39f2b, 0xdb83adf7, }, 
 
          /* S-Box 2 */ 
          new uint[256]{ 0xe93d5a68, 0x948140f7, 0xf64c261c, 0x94692934, 0x411520f7, 0x7602d4f7, 
            0xbcf46b2e, 0xd4a20068, 0xd4082471, 0x3320f46a, 0x43b7d4b7, 0x500061af, 
            0x1e39f62e, 0x97244546, 0x14214f74, 0xbf8b8840, 0x4d95fc1d, 0x96b591af, 
            0x70f4ddd3, 0x66a02f45, 0xbfbc09ec, 0x03bd9785, 0x7fac6dd0, 0x31cb8504, 
            0x96eb27b3, 0x55fd3941, 0xda2547e6, 0xabca0a9a, 0x28507825, 0x530429f4, 
            0x0a2c86da, 0xe9b66dfb, 0x68dc1462, 0xd7486900, 0x680ec0a4, 0x27a18dee, 
            0x4f3ffea2, 0xe887ad8c, 0xb58ce006, 0x7af4d6b6, 0xaace1e7c, 0xd3375fec, 
            0xce78a399, 0x406b2a42, 0x20fe9e35, 0xd9f385b9, 0xee39d7ab, 0x3b124e8b, 
            0x1dc9faf7, 0x4b6d1856, 0x26a36631, 0xeae397b2, 0x3a6efa74, 0xdd5b4332, 
            0x6841e7f7, 0xca7820fb, 0xfb0af54e, 0xd8feb397, 0x454056ac, 0xba489527, 
            0x55533a3a, 0x20838d87, 0xfe6ba9b7, 0xd096954b, 0x55a867bc, 0xa1159a58, 
            0xcca92963, 0x99e1db33, 0xa62a4a56, 0x3f3125f9, 0x5ef47e1c, 0x9029317c, 
            0xfdf8e802, 0x04272f70, 0x80bb155c, 0x05282ce3, 0x95c11548, 0xe4c66d22, 
            0x48c1133f, 0xc70f86dc, 0x07f9c9ee, 0x41041f0f, 0x404779a4, 0x5d886e17, 
            0x325f51eb, 0xd59bc0d1, 0xf2bcc18f, 0x41113564, 0x257b7834, 0x602a9c60, 
            0xdff8e8a3, 0x1f636c1b, 0x0e12b4c2, 0x02e1329e, 0xaf664fd1, 0xcad18115, 
            0x6b2395e0, 0x333e92e1, 0x3b240b62, 0xeebeb922, 0x85b2a20e, 0xe6ba0d99, 
            0xde720c8c, 0x2da2f728, 0xd0127845, 0x95b794fd, 0x647d0862, 0xe7ccf5f0, 
            0x5449a36f, 0x877d48fa, 0xc39dfd27, 0xf33e8d1e, 0x0a476341, 0x992eff74, 
            0x3a6f6eab, 0xf4f8fd37, 0xa812dc60, 0xa1ebddf8, 0x991be14c, 0xdb6e6b0d, 
            0xc67b5510, 0x6d672c37, 0x2765d43b, 0xdcd0e804, 0xf1290dc7, 0xcc00ffa3, 
            0xb5390f92, 0x690fed0b, 0x667b9ffb, 0xcedb7d9c, 0xa091cf0b, 0xd9155ea3, 
            0xbb132f88, 0x515bad24, 0x7b9479bf, 0x763bd6eb, 0x37392eb3, 0xcc115979, 
            0x8026e297, 0xf42e312d, 0x6842ada7, 0xc66a2b3b, 0x12754ccc, 0x782ef11c, 
            0x6a124237, 0xb79251e7, 0x06a1bbe6, 0x4bfb6350, 0x1a6b1018, 0x11caedfa, 
            0x3d25bdd8, 0xe2e1c3c9, 0x44421659, 0x0a121386, 0xd90cec6e, 0xd5abea2a, 
            0x64af674e, 0xda86a85f, 0xbebfe988, 0x64e4c3fe, 0x9dbc8057, 0xf0f7c086, 
            0x60787bf8, 0x6003604d, 0xd1fd8346, 0xf6381fb0, 0x7745ae04, 0xd736fccc, 
            0x83426b33, 0xf01eab71, 0xb0804187, 0x3c005e5f, 0x77a057be, 0xbde8ae24, 
            0x55464299, 0xbf582e61, 0x4e58f48f, 0xf2ddfda2, 0xf474ef38, 0x8789bdc2, 
            0x5366f9c3, 0xc8b38e74, 0xb475f255, 0x46fcd9b9, 0x7aeb2661, 0x8b1ddf84, 
            0x846a0e79, 0x915f95e2, 0x466e598e, 0x20b45770, 0x8cd55591, 0xc902de4c, 
            0xb90bace1, 0xbb8205d0, 0x11a86248, 0x7574a99e, 0xb77f19b6, 0xe0a9dc09, 
            0x662d09a1, 0xc4324633, 0xe85a1f02, 0x09f0be8c, 0x4a99a025, 0x1d6efe10, 
            0x1ab93d1d, 0x0ba5a4df, 0xa186f20f, 0x2868f169, 0xdcb7da83, 0x573906fe, 
            0xa1e2ce9b, 0x4fcd7f52, 0x50115e01, 0xa70683fa, 0xa002b5c4, 0x0de6d027, 
            0x9af88c27, 0x773f8641, 0xc3604c06, 0x61a806b5, 0xf0177a28, 0xc0f586e0, 
            0x006058aa, 0x30dc7d62, 0x11e69ed7, 0x2338ea63, 0x53c2dd94, 0xc2c21634, 
            0xbbcbee56, 0x90bcb6de, 0xebfc7da1, 0xce591d76, 0x6f05e409, 0x4b7c0188, 
            0x39720a3d, 0x7c927c24, 0x86e3725f, 0x724d9db9, 0x1ac15bb4, 0xd39eb8fc, 
            0xed545578, 0x08fca5b5, 0xd83d7cd3, 0x4dad0fc4, 0x1e50ef5e, 0xb161e6f8, 
            0xa28514d9, 0x6c51133c, 0x6fd5c7e7, 0x56e14ec4, 0x362abfce, 0xddc6c837, 
            0xd79a3234, 0x92638212, 0x670efa8e, 0x406000e0, }, 
 
          /* S-Box 3 */ 
          new uint[256]{ 0x3a39ce37, 0xd3faf5cf, 0xabc27737, 0x5ac52d1b, 0x5cb0679e, 0x4fa33742, 
            0xd3822740, 0x99bc9bbe, 0xd5118e9d, 0xbf0f7315, 0xd62d1c7e, 0xc700c47b, 
            0xb78c1b6b, 0x21a19045, 0xb26eb1be, 0x6a366eb4, 0x5748ab2f, 0xbc946e79, 
            0xc6a376d2, 0x6549c2c8, 0x530ff8ee, 0x468dde7d, 0xd5730a1d, 0x4cd04dc6, 
            0x2939bbdb, 0xa9ba4650, 0xac9526e8, 0xbe5ee304, 0xa1fad5f0, 0x6a2d519a, 
            0x63ef8ce2, 0x9a86ee22, 0xc089c2b8, 0x43242ef6, 0xa51e03aa, 0x9cf2d0a4, 
            0x83c061ba, 0x9be96a4d, 0x8fe51550, 0xba645bd6, 0x2826a2f9, 0xa73a3ae1, 
            0x4ba99586, 0xef5562e9, 0xc72fefd3, 0xf752f7da, 0x3f046f69, 0x77fa0a59, 
            0x80e4a915, 0x87b08601, 0x9b09e6ad, 0x3b3ee593, 0xe990fd5a, 0x9e34d797, 
            0x2cf0b7d9, 0x022b8b51, 0x96d5ac3a, 0x017da67d, 0xd1cf3ed6, 0x7c7d2d28, 
            0x1f9f25cf, 0xadf2b89b, 0x5ad6b472, 0x5a88f54c, 0xe029ac71, 0xe019a5e6, 
            0x47b0acfd, 0xed93fa9b, 0xe8d3c48d, 0x283b57cc, 0xf8d56629, 0x79132e28, 
            0x785f0191, 0xed756055, 0xf7960e44, 0xe3d35e8c, 0x15056dd4, 0x88f46dba, 
            0x03a16125, 0x0564f0bd, 0xc3eb9e15, 0x3c9057a2, 0x97271aec, 0xa93a072a, 
            0x1b3f6d9b, 0x1e6321f5, 0xf59c66fb, 0x26dcf319, 0x7533d928, 0xb155fdf5, 
            0x03563482, 0x8aba3cbb, 0x28517711, 0xc20ad9f8, 0xabcc5167, 0xccad925f, 
            0x4de81751, 0x3830dc8e, 0x379d5862, 0x9320f991, 0xea7a90c2, 0xfb3e7bce, 
            0x5121ce64, 0x774fbe32, 0xa8b6e37e, 0xc3293d46, 0x48de5369, 0x6413e680, 
            0xa2ae0810, 0xdd6db224, 0x69852dfd, 0x09072166, 0xb39a460a, 0x6445c0dd, 
            0x586cdecf, 0x1c20c8ae, 0x5bbef7dd, 0x1b588d40, 0xccd2017f, 0x6bb4e3bb, 
            0xdda26a7e, 0x3a59ff45, 0x3e350a44, 0xbcb4cdd5, 0x72eacea8, 0xfa6484bb, 
            0x8d6612ae, 0xbf3c6f47, 0xd29be463, 0x542f5d9e, 0xaec2771b, 0xf64e6370, 
            0x740e0d8d, 0xe75b1357, 0xf8721671, 0xaf537d5d, 0x4040cb08, 0x4eb4e2cc, 
            0x34d2466a, 0x0115af84, 0xe1b00428, 0x95983a1d, 0x06b89fb4, 0xce6ea048, 
            0x6f3f3b82, 0x3520ab82, 0x011a1d4b, 0x277227f8, 0x611560b1, 0xe7933fdc, 
            0xbb3a792b, 0x344525bd, 0xa08839e1, 0x51ce794b, 0x2f32c9b7, 0xa01fbac9, 
            0xe01cc87e, 0xbcc7d1f6, 0xcf0111c3, 0xa1e8aac7, 0x1a908749, 0xd44fbd9a, 
            0xd0dadecb, 0xd50ada38, 0x0339c32a, 0xc6913667, 0x8df9317c, 0xe0b12b4f, 
            0xf79e59b7, 0x43f5bb3a, 0xf2d519ff, 0x27d9459c, 0xbf97222c, 0x15e6fc2a, 
            0x0f91fc71, 0x9b941525, 0xfae59361, 0xceb69ceb, 0xc2a86459, 0x12baa8d1, 
            0xb6c1075e, 0xe3056a0c, 0x10d25065, 0xcb03a442, 0xe0ec6e0e, 0x1698db3b, 
            0x4c98a0be, 0x3278e964, 0x9f1f9532, 0xe0d392df, 0xd3a0342b, 0x8971f21e, 
            0x1b0a7441, 0x4ba3348c, 0xc5be7120, 0xc37632d8, 0xdf359f8d, 0x9b992f2e, 
            0xe60b6f47, 0x0fe3f11d, 0xe54cda54, 0x1edad891, 0xce6279cf, 0xcd3e7e6f, 
            0x1618b166, 0xfd2c1d05, 0x848fd2c5, 0xf6fb2299, 0xf523f357, 0xa6327623, 
            0x93a83531, 0x56cccd02, 0xacf08162, 0x5a75ebb5, 0x6e163697, 0x88d273cc, 
            0xde966292, 0x81b949d0, 0x4c50901b, 0x71c65614, 0xe6c6c7bd, 0x327a140a, 
            0x45e1d006, 0xc3f27b9a, 0xc9aa53fd, 0x62a80f00, 0xbb25bfe2, 0x35bdd2f6, 
            0x71126905, 0xb2040222, 0xb6cbcf7c, 0xcd769c2b, 0x53113ec0, 0x1640e3d3, 
            0x38abbd60, 0x2547adf0, 0xba38209c, 0xf746ce76, 0x77afa1c5, 0x20756060, 
            0x85cbfe4e, 0x8ae88dd8, 0x7aaaf9b0, 0x4cf9aa7e, 0x1948c25c, 0x02fb8a8c, 
            0x01c36ae4, 0xd6ebe1f9, 0x90d4f869, 0xa65cdea0, 0x3f09252d, 0xc208e69f, 
            0xb74e6132, 0xce77e25b, 0x578fdfe3, 0x3ac372e6, }, 
        };


        static readonly uint[] bf_pinit = { 
          /* P-Box */ 
          0x243f6a88, 0x85a308d3, 0x13198a2e, 0x03707344, 0xa4093822, 0x299f31d0, 
          0x082efa98, 0xec4e6c89, 0x452821e6, 0x38d01377, 0xbe5466cf, 0x34e90c6c, 
          0xc0ac29b7, 0xc97c50dd, 0x3f84d5b5, 0xb5470917, 0x9216d5d9, 0x8979fb1b, 
        };

        static readonly byte[][] rtmpe9_keys = new byte[16][] { 
         new byte[24]{ 0x79, 0x34, 0x77, 0x4c, 0x67, 0xd1, 0x38, 0x3a, 0xdf, 0xb3, 0x56, 0xbe, 
           0x8b, 0x7b, 0xd0, 0x24, 0x38, 0xe0, 0x73, 0x58, 0x41, 0x5d, 0x69, 0x67, }, 
         new byte[24]{ 0x46, 0xf6, 0xb4, 0xcc, 0x01, 0x93, 0xe3, 0xa1, 0x9e, 0x7d, 0x3c, 0x65, 
           0x55, 0x86, 0xfd, 0x09, 0x8f, 0xf7, 0xb3, 0xc4, 0x6f, 0x41, 0xca, 0x5c, }, 
         new byte[24]{ 0x1a, 0xe7, 0xe2, 0xf3, 0xf9, 0x14, 0x79, 0x94, 0xc0, 0xd3, 0x97, 0x43, 
           0x08, 0x7b, 0xb3, 0x84, 0x43, 0x2f, 0x9d, 0x84, 0x3f, 0x21, 0x01, 0x9b, }, 
         new byte[24]{ 0xd3, 0xe3, 0x54, 0xb0, 0xf7, 0x1d, 0xf6, 0x2b, 0x5a, 0x43, 0x4d, 0x04, 
           0x83, 0x64, 0x3e, 0x0d, 0x59, 0x2f, 0x61, 0xcb, 0xb1, 0x6a, 0x59, 0x0d, }, 
         new byte[24]{ 0xc8, 0xc1, 0xe9, 0xb8, 0x16, 0x56, 0x99, 0x21, 0x7b, 0x5b, 0x36, 0xb7, 
           0xb5, 0x9b, 0xdf, 0x06, 0x49, 0x2c, 0x97, 0xf5, 0x95, 0x48, 0x85, 0x7e, }, 
         new byte[24]{ 0xeb, 0xe5, 0xe6, 0x2e, 0xa4, 0xba, 0xd4, 0x2c, 0xf2, 0x16, 0xe0, 0x8f, 
           0x66, 0x23, 0xa9, 0x43, 0x41, 0xce, 0x38, 0x14, 0x84, 0x95, 0x00, 0x53, }, 
         new byte[24]{ 0x66, 0xdb, 0x90, 0xf0, 0x3b, 0x4f, 0xf5, 0x6f, 0xe4, 0x9c, 0x20, 0x89, 
           0x35, 0x5e, 0xd2, 0xb2, 0xc3, 0x9e, 0x9f, 0x7f, 0x63, 0xb2, 0x28, 0x81, }, 
         new byte[24]{ 0xbb, 0x20, 0xac, 0xed, 0x2a, 0x04, 0x6a, 0x19, 0x94, 0x98, 0x9b, 0xc8, 
           0xff, 0xcd, 0x93, 0xef, 0xc6, 0x0d, 0x56, 0xa7, 0xeb, 0x13, 0xd9, 0x30, }, 
         new byte[24]{ 0xbc, 0xf2, 0x43, 0x82, 0x09, 0x40, 0x8a, 0x87, 0x25, 0x43, 0x6d, 0xe6, 
           0xbb, 0xa4, 0xb9, 0x44, 0x58, 0x3f, 0x21, 0x7c, 0x99, 0xbb, 0x3f, 0x24, }, 
         new byte[24]{ 0xec, 0x1a, 0xaa, 0xcd, 0xce, 0xbd, 0x53, 0x11, 0xd2, 0xfb, 0x83, 0xb6, 
           0xc3, 0xba, 0xab, 0x4f, 0x62, 0x79, 0xe8, 0x65, 0xa9, 0x92, 0x28, 0x76, }, 
         new byte[24]{ 0xc6, 0x0c, 0x30, 0x03, 0x91, 0x18, 0x2d, 0x7b, 0x79, 0xda, 0xe1, 0xd5, 
           0x64, 0x77, 0x9a, 0x12, 0xc5, 0xb1, 0xd7, 0x91, 0x4f, 0x96, 0x4c, 0xa3, }, 
         new byte[24]{ 0xd7, 0x7c, 0x2a, 0xbf, 0xa6, 0xe7, 0x85, 0x7c, 0x45, 0xad, 0xff, 0x12, 
           0x94, 0xd8, 0xde, 0xa4, 0x5c, 0x3d, 0x79, 0xa4, 0x44, 0x02, 0x5d, 0x22, }, 
         new byte[24]{ 0x16, 0x19, 0x0d, 0x81, 0x6a, 0x4c, 0xc7, 0xf8, 0xb8, 0xf9, 0x4e, 0xcd, 
           0x2c, 0x9e, 0x90, 0x84, 0xb2, 0x08, 0x25, 0x60, 0xe1, 0x1e, 0xae, 0x18, }, 
         new byte[24]{ 0xe9, 0x7c, 0x58, 0x26, 0x1b, 0x51, 0x9e, 0x49, 0x82, 0x60, 0x61, 0xfc, 
           0xa0, 0xa0, 0x1b, 0xcd, 0xf5, 0x05, 0xd6, 0xa6, 0x6d, 0x07, 0x88, 0xa3, }, 
         new byte[24]{ 0x2b, 0x97, 0x11, 0x8b, 0xd9, 0x4e, 0xd9, 0xdf, 0x20, 0xe3, 0x9c, 0x10, 
           0xe6, 0xa1, 0x35, 0x21, 0x11, 0xf9, 0x13, 0x0d, 0x0b, 0x24, 0x65, 0xb2, }, 
         new byte[24]{ 0x53, 0x6a, 0x4c, 0x54, 0xac, 0x8b, 0x9b, 0xb8, 0x97, 0x29, 0xfc, 0x60, 
           0x2c, 0x5b, 0x3a, 0x85, 0x68, 0xb5, 0xaa, 0x6a, 0x44, 0xcd, 0x3f, 0xa7, }
        };


        static readonly uint[] packetSize = { 12, 8, 4, 1 };

        const int RTMP_DEFAULT_CHUNKSIZE = 128;
        const int SHA256_DIGEST_LENGTH = 32;
        const int RTMP_LARGE_HEADER_SIZE = 12;
        const int RTMP_SIG_SIZE = 1536;
        const int RTMP_CHANNELS = 65600;

        const int TIMEOUT_RECEIVE = 15000;

        #endregion

        #region Public Properties

        /// <summary>
        /// The <see cref="Link"/> used to establish the rtmp connection and respond to invokes.
        /// </summary>
        public Link Link { get; set; }

        /// <summary>
        /// Current ChunkSize for incoming packets (default: 128 byte)
        /// </summary>
        public int InChunkSize { get; protected set; }

        /// <summary>
        /// indicates if currently streaming a media file
        /// </summary>
        public bool Playing { get; protected set; }

        public int Pausing { get; protected set; }

        /// <summary>
        /// Duration of stream in seconds returned by Metadata
        /// </summary>
        public double Duration { get; protected set; }

        /// <summary>
        /// Sum of bytes of all tracks in the stream (if Metadata was received and held that information)
        /// </summary>
        public long CombinedTracksLength { get; protected set; } // number of bytes

        /// <summary>
        /// sum of bitrates of all tracks in the stream (if Metadata was received and held that information)
        /// </summary>
        public long CombinedBitrates { get; protected set; }

        #endregion

        Socket tcpSocket = null;
        SslStream sslStream = null;

        int outChunkSize = RTMP_DEFAULT_CHUNKSIZE;
        int bytesReadTotal = 0;
        int lastSentBytesRead = 0;
        int m_numInvokes;
        int m_nBufferMS = (10 * 60 * 60 * 1000)	/* 10 hours default */;
        int receiveTimeoutMS = TIMEOUT_RECEIVE; // intial timeout until stream is connected (don't set too low, as the server's answer after the handshake might take some time)
        int m_stream_id; // returned in _result from invoking createStream            
        int m_mediaChannel = 0;
        int m_nBWCheckCounter;
        int m_nServerBW;
        int m_nClientBW;
        byte m_nClientBW2;
        RTMPPacket[] m_vecChannelsIn = new RTMPPacket[RTMP_CHANNELS];
        RTMPPacket[] m_vecChannelsOut = new RTMPPacket[RTMP_CHANNELS];
        uint[] m_channelTimestamp = new uint[RTMP_CHANNELS]; // abs timestamp of last packet
        Queue<string> m_methodCalls = new Queue<string>(); //remote method calls queue
        public bool invalidRTMPHeader = false;
        uint m_mediaStamp = 0;
        public delegate bool MethodHook(string method, AMFObject obj, RTMP rtmp);
        public MethodHook MethodHookHandler = null;
        public bool SkipCreateStream = false;

        public bool Connect()
        {
            if (Link == null) return false;

            // close any previous connection
            Close();

            // connect
            tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.Connect(Link.hostname, Link.port);
            tcpSocket.NoDelay = true;
            tcpSocket.ReceiveTimeout = receiveTimeoutMS;

            /*if (Link.port == 443)
            {
                sslStream = new SslStream(new NetworkStream(tcpSocket));
                sslStream.AuthenticateAsClient(String.Empty);
            }*/

            if (!HandShake(true)) return false;
            if (!SendConnect()) return false;
            if (!ConnectStream())
            {
                if (!ReconnectStream()) return false;
            }

            // after connection was successfull, set the timeouts for receiving data higher
            receiveTimeoutMS = TIMEOUT_RECEIVE * 2;
            tcpSocket.ReceiveTimeout = receiveTimeoutMS;

            return true;
        }

        public bool IsConnected()
        {
            return tcpSocket != null && tcpSocket.Connected;
        }

        public bool ReconnectStream()
        {
            if (IsConnected())
            {
                DeleteStream();
                SendCreateStream();
                return ConnectStream();
            }
            return false;
        }

        public bool ToggleStream()
        {
            bool res;
            if (Pausing == 0)
            {
                res = SendPause(true);
                if (!res) return res;

                Pausing = 1;
                System.Threading.Thread.Sleep(10);
            }
            res = SendPause(false);
            Pausing = 3;
            return res;
        }

        void DeleteStream()
        {
            if (m_stream_id < 0) return;
            Playing = false;
            SendDeleteStream();
            m_stream_id = -1;
        }

        bool ConnectStream()
        {
            bool firstPacketReceived = false;
            RTMPPacket packet = null;
            while (!Playing && IsConnected() && ReadPacket(out packet))
            {
                if (!packet.IsReady()) continue; // keep reading until complete package has arrived

                if (!firstPacketReceived)
                {
                    firstPacketReceived = true;
                    Logger.Log("First Packet after Connect received.");
                }

                if (packet.PacketType == PacketType.Audio || packet.PacketType == PacketType.Video || packet.PacketType == PacketType.Metadata)
                {
                    Logger.Log("Received FLV packet before play()! Ignoring.");
                    continue;
                }

                ClientPacket(packet);
            }
            return Playing;
        }

        public int GetNextMediaPacket(out RTMPPacket packet)
        {
            int bHasMediaPacket = 0;
            packet = null;

            while (bHasMediaPacket == 0 && IsConnected() && ReadPacket(out packet))
            {
                if (!packet.IsReady()) continue; // keep reading until complete package has arrived
                bHasMediaPacket = ClientPacket(packet);
                if (bHasMediaPacket > 0 && Pausing == 3) Pausing = 0;
                packet.m_nBytesRead = 0;
            }
            if (bHasMediaPacket > 0) Playing = true;
            return bHasMediaPacket;
        }

        /// <summary>
        /// Reacts corresponding to the packet.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns>0 - no media packet, 1 - media packet, 2 - play complete</returns>
        int ClientPacket(RTMPPacket packet)
        {
            int bHasMediaPacket = 0;

            switch (packet.PacketType)
            {
                case PacketType.ChunkSize:
                    HandleChangeChunkSize(packet);
                    break;
                case PacketType.BytesRead:
                    //CLog::Log(LOGDEBUG,"%s, received: bytes read report", __FUNCTION__);
                    break;
                case PacketType.Control:
                    HandlePing(packet);
                    break;
                case PacketType.ServerBW:
                    HandleServerBW(packet);
                    break;
                case PacketType.ClientBW:
                    HandleClientBW(packet);
                    break;
                case PacketType.Audio:
                    //CLog::Log(LOGDEBUG,"%s, received: audio %lu bytes", __FUNCTION__, packet.m_nBodySize);
                    //HandleAudio(packet);
                    if (m_mediaChannel == 0) m_mediaChannel = packet.m_nChannel;
                    if (Pausing == 0) m_mediaStamp = packet.m_nTimeStamp;
                    bHasMediaPacket = 1;
                    break;
                case PacketType.Video:
                    //CLog::Log(LOGDEBUG,"%s, received: video %lu bytes", __FUNCTION__, packet.m_nBodySize);
                    //HandleVideo(packet);
                    if (m_mediaChannel == 0) m_mediaChannel = packet.m_nChannel;
                    if (Pausing == 0) m_mediaStamp = packet.m_nTimeStamp;
                    bHasMediaPacket = 1;
                    break;
                case PacketType.Metadata:
                    //CLog::Log(LOGDEBUG,"%s, received: notify %lu bytes", __FUNCTION__, packet.m_nBodySize);
                    HandleMetadata(packet);
                    bHasMediaPacket = 1;
                    break;
                case PacketType.Invoke:
                    //CLog::Log(LOGDEBUG,"%s, received: invoke %lu bytes", __FUNCTION__, packet.m_nBodySize);
                    if (HandleInvoke(packet) == true) bHasMediaPacket = 2;
                    break;
                case PacketType.FlvTags:
                    //Logger.Log(string.Format("received: FLV tag(s) {0} bytes", packet.m_nBodySize));
                    HandleFlvTags(packet);
                    bHasMediaPacket = 1;
                    break;
                default:
                    Logger.Log(string.Format("Ignoring packet of type {0}", packet.PacketType));
                    break;
            }

            return bHasMediaPacket;
        }

        bool ReadPacket(out RTMPPacket packet)
        {
            // Chunk Basic Header (1, 2 or 3 bytes)
            // the two most significant bits hold the chunk type
            // value in the 6 least significant bits gives the chunk stream id (0,1,2 are reserved): 0 -> 3 byte header | 1 -> 2 byte header | 2 -> low level protocol message | 3-63 -> stream id
            byte[] singleByteToReadBuffer = new byte[1];
            if (ReadN(singleByteToReadBuffer, 0, 1) != 1)
            {
                Logger.Log("failed to read RTMP packet header");
                packet = null;
                return false;
            }

            byte type = singleByteToReadBuffer[0];

            byte headerType = (byte)((type & 0xc0) >> 6);
            int channel = (byte)(type & 0x3f);

            if (channel == 0)
            {
                if (ReadN(singleByteToReadBuffer, 0, 1) != 1)
                {
                    Logger.Log("failed to read RTMP packet header 2nd byte");
                    packet = null;
                    return false;
                }
                channel = singleByteToReadBuffer[0];
                channel += 64;
                //header++;
            }
            else if (channel == 1)
            {
                int tmp;
                byte[] hbuf = new byte[2];

                if (ReadN(hbuf, 0, 2) != 2)
                {
                    Logger.Log("failed to read RTMP packet header 3rd and 4th byte");
                    packet = null;
                    return false;
                }
                tmp = ((hbuf[2]) << 8) + hbuf[1];
                channel = tmp + 64;
                Logger.Log(string.Format("channel: {0}", channel));
                //header += 2;
            }

            uint nSize = packetSize[headerType];

            //Logger.Log(string.Format("reading RTMP packet chunk on channel {0}, headersz {1}", channel, nSize));

            if (nSize < RTMP_LARGE_HEADER_SIZE)
                packet = m_vecChannelsIn[channel]; // using values from the last message of this channel
            else
                packet = new RTMPPacket() { HeaderType = (HeaderType)headerType, m_nChannel = channel, m_hasAbsTimestamp = true }; // new packet

            nSize--;

            byte[] header = new byte[RTMP_LARGE_HEADER_SIZE];
            if (nSize > 0 && ReadN(header, 0, (int)nSize) != nSize)
            {
                Logger.Log(string.Format("failed to read RTMP packet header. type: {0}", type));
                return false;
            }

            if (nSize >= 3)
            {
                packet.m_nTimeStamp = (uint)ReadInt24(header, 0);

                if (nSize >= 6)
                {
                    packet.m_nBodySize = (uint)ReadInt24(header, 3);
                    //Logger.Log(string.Format("new packet body to read {0}", packet.m_nBodySize));
                    packet.m_nBytesRead = 0;
                    packet.Free(); // new packet body

                    if (nSize > 6)
                    {
                        if (Enum.IsDefined(typeof(PacketType), header[6])) packet.PacketType = (PacketType)header[6];
                        else Logger.Log(string.Format("Unknown packet type received: {0}", header[6]));

                        if (nSize == 11)
                            packet.m_nInfoField2 = ReadInt32LE(header, 7);
                    }
                }

                if (packet.m_nTimeStamp == 0xffffff)
                {
                    byte[] extendedTimestampDate = new byte[4];
                    if (ReadN(extendedTimestampDate, 0, 4) != 4)
                    {
                        Logger.Log("failed to read extended timestamp");
                        return false;
                    }
                    packet.m_nTimeStamp = (uint)ReadInt32(extendedTimestampDate, 0);
                }
            }

            if (packet.m_nBodySize >= 0 && packet.m_body == null && !packet.AllocPacket((int)packet.m_nBodySize))
            {
                //CLog::Log(LOGDEBUG,"%s, failed to allocate packet", __FUNCTION__);
                return false;
            }

            uint nToRead = packet.m_nBodySize - packet.m_nBytesRead;
            uint nChunk = (uint)InChunkSize;
            if (nToRead < nChunk)
                nChunk = nToRead;

            int read = ReadN(packet.m_body, (int)packet.m_nBytesRead, (int)nChunk);
            if (read != nChunk)
            {
                Logger.Log(string.Format("failed to read RTMP packet body. total:{0}/{1} chunk:{2}/{3}", packet.m_nBytesRead, packet.m_nBodySize, read, nChunk));
                packet.m_body = null; // we dont want it deleted since its pointed to from the stored packets (m_vecChannelsIn)
                return false;
            }

            packet.m_nBytesRead += nChunk;

            // keep the packet as ref for other packets on this channel
            m_vecChannelsIn[packet.m_nChannel] = packet.ShallowCopy();

            if (packet.IsReady())
            {
                //Logger.Log(string.Format("packet with {0} bytes read", packet.m_nBytesRead));

                // make packet's timestamp absolute 
                if (!packet.m_hasAbsTimestamp)
                    packet.m_nTimeStamp += m_channelTimestamp[packet.m_nChannel]; // timestamps seem to be always relative!! 
                m_channelTimestamp[packet.m_nChannel] = packet.m_nTimeStamp;

                // reset the data from the stored packet. we keep the header since we may use it later if a new packet for this channel
                // arrives and requests to re-use some info (small packet header)
                m_vecChannelsIn[packet.m_nChannel].m_body = null;
                m_vecChannelsIn[packet.m_nChannel].m_nBytesRead = 0;
                m_vecChannelsIn[packet.m_nChannel].m_hasAbsTimestamp = false; // can only be false if we reuse header
            }

            return true;
        }

        public void Close()
        {
            if (sslStream != null)
            {
                try
                {
                    sslStream.Close();
                    sslStream.Dispose();
                }
                catch { }
            }
            if (tcpSocket != null)
            {
                try
                {
                    tcpSocket.Shutdown(SocketShutdown.Both);
                    tcpSocket.Close();
                }
                catch { }
            }

            sslStream = null;
            tcpSocket = null;

            m_nBufferMS = (10 * 60 * 60 * 1000)	/* 10 hours default */;
            Pausing = 0;
            receiveTimeoutMS = TIMEOUT_RECEIVE;

            InChunkSize = RTMP_DEFAULT_CHUNKSIZE;
            outChunkSize = RTMP_DEFAULT_CHUNKSIZE;
            Playing = false;
            Duration = 0;
            CombinedTracksLength = 0;
            CombinedBitrates = 0;

            m_mediaChannel = 0;
            m_stream_id = -1;
            m_nBWCheckCounter = 0;
            m_nClientBW = 2500000;
            m_nClientBW2 = 2;
            m_nServerBW = 2500000;
            bytesReadTotal = 0;
            lastSentBytesRead = 0;
            m_numInvokes = 0;

            for (int i = 0; i < RTMP_CHANNELS; i++)
            {
                m_vecChannelsIn[i] = null;
                m_vecChannelsOut[i] = null;
                m_channelTimestamp[i] = 0;
            }

            m_methodCalls.Clear();
        }

        #region Send Client Packets

        bool SendConnect()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.Invoke;
            packet.AllocPacket(4096);

            Logger.Log("Sending connect");
            List<byte> enc = new List<byte>();
            EncodeString(enc, "connect");
            EncodeNumber(enc, ++m_numInvokes);
            enc.Add(0x03); //Object Datatype                
            EncodeString(enc, "app", Link.app); Logger.Log(string.Format("app : {0}", Link.app));
            if (String.IsNullOrEmpty(Link.flashVer))
                EncodeString(enc, "flashVer", "WIN 10,0,32,18");
            else
                EncodeString(enc, "flashVer", Link.flashVer);
            if (!string.IsNullOrEmpty(Link.swfUrl)) EncodeString(enc, "swfUrl", Link.swfUrl);
            EncodeString(enc, "tcUrl", Link.tcUrl); Logger.Log(string.Format("tcUrl : {0}", Link.tcUrl));
            EncodeBoolean(enc, "fpad", false);
            EncodeNumber(enc, "capabilities", 15.0);
            EncodeNumber(enc, "audioCodecs", 3191.0);
            EncodeNumber(enc, "videoCodecs", 252.0);
            EncodeNumber(enc, "videoFunction", 1.0);
            if (!string.IsNullOrEmpty(Link.pageUrl)) EncodeString(enc, "pageUrl", Link.pageUrl);
            //EncodeNumber(enc, "objectEncoding", 0.0);
            enc.Add(0); enc.Add(0); enc.Add(0x09); // end of object - 0x00 0x00 0x09

            // add auth string
            if (!string.IsNullOrEmpty(Link.auth))
            {
                EncodeBoolean(enc, true);
                EncodeString(enc, Link.auth);
            }

            // add aditional arbitrary AMF connection properties
            if (Link.extras != null)
            {
                foreach (AMFObjectProperty aProp in Link.extras.m_properties) aProp.Encode(enc);
            }

            Array.Copy(enc.ToArray(), packet.m_body, enc.Count);
            packet.m_nBodySize = (uint)enc.Count;

            return SendPacket(packet);
        }

        bool SendPlay()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x08;   // we make 8 our stream channel
            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.Invoke;
            packet.m_nInfoField2 = m_stream_id;

            packet.AllocPacket(256); // should be enough
            List<byte> enc = new List<byte>();

            EncodeString(enc, "play");
            EncodeNumber(enc, ++m_numInvokes);
            enc.Add(0x05); // NULL              

            EncodeString(enc, Link.playpath);

            /* Optional parameters start and len.
             *
             * start: -2, -1, 0, positive number
             *  -2: looks for a live stream, then a recorded stream, if not found any open a live stream
             *  -1: plays a live stream
             * >=0: plays a recorded streams from 'start' milliseconds
            */
            if (Link.bLiveStream)
            {
                EncodeNumber(enc, -1000.0d);
            }
            else
            {
                if (Link.seekTime > 0.0)
                    EncodeNumber(enc, Link.seekTime);
                else
                    EncodeNumber(enc, 0.0d);
            }
            /* len: -1, 0, positive number
             *  -1: plays live or recorded stream to the end (default)
             *   0: plays a frame 'start' ms away from the beginning
             *  >0: plays a live or recoded stream for 'len' milliseconds
             */

            packet.m_body = enc.ToArray();
            packet.m_nBodySize = (uint)enc.Count;

            Logger.Log(string.Format("Sending play: '{0}' from time: '{1}'", Link.playpath, m_channelTimestamp[m_mediaChannel]));

            return SendPacket(packet);
        }

        bool SendPause(bool doPause)
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x08;   // we make 8 our stream channel
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Invoke;

            List<byte> enc = new List<byte>();

            EncodeString(enc, "pause");
            EncodeNumber(enc, ++m_numInvokes);
            enc.Add(0x05); // NULL  
            EncodeBoolean(enc, doPause);
            EncodeNumber(enc, (double)m_channelTimestamp[m_mediaChannel]);

            packet.m_body = enc.ToArray();
            packet.m_nBodySize = (uint)enc.Count;

            Logger.Log(string.Format("Sending pause: ({0}), Time = {1}", doPause.ToString(), m_channelTimestamp[m_mediaChannel]));

            return SendPacket(packet);
        }

        public bool SendFlex(string name, double number)
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.Invoke_AMF3;

            List<byte> enc = new List<byte>();
            enc.Add(0x00);
            EncodeString(enc, name);
            EncodeNumber(enc, 0);
            enc.Add(0x05); // NULL  
            EncodeNumber(enc, number);

            packet.m_body = enc.ToArray();
            packet.m_nBodySize = (uint)enc.Count;

            Logger.Log(string.Format("Sending flex: ({0},{1})", name, number.ToString()));

            return SendPacket(packet);
        }

        public bool SendRequestData(string id, string request)
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.Invoke_AMF3;

            List<byte> enc = new List<byte>();
            enc.Add(0x00);
            EncodeString(enc, "requestData");
            EncodeNumber(enc, 0);
            enc.Add(0x05); // NULL  
            EncodeString(enc, id);
            EncodeString(enc, request);

            packet.m_body = enc.ToArray();
            packet.m_nBodySize = (uint)enc.Count;

            Logger.Log(string.Format("Sending requestData: ({0},{1})", id, request));

            return SendPacket(packet);
        }

        /// <summary>
        /// The type of Ping packet is 0x4 and contains two mandatory parameters and two optional parameters. 
        /// The first parameter is the type of Ping (short integer).
        /// The second parameter is the target of the ping. 
        /// As Ping is always sent in Channel 2 (control channel) and the target object in RTMP header is always 0 
        /// which means the Connection object, 
        /// it's necessary to put an extra parameter to indicate the exact target object the Ping is sent to. 
        /// The second parameter takes this responsibility. 
        /// The value has the same meaning as the target object field in RTMP header. 
        /// (The second value could also be used as other purposes, like RTT Ping/Pong. It is used as the timestamp.) 
        /// The third and fourth parameters are optional and could be looked upon as the parameter of the Ping packet. 
        /// Below is an unexhausted list of Ping messages.
        /// type 0: Clear the stream. No third and fourth parameters. The second parameter could be 0. After the connection is established, a Ping 0,0 will be sent from server to client. The message will also be sent to client on the start of Play and in response of a Seek or Pause/Resume request. This Ping tells client to re-calibrate the clock with the timestamp of the next packet server sends.
        /// type 1: Tell the stream to clear the playing buffer.
        /// type 3: Buffer time of the client. The third parameter is the buffer time in millisecond.
        /// type 4: Reset a stream. Used together with type 0 in the case of VOD. Often sent before type 0.
        /// type 6: Ping the client from server. The second parameter is the current time.
        /// type 7: Pong reply from client. The second parameter is the time the server sent with his ping request.
        /// type 26: SWFVerification request
        /// type 27: SWFVerification response
        /// type 31: Buffer empty
        /// type 32: Buffer full
        /// </summary>
        /// <param name="nType"></param>
        /// <param name="nObject"></param>
        /// <param name="nTime"></param>
        /// <returns></returns>
        bool SendPing(short nType, uint nObject, uint nTime)
        {
            Logger.Log(string.Format("Sending ping type: {0}", nType));

            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x02;   // control channel (ping)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Control;
            //packet.m_nInfoField1 = System.Environment.TickCount;

            int nSize = (nType == 0x03 ? 10 : 6); // type 3 is the buffer time and requires all 3 parameters. all in all 10 bytes.
            if (nType == 0x1B) nSize = 44;
            packet.AllocPacket(nSize);
            packet.m_nBodySize = (uint)nSize;

            List<byte> buf = new List<byte>();
            EncodeInt16(buf, nType);

            if (nType == 0x1B)
            {
                buf.AddRange(Link.SWFVerificationResponse);
            }
            else
            {
                if (nSize > 2)
                    EncodeInt32(buf, (int)nObject);

                if (nSize > 6)
                    EncodeInt32(buf, (int)nTime);
            }
            packet.m_body = buf.ToArray();
            return SendPacket(packet, false);
        }

        bool SendCheckBW()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)

            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.Invoke;
            //packet.m_nInfoField1 = System.Environment.TickCount;

            Logger.Log("Sending _checkbw");
            List<byte> enc = new List<byte>();
            EncodeString(enc, "_checkbw");
            EncodeNumber(enc, ++m_numInvokes);
            enc.Add(0x05); // NULL            

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            // triggers _onbwcheck and eventually results in _onbwdone
            return SendPacket(packet, false);
        }

        bool SendCheckBWResult(double txn)
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Invoke;
            packet.m_nTimeStamp = (uint)(0x16 * m_nBWCheckCounter); // temp inc value. till we figure it out.

            packet.AllocPacket(256); // should be enough
            List<byte> enc = new List<byte>();
            EncodeString(enc, "_result");
            EncodeNumber(enc, txn);
            enc.Add(0x05); // NULL            
            EncodeNumber(enc, (double)m_nBWCheckCounter++);

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            return SendPacket(packet, false);
        }

        bool SendBytesReceived()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x02;   // control channel (invoke)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.BytesRead;

            packet.AllocPacket(4);
            packet.m_nBodySize = 4;

            List<byte> enc = new List<byte>();
            EncodeInt32(enc, bytesReadTotal);
            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            lastSentBytesRead = bytesReadTotal;
            Logger.Log(string.Format("Send bytes report. ({0} bytes)", bytesReadTotal));
            return SendPacket(packet, false);
        }

        bool SendServerBW()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x02;   // control channel (invoke)
            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.ServerBW;

            packet.AllocPacket(4);
            packet.m_nBodySize = 4;

            List<byte> bytesToSend = new List<byte>();
            EncodeInt32(bytesToSend, m_nServerBW); // was hard coded : 0x001312d0
            packet.m_body = bytesToSend.ToArray();
            return SendPacket(packet, false);
        }

        public bool SendCreateStream()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Invoke;

            Logger.Log("Sending createStream");
            packet.AllocPacket(256); // should be enough
            List<byte> enc = new List<byte>();
            EncodeString(enc, "createStream");
            EncodeNumber(enc, ++m_numInvokes);
            enc.Add(0x05); // NULL

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            return SendPacket(packet);
        }

        bool SendDeleteStream()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Invoke;

            Logger.Log("Sending deleteStream");
            List<byte> enc = new List<byte>();
            EncodeString(enc, "deleteStream");
            EncodeNumber(enc, ++m_numInvokes);
            enc.Add(0x05); // NULL
            EncodeNumber(enc, m_stream_id);

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            /* no response expected */
            return SendPacket(packet, false);
        }

        bool SendSecureTokenResponse(string resp)
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;	/* control channel (invoke) */
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Invoke;

            Logger.Log(string.Format("Sending SecureTokenResponse: {0}", resp));
            List<byte> enc = new List<byte>();
            EncodeString(enc, "secureTokenResponse");
            EncodeNumber(enc, 0.0);
            enc.Add(0x05); // NULL
            EncodeString(enc, resp);

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            return SendPacket(packet, false);
        }

        bool SendFCSubscribe(string subscribepath)
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Invoke;

            Logger.Log(string.Format("Sending FCSubscribe: {0}", subscribepath));
            List<byte> enc = new List<byte>();
            EncodeString(enc, "FCSubscribe");
            EncodeNumber(enc, ++m_numInvokes);
            enc.Add(0x05); // NULL
            EncodeString(enc, subscribepath);

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            return SendPacket(packet);
        }

        bool SendPacket(RTMPPacket packet, bool queue = true)
        {
            uint last = 0;
            uint t = 0;

            RTMPPacket prevPacket = m_vecChannelsOut[packet.m_nChannel];
            if (packet.HeaderType != HeaderType.Large && prevPacket != null)
            {
                // compress a bit by using the prev packet's attributes
                if (prevPacket.m_nBodySize == packet.m_nBodySize &&
                    prevPacket.PacketType == packet.PacketType &&
                    packet.HeaderType == HeaderType.Medium)
                    packet.HeaderType = HeaderType.Small;

                if (prevPacket.m_nTimeStamp == packet.m_nTimeStamp &&
                    packet.HeaderType == HeaderType.Small)
                    packet.HeaderType = HeaderType.Minimum;

                last = prevPacket.m_nTimeStamp;
            }

            uint nSize = packetSize[(byte)packet.HeaderType];
            t = packet.m_nTimeStamp - last;
            List<byte> header = new List<byte>();//byte[RTMP_LARGE_HEADER_SIZE];
            byte c = (byte)(((byte)packet.HeaderType << 6) | packet.m_nChannel);
            header.Add(c);
            if (nSize > 1)
                EncodeInt24(header, (int)t);

            if (nSize > 4)
            {
                EncodeInt24(header, (int)packet.m_nBodySize);
                header.Add((byte)packet.PacketType);
            }

            if (nSize > 8)
                EncodeInt32LE(header, packet.m_nInfoField2);

            uint hSize = nSize;
            byte[] headerBuffer = header.ToArray();
            nSize = packet.m_nBodySize;
            byte[] buffer = packet.m_body;
            uint bufferOffset = 0;
            uint nChunkSize = (uint)outChunkSize;
            while (nSize + hSize > 0)
            {
                if (nSize < nChunkSize) nChunkSize = nSize;

                if (hSize > 0)
                {
                    byte[] combinedBuffer = new byte[headerBuffer.Length + nChunkSize];
                    Array.Copy(headerBuffer, combinedBuffer, headerBuffer.Length);
                    Array.Copy(buffer, (int)bufferOffset, combinedBuffer, headerBuffer.Length, (int)nChunkSize);
                    WriteN(combinedBuffer, 0, combinedBuffer.Length);
                    hSize = 0;
                }
                else
                {
                    WriteN(buffer, (int)bufferOffset, (int)nChunkSize);
                }

                nSize -= nChunkSize;
                bufferOffset += nChunkSize;

                if (nSize > 0)
                {
                    byte sep = (byte)(0xc0 | c);
                    hSize = 1;
                    headerBuffer = new byte[1] { sep };
                }
            }

            if (packet.PacketType == PacketType.Invoke && queue) // we invoked a remote method, keep it in call queue till result arrives
                m_methodCalls.Enqueue(ReadString(packet.m_body, 1));

            m_vecChannelsOut[packet.m_nChannel] = packet;
            //m_vecChannelsOut[packet.m_nChannel].m_body = null;

            return true;
        }

        #endregion

        #region Handle Server Packets

        void HandleChangeChunkSize(RTMPPacket packet)
        {
            if (packet.m_nBodySize >= 4)
            {
                InChunkSize = ReadInt32(packet.m_body, 0);
                Logger.Log(string.Format("received: chunk size change to {0}", InChunkSize));
            }
        }

        void HandlePing(RTMPPacket packet)
        {
            short nType = -1;
            if (packet.m_body != null && packet.m_nBodySize >= 2)
                nType = (short)ReadInt16(packet.m_body, 0);

            Logger.Log(string.Format("received: ping, type: {0}", nType));

            if (packet.m_nBodySize >= 6)
            {
                uint nTime = (uint)ReadInt32(packet.m_body, 2);
                switch (nType)
                {
                    case 0:
                        Logger.Log(string.Format("Stream Begin {0}", nTime));
                        break;
                    case 1:
                        Logger.Log(string.Format("Stream EOF {0}", nTime));
                        if (Pausing == 1) Pausing = 2;
                        break;
                    case 2:
                        Logger.Log(string.Format("Stream Dry {0}", nTime));
                        break;
                    case 4:
                        Logger.Log(string.Format("Stream IsRecorded {0}", nTime));
                        break;
                    case 6:
                        // server ping. reply with pong.
                        Logger.Log(string.Format("Ping {0}", nTime));
                        SendPing(0x07, nTime, 0);
                        break;
                    case 31:
                        Logger.Log(string.Format("Stream BufferEmpty {0}", nTime));
                        if (!Link.bLiveStream)
                        {
                            if (Pausing == 0)
                            {
                                SendPause(true);
                                Pausing = 1;
                            }
                            else if (Pausing == 2)
                            {
                                SendPause(false);
                                Pausing = 3;
                            }
                        }
                        break;
                    case 32:
                        Logger.Log(string.Format("Stream BufferReady {0}", nTime));
                        break;
                    default:
                        Logger.Log(string.Format("Stream xx {0}", nTime));
                        break;
                }
            }

            if (nType == 0x1A)
            {
                // respond with HMAC SHA256 of decompressed SWF, key is the 30byte player key, also the last 30 bytes of the server handshake are applied
                if (Link.SWFHash != null)
                {
                    SendPing(0x1B, 0, 0);
                }
                else
                {
                    Logger.Log("Ignoring SWFVerification request, swfhash and swfsize parameters not set!");
                }
            }
        }

        void HandleServerBW(RTMPPacket packet)
        {
            m_nServerBW = ReadInt32(packet.m_body, 0);
            Logger.Log(string.Format("HandleServerBW: server BW = {0}", m_nServerBW));
        }

        void HandleClientBW(RTMPPacket packet)
        {
            m_nClientBW = ReadInt32(packet.m_body, 0);
            if (packet.m_nBodySize > 4)
                m_nClientBW2 = packet.m_body[4];
            else
                m_nClientBW2 = 0;
            Logger.Log(string.Format("HandleClientBW: client BW = {0} {1}", m_nClientBW, m_nClientBW2));
        }

        public bool GetExpectedPacket(string expectedMethod, out AMFObject obj)
        {
            RTMPPacket packet = null;
            obj = null;
            bool ready = false;
            while (!ready && IsConnected() && ReadPacket(out packet))
            {
                if (!packet.IsReady()) continue; // keep reading until complete package has arrived
                if (packet.PacketType != PacketType.Invoke)
                    Logger.Log(string.Format("Ignoring packet of type {0}", packet.PacketType));
                else
                {
                    if (packet.m_body[0] != 0x02) // make sure it is a string method name we start with
                    {
                        Logger.Log("GetExpectedPacket: Sanity failed. no string method in invoke packet");
                        return false;
                    }

                    obj = new AMFObject();
                    int nRes = obj.Decode(packet.m_body, 0, (int)packet.m_nBodySize, false);
                    if (nRes < 0)
                    {
                        Logger.Log("GetExpectedPacket: error decoding invoke packet");
                        return false;
                    }

                    obj.Dump();
                    string method = obj.GetProperty(0).GetString();
                    double txn = obj.GetProperty(1).GetNumber();
                    Logger.Log(string.Format("server invoking <{0}>", method));
                    if (method == "_result" && m_methodCalls.Count > 0)
                    {
                        string methodInvoked = m_methodCalls.Dequeue();
                        Logger.Log(string.Format("received result for method call <{0}>", methodInvoked));
                    }
                    ready = method == expectedMethod;
                }
            }
            return ready;
        }

        /// <summary>
        /// Analyzes and responds if required to the given <see cref="RTMPPacket"/>.
        /// </summary>
        /// <param name="packet">The <see cref="RTMPPacket"/> to inspect amnd react to.</param>
        /// <returns>0 (false) for OK/Failed/error, 1 for 'Stop or Complete' (true)</returns>
        bool HandleInvoke(RTMPPacket packet)
        {
            bool ret = false;

            if (packet.m_body[0] != 0x02) // make sure it is a string method name we start with
            {
                Logger.Log("HandleInvoke: Sanity failed. no string method in invoke packet");
                return false;
            }

            AMFObject obj = new AMFObject();
            int nRes = obj.Decode(packet.m_body, 0, (int)packet.m_nBodySize, false);
            if (nRes < 0)
            {
                Logger.Log("HandleInvoke: error decoding invoke packet");
                return false;
            }

            obj.Dump();
            string method = obj.GetProperty(0).GetString();
            double txn = obj.GetProperty(1).GetNumber();

            Logger.Log(string.Format("server invoking <{0}>", method));

            if (method == "_result")
            {
                string methodInvoked = m_methodCalls.Dequeue();

                Logger.Log(string.Format("received result for method call <{0}>", methodInvoked));

                if (methodInvoked == "connect")
                {
                    if (!string.IsNullOrEmpty(Link.token))
                    {
                        List<AMFObjectProperty> props = new List<AMFObjectProperty>();
                        obj.FindMatchingProperty("secureToken", props, int.MaxValue);
                        if (props.Count > 0)
                        {
                            string decodedToken = Tea.Decrypt(props[0].GetString(), Link.token);
                            SendSecureTokenResponse(decodedToken);
                        }
                    }
                    SendServerBW();
                    if (!SkipCreateStream)
                    {
                        SendPing(3, 0, 300);
                        SendCreateStream();
                    }
                    if (!string.IsNullOrEmpty(Link.subscribepath)) SendFCSubscribe(Link.subscribepath);
                    else if (Link.bLiveStream) SendFCSubscribe(Link.playpath);
                }
                else if (methodInvoked == "createStream")
                {
                    m_stream_id = (int)obj.GetProperty(3).GetNumber();
                    SendPlay();
                    SendPing(3, (uint)m_stream_id, (uint)m_nBufferMS);
                }
                else if (methodInvoked == "play")
                {
                    Playing = true;
                }
            }
            else if (method == "onBWDone")
            {
                if (m_nBWCheckCounter == 0) SendCheckBW();
            }
            else if (method == "_onbwcheck")
            {
                SendCheckBWResult(txn);
            }
            else if (method == "_onbwdone")
            {
                if (m_methodCalls.Contains("_checkbw"))
                {
                    string[] queue = m_methodCalls.ToArray();
                    m_methodCalls.Clear();
                    for (int i = 0; i < queue.Length; i++) if (queue[i] != "_checkbw") m_methodCalls.Enqueue(queue[i]);
                }
            }
            else if (method == "_error")
            {
                Logger.Log("rtmp server sent error");
            }
            else if (method == "close")
            {
                Logger.Log("rtmp server requested close");
                Close();
            }
            else if (method == "onStatus")
            {
                string code = obj.GetProperty(3).GetObject().GetProperty("code").GetString();
                string level = obj.GetProperty(3).GetObject().GetProperty("level").GetString();

                Logger.Log(string.Format("onStatus: code :{0}, level: {1}", code, level));

                if (code == "NetStream.Failed" || code == "NetStream.Play.Failed" || code == "NetStream.Play.StreamNotFound" || code == "NetConnection.Connect.InvalidApp")
                {
                    Close();
                }
                else if (code == "NetStream.Play.Start" || code == "NetStream.Publish.Start")
                {
                    Playing = true;
                }
                else if (code == "NetStream.Play.Complete" || code == "NetStream.Play.Stop")
                {
                    Close();
                    ret = true;
                }
                else if (code == "NetStream.Pause.Notify")
                {
                    if (Pausing == 1 || Pausing == 2)
                    {
                        SendPause(false);
                        Pausing = 3;
                    }
                }
            }
            else if (MethodHookHandler != null)
            {
                ret = MethodHookHandler(method, obj, this);
            }
            else
            {

            }

            return ret;
        }

        void HandleMetadata(RTMPPacket packet)
        {
            HandleMetadata(packet.m_body, 0, (int)packet.m_nBodySize);
        }

        void HandleMetadata(byte[] buffer, int offset, int size)
        {
            AMFObject obj = new AMFObject();
            int nRes = obj.Decode(buffer, offset, size, false);
            if (nRes < 0)
            {
                //Log(LOGERROR, "%s, error decoding meta data packet", __FUNCTION__);
                return;
            }

            if (!Playing) obj.Dump();
            string metastring = obj.GetProperty(0).GetString();

            if (metastring == "onMetaData")
            {
                if (Playing) obj.Dump(); // always dump metadata for further analyzing

                List<AMFObjectProperty> props = new List<AMFObjectProperty>();
                obj.FindMatchingProperty("duration", props, 1);
                if (props.Count > 0)
                {
                    Duration = props[0].GetNumber();
                    Logger.Log(string.Format("Set duration: {0}", Duration));
                }
                props.Clear();
                obj.FindMatchingProperty("audiodatarate", props, 1);
                if (props.Count > 0)
                {
                    int audiodatarate = (int)props[0].GetNumber();
                    CombinedBitrates += audiodatarate;
                    Logger.Log(string.Format("audiodatarate: {0}", audiodatarate));
                }
                props.Clear();
                obj.FindMatchingProperty("videodatarate", props, 1);
                if (props.Count > 0)
                {
                    int videodatarate = (int)props[0].GetNumber();
                    CombinedBitrates += videodatarate;
                    Logger.Log(string.Format("videodatarate: {0}", videodatarate));
                }
                if (CombinedTracksLength == 0)
                {
                    props.Clear();
                    obj.FindMatchingProperty("filesize", props, int.MaxValue);
                    if (props.Count > 0)
                    {
                        CombinedTracksLength = (int)props[0].GetNumber();
                        Logger.Log(string.Format("Set CombinedTracksLength from filesize: {0}", CombinedTracksLength));
                    }
                }
                if (CombinedTracksLength == 0)
                {
                    props.Clear();
                    obj.FindMatchingProperty("datasize", props, int.MaxValue);
                    if (props.Count > 0)
                    {
                        CombinedTracksLength = (int)props[0].GetNumber();
                        Logger.Log(string.Format("Set CombinedTracksLength from datasize: {0}", CombinedTracksLength));
                    }
                }
            }
        }

        void HandleFlvTags(RTMPPacket packet)
        {
            // go through FLV packets and handle metadata packets
            int pos = 0;
            uint nTimeStamp = packet.m_nTimeStamp;

            while (pos + 11 < packet.m_nBodySize)
            {
                int dataSize = ReadInt24(packet.m_body, pos + 1); // size without header (11) and prevTagSize (4)

                if (pos + 11 + dataSize + 4 > packet.m_nBodySize)
                {
                    Logger.Log("Stream corrupt?!");
                    break;
                }
                if (packet.m_body[pos] == 0x12)
                {
                    HandleMetadata(packet.m_body, pos + 11, dataSize);
                }
                else if (packet.m_body[pos] == 8 || packet.m_body[pos] == 9)
                {
                    nTimeStamp = (uint)ReadInt24(packet.m_body, pos + 4);
                    nTimeStamp |= (uint)(packet.m_body[pos + 7] << 24);
                }
                pos += (11 + dataSize + 4);
            }
            if (Pausing == 0) m_mediaStamp = nTimeStamp;
        }

        #endregion

        #region Encode Functions

        public static void EncodeString(List<byte> output, string strName, string strValue)
        {
            short length = IPAddress.HostToNetworkOrder((short)strName.Length);
            output.AddRange(BitConverter.GetBytes(length));
            output.AddRange(Encoding.ASCII.GetBytes(strName));
            EncodeString(output, strValue);
        }

        public static void EncodeString(List<byte> output, string strValue)
        {
            output.Add(0x02); // type: String
            short length = IPAddress.HostToNetworkOrder((short)strValue.Length);
            output.AddRange(BitConverter.GetBytes(length));
            output.AddRange(Encoding.ASCII.GetBytes(strValue));
        }

        public static void EncodeBoolean(List<byte> output, string strName, bool bVal)
        {
            short length = IPAddress.HostToNetworkOrder((short)strName.Length);
            output.AddRange(BitConverter.GetBytes(length));
            output.AddRange(Encoding.ASCII.GetBytes(strName));
            EncodeBoolean(output, bVal);
        }

        public static void EncodeBoolean(List<byte> output, bool bVal)
        {
            output.Add(0x01); // type: Boolean
            output.Add(bVal ? (byte)0x01 : (byte)0x00);
        }

        public static void EncodeNumber(List<byte> output, string strName, double dVal)
        {
            short length = IPAddress.HostToNetworkOrder((short)strName.Length);
            output.AddRange(BitConverter.GetBytes(length));
            output.AddRange(Encoding.ASCII.GetBytes(strName));
            EncodeNumber(output, dVal);
        }

        public static void EncodeNumber(List<byte> output, double dVal)
        {
            output.Add(0x00); // type: Number
            byte[] bytes = BitConverter.GetBytes(dVal);
            for (int i = bytes.Length - 1; i >= 0; i--) output.Add(bytes[i]); // add in reversed byte order
        }

        public static void EncodeInt16(List<byte> output, short nVal)
        {
            output.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(nVal)));
        }

        public static void EncodeInt24(List<byte> output, int nVal)
        {
            byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(nVal));
            for (int i = 1; i < 4; i++) output.Add(bytes[i]);
        }

        /// <summary>
        /// big-endian 32bit integer
        /// </summary>
        /// <param name="output"></param>
        /// <param name="nVal"></param>
        public static void EncodeInt32(List<byte> output, int nVal, uint offset = 0)
        {
            byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(nVal));
            if (offset == 0)
                output.AddRange(bytes);
            else
                for (int i = 0; i < bytes.Length; i++) output[(int)offset + i] = bytes[i];
        }

        /// <summary>
        /// little-endian 32bit integer
        /// TODO: this is wrong on big-endian processors
        /// </summary>
        /// <param name="output"></param>
        /// <param name="nVal"></param>
        public static void EncodeInt32LE(List<byte> output, int nVal)
        {
            output.AddRange(BitConverter.GetBytes(nVal));
        }

        #endregion

        #region Read Functions

        public static string ReadString(byte[] data, int offset)
        {
            string strRes = "";
            ushort length = ReadInt16(data, offset);
            if (length > 0) strRes = Encoding.ASCII.GetString(data, offset + 2, length);
            return strRes;
        }

        public static string ReadLongString(byte[] data, int offset)
        {
            string strRes = "";
            int length = ReadInt32(data, offset);
            if (length > 0) strRes = Encoding.ASCII.GetString(data, offset + 4, length);
            return strRes;
        }

        public static ushort ReadInt16(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        public static int ReadInt24(byte[] data, int offset)
        {
            return (data[offset] << 16) | (data[offset + 1] << 8) | data[offset + 2];
        }

        /// <summary>
        /// big-endian 32bit integer
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int ReadInt32(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }

        /// <summary>
        /// little-endian 32bit integer
        /// TODO: this is wrong on big-endian processors
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int ReadInt32LE(byte[] data, int offset)
        {
            return BitConverter.ToInt32(data, offset);
        }

        public static bool ReadBool(byte[] data, int offset)
        {
            return data[offset] == 0x01;
        }

        public static double ReadNumber(byte[] data, int offset)
        {
            byte[] bytes = new byte[8];
            Array.Copy(data, offset, bytes, 0, 8);
            Array.Reverse(bytes); // reversed byte order
            return BitConverter.ToDouble(bytes, 0);
        }

        #endregion

        # region Handshake

        bool HandShake(bool FP9HandShake)
        {
            Random rand = new Random(0); // use the same seed everytime to have the same random number everytime (as rtmpdump)

            int offalg = 0;
            int dhposClient = 0;
            int digestPosClient = 0;
            bool encrypted = Link.protocol == Protocol.RTMPE || Link.protocol == Protocol.RTMPTE;

            if (encrypted && !FP9HandShake)
            {
                Logger.Log("RTMPE requires FP9 handshake!");
                return false;
            }

            byte[] clientsig = new byte[RTMP_SIG_SIZE + 1];
            byte[] serversig = new byte[RTMP_SIG_SIZE];

            if (encrypted)
            {
                clientsig[0] = 0x06; // 0x08 is RTMPE as well
                offalg = 1;
            }
            else clientsig[0] = 0x03;

            int uptime = System.Environment.TickCount;
            byte[] uptime_bytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(uptime));
            Array.Copy(uptime_bytes, 0, clientsig, 1, uptime_bytes.Length);

            if (FP9HandShake)
            {
                /* set version to at least 9.0.115.0 */
                if (encrypted)
                {
                    clientsig[5] = 128;
                    clientsig[7] = 3;
                    /*clientsig[5] = 9;
                    clientsig[7] = 0x7c;*/
                }
                else
                {
                    clientsig[5] = 10;
                    clientsig[7] = 45;
                }
                clientsig[6] = 0;
                clientsig[8] = 2;

                Logger.Log(string.Format("Client type: {0}", clientsig[0]));
            }
            else
            {
                clientsig[5] = 0; clientsig[6] = 0; clientsig[7] = 0; clientsig[8] = 0;
            }

            // generate random data
            for (int i = 9; i < RTMP_SIG_SIZE; i += 4) Array.Copy(BitConverter.GetBytes(rand.Next(ushort.MaxValue)), 0, clientsig, i, 4);

            byte[] keyIn = null;
            byte[] keyOut = null;

            if (encrypted)
            {
                // generate Diffie-Hellmann parameters                                
                Org.BouncyCastle.Crypto.Parameters.DHParameters dhParams =
                    new Org.BouncyCastle.Crypto.Parameters.DHParameters(
                        new Org.BouncyCastle.Math.BigInteger(1, DH_MODULUS_BYTES),
                        Org.BouncyCastle.Math.BigInteger.ValueOf(2));
                Org.BouncyCastle.Crypto.Parameters.DHKeyGenerationParameters keySpec = new Org.BouncyCastle.Crypto.Parameters.DHKeyGenerationParameters(new Org.BouncyCastle.Security.SecureRandom(), dhParams);
                Org.BouncyCastle.Crypto.Generators.DHBasicKeyPairGenerator keyGen = new Org.BouncyCastle.Crypto.Generators.DHBasicKeyPairGenerator();
                keyGen.Init(keySpec);
                Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair pair = keyGen.GenerateKeyPair();
                Org.BouncyCastle.Crypto.Agreement.DHBasicAgreement keyAgreement = new Org.BouncyCastle.Crypto.Agreement.DHBasicAgreement();
                keyAgreement.Init(pair.Private);
                Link.keyAgreement = keyAgreement;

                byte[] publicKey = (pair.Public as Org.BouncyCastle.Crypto.Parameters.DHPublicKeyParameters).Y.ToByteArray();

                byte[] temp = new byte[128];
                if (publicKey.Length < 128)
                {
                    Array.Copy(publicKey, 0, temp, 128 - publicKey.Length, publicKey.Length);
                    publicKey = temp;
                    Logger.Log("padded public key length to 128");
                }
                else if (publicKey.Length > 128)
                {
                    Array.Copy(publicKey, publicKey.Length - 128, temp, 0, 128);
                    publicKey = temp;
                    Logger.Log("truncated public key length to 128");
                }

                dhposClient = (int)GetDHOffset(offalg, clientsig, 1, RTMP_SIG_SIZE);
                Logger.Log(string.Format("DH pubkey position: {0}", dhposClient));

                Array.Copy(publicKey, 0, clientsig, 1 + dhposClient, 128);
            }

            // set handshake digest
            if (FP9HandShake)
            {
                digestPosClient = (int)GetDigestOffset(offalg, clientsig, 1, RTMP_SIG_SIZE); // maybe reuse this value in verification
                Logger.Log(string.Format("Client digest offset: {0}", digestPosClient));

                CalculateDigest(digestPosClient, clientsig, 1, GenuineFPKey, 30, clientsig, 1 + digestPosClient);

                Logger.Log("Initial client digest: ");
                string digestAsHexString = "";
                for (int i = 1 + digestPosClient; i < 1 + digestPosClient + SHA256_DIGEST_LENGTH; i++) digestAsHexString += clientsig[i].ToString("X2") + " ";
                Logger.Log(digestAsHexString);
            }

            WriteN(clientsig, 0, RTMP_SIG_SIZE + 1);

            byte[] singleByteToReadBuffer = new byte[1];
            if (ReadN(singleByteToReadBuffer, 0, 1) != 1) return false;
            byte type = singleByteToReadBuffer[0]; // 0x03 or 0x06

            Logger.Log(string.Format("Type Answer   : {0}", type.ToString()));

            if (type != clientsig[0]) Logger.Log(string.Format("Type mismatch: client sent {0}, server answered {1}", clientsig[0], type));

            if (ReadN(serversig, 0, RTMP_SIG_SIZE) != RTMP_SIG_SIZE) return false;

            // decode server response
            uint suptime = (uint)ReadInt32(serversig, 0);

            Logger.Log(string.Format("Server Uptime : {0}", suptime));
            Logger.Log(string.Format("FMS Version   : {0}.{1}.{2}.{3}", serversig[4], serversig[5], serversig[6], serversig[7]));

            if (FP9HandShake && type == 3 && serversig[4] == 0) FP9HandShake = false;

            byte[] clientResp;

            if (FP9HandShake)
            {
                // we have to use this signature now to find the correct algorithms for getting the digest and DH positions
                int digestPosServer = (int)GetDigestOffset(offalg, serversig, 0, RTMP_SIG_SIZE);
                int dhposServer = (int)GetDHOffset(offalg, serversig, 0, RTMP_SIG_SIZE);

                if (!VerifyDigest(digestPosServer, serversig, GenuineFMSKey, 36))
                {
                    Logger.Log("Trying different position for server digest!");
                    offalg ^= 1;
                    digestPosServer = (int)GetDigestOffset(offalg, serversig, 0, RTMP_SIG_SIZE);
                    dhposServer = (int)GetDHOffset(offalg, serversig, 0, RTMP_SIG_SIZE);

                    if (!VerifyDigest(digestPosServer, serversig, GenuineFMSKey, 36))
                    {
                        Logger.Log("Couldn't verify the server digest");//,  continuing anyway, will probably fail!\n");
                        return false;
                    }
                }

                Logger.Log(string.Format("Server DH public key offset: {0}", dhposServer));

                // generate SWFVerification token (SHA256 HMAC hash of decompressed SWF, key are the last 32 bytes of the server handshake)            
                if (Link.SWFHash != null)
                {
                    byte[] swfVerify = new byte[2] { 0x01, 0x01 };
                    Array.Copy(swfVerify, Link.SWFVerificationResponse, 2);
                    List<byte> data = new List<byte>();
                    EncodeInt32(data, Link.SWFSize);
                    EncodeInt32(data, Link.SWFSize);
                    Array.Copy(data.ToArray(), 0, Link.SWFVerificationResponse, 2, data.Count);
                    byte[] key = new byte[SHA256_DIGEST_LENGTH];
                    Array.Copy(serversig, RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH, key, 0, SHA256_DIGEST_LENGTH);
                    HMACsha256(Link.SWFHash, 0, SHA256_DIGEST_LENGTH, key, SHA256_DIGEST_LENGTH, Link.SWFVerificationResponse, 10);
                }

                // do Diffie-Hellmann Key exchange for encrypted RTMP
                if (encrypted)
                {
                    // compute secret key	
                    byte[] secretKey = new byte[128];

                    byte[] serverKey = new byte[128];
                    Array.Copy(serversig, dhposServer, serverKey, 0, 128);

                    Org.BouncyCastle.Crypto.Parameters.DHParameters dhParams =
                        new Org.BouncyCastle.Crypto.Parameters.DHParameters(
                            new Org.BouncyCastle.Math.BigInteger(1, DH_MODULUS_BYTES),
                            Org.BouncyCastle.Math.BigInteger.ValueOf(2));

                    Org.BouncyCastle.Crypto.Parameters.DHPublicKeyParameters dhPubKey =
                        new Org.BouncyCastle.Crypto.Parameters.DHPublicKeyParameters(
                            new Org.BouncyCastle.Math.BigInteger(1, serverKey),
                            dhParams);

                    secretKey = Link.keyAgreement.CalculateAgreement(dhPubKey).ToByteArray();

                    Logger.Log("DH SecretKey:");
                    Logger.LogHex(secretKey, 0, 128);

                    InitRC4Encryption(
                        secretKey,
                        serversig, dhposServer,
                        clientsig, 1 + dhposClient,
                        out keyIn, out keyOut);
                }

                clientResp = new byte[RTMP_SIG_SIZE];

                // generate random data
                for (int i = 0; i < RTMP_SIG_SIZE; i += 4) Array.Copy(BitConverter.GetBytes(rand.Next(ushort.MaxValue)), 0, clientResp, i, 4);

                // calculate response now
                byte[] signatureResp = new byte[SHA256_DIGEST_LENGTH];
                byte[] digestResp = new byte[SHA256_DIGEST_LENGTH];

                HMACsha256(serversig, digestPosServer, SHA256_DIGEST_LENGTH, GenuineFPKey, GenuineFPKey.Length, digestResp, 0);
                HMACsha256(clientResp, 0, RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH, digestResp, SHA256_DIGEST_LENGTH, signatureResp, 0);

                // some info output
                Logger.Log("Calculated digest key from secure key and server digest: ");
                Logger.LogHex(digestResp, 0, SHA256_DIGEST_LENGTH);

                // FP10 stuff
                if (type == 8)
                {
                    /* encrypt signatureResp */
                    for (int i = 0; i < SHA256_DIGEST_LENGTH; i += 8)
                        rtmpe8_sig(signatureResp, i, digestResp[i] % 15);
                }
                else if (type == 9)
                {
                    /* encrypt signatureResp */
                    for (int i = 0; i < SHA256_DIGEST_LENGTH; i += 8)
                        rtmpe9_sig(signatureResp, i, digestResp[i] % 15);
                }

                Logger.Log("Client signature calculated:");
                Logger.LogHex(signatureResp, 0, SHA256_DIGEST_LENGTH);

                Array.Copy(signatureResp, 0, clientResp, RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH, SHA256_DIGEST_LENGTH);
            }
            else
            {
                clientResp = serversig;
            }

            WriteN(clientResp, 0, RTMP_SIG_SIZE);

            // 2nd part of handshake
            byte[] resp = new byte[RTMP_SIG_SIZE];
            if (ReadN(resp, 0, RTMP_SIG_SIZE) != RTMP_SIG_SIZE) return false;

            if (FP9HandShake)
            {
                if (resp[4] == 0 && resp[5] == 0 && resp[6] == 0 && resp[7] == 0)
                {
                    Logger.Log("Wait, did the server just refuse signed authentication?");
                }

                // verify server response
                byte[] signature = new byte[SHA256_DIGEST_LENGTH];
                byte[] digest = new byte[SHA256_DIGEST_LENGTH];

                Logger.Log(string.Format("Client signature digest position: {0}", digestPosClient));
                HMACsha256(clientsig, 1 + digestPosClient, SHA256_DIGEST_LENGTH, GenuineFMSKey, GenuineFMSKey.Length, digest, 0);
                HMACsha256(resp, 0, RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH, digest, SHA256_DIGEST_LENGTH, signature, 0);

                // show some information
                Logger.Log("Digest key: ");
                Logger.LogHex(digest, 0, SHA256_DIGEST_LENGTH);

                // FP10 stuff
                if (type == 8)
                {
                    /* encrypt signatureResp */
                    for (int i = 0; i < SHA256_DIGEST_LENGTH; i += 8)
                        rtmpe8_sig(signature, i, digest[i] % 15);
                }
                else if (type == 9)
                {
                    /* encrypt signatureResp */
                    for (int i = 0; i < SHA256_DIGEST_LENGTH; i += 8)
                        rtmpe9_sig(signature, i, digest[i] % 15);
                }

                Logger.Log("Signature calculated:");
                Logger.LogHex(signature, 0, SHA256_DIGEST_LENGTH);

                Logger.Log("Server sent signature:");
                Logger.LogHex(resp, RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH, SHA256_DIGEST_LENGTH);

                for (int i = 0; i < SHA256_DIGEST_LENGTH; i++)
                    if (signature[i] != resp[RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH + i])
                    {
                        Logger.Log("Server not genuine Adobe!");
                        return false;
                    }
                Logger.Log("Genuine Adobe Flash Media Server");

                if (encrypted)
                {
                    // set keys for encryption from now on
                    Link.rc4In = new Org.BouncyCastle.Crypto.Engines.RC4Engine();
                    Link.rc4In.Init(false, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(keyIn));

                    Link.rc4Out = new Org.BouncyCastle.Crypto.Engines.RC4Engine();
                    Link.rc4Out.Init(true, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(keyOut));

                    // update 'encoder / decoder state' for the RC4 keys
                    // both parties *pretend* as if handshake part 2 (1536 bytes) was encrypted
                    // effectively this hides / discards the first few bytes of encrypted session
                    // which is known to increase the secure-ness of RC4
                    // RC4 state is just a function of number of bytes processed so far
                    // that's why we just run 1536 arbitrary bytes through the keys below
                    byte[] dummyBytes = new byte[RTMP_SIG_SIZE];
                    Link.rc4In.ProcessBytes(dummyBytes, 0, RTMP_SIG_SIZE, new byte[RTMP_SIG_SIZE], 0);
                    Link.rc4Out.ProcessBytes(dummyBytes, 0, RTMP_SIG_SIZE, new byte[RTMP_SIG_SIZE], 0);
                }
            }
            else
            {
                for (int i = 0; i < RTMP_SIG_SIZE; i++)
                    if (resp[i] != clientsig[i + 1])
                    {
                        Logger.Log("client signature does not match!");
                        return false;
                    }
            }

            Logger.Log("Handshaking finished....");
            return true;
        }

        uint GetDHOffset(int alg, byte[] handshake, int bufferoffset, uint len)
        {
            if (alg == 0) return GetDHOffset1(handshake, bufferoffset, len);
            else return GetDHOffset2(handshake, bufferoffset, len);
        }

        uint GetDigestOffset(int alg, byte[] handshake, int bufferoffset, uint len)
        {
            if (alg == 0) return GetDigestOffset1(handshake, bufferoffset, len);
            else return GetDigestOffset2(handshake, bufferoffset, len);
        }

        uint GetDHOffset1(byte[] handshake, int bufferoffset, uint len)
        {
            int offset = 0;
            bufferoffset += 1532;

            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset];// (*ptr);

            int res = (offset % 632) + 772;

            if (res + 128 > 1531)
            {
                Logger.Log(string.Format("Couldn't calculate DH offset (got {0}), exiting!", res));
                throw new Exception();
            }

            return (uint)res;
        }

        uint GetDigestOffset1(byte[] handshake, int bufferoffset, uint len)
        {
            int offset = 0;
            bufferoffset += 8;

            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset];

            int res = (offset % 728) + 12;

            if (res + 32 > 771)
            {
                Logger.Log(string.Format("Couldn't calculate digest offset (got {0}), exiting!", res));
                throw new Exception();
            }

            return (uint)res;
        }

        uint GetDHOffset2(byte[] handshake, int bufferoffset, uint len)
        {
            uint offset = 0;
            bufferoffset += 768;
            //assert(RTMP_SIG_SIZE <= len);

            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset];

            uint res = (offset % 632) + 8;

            if (res + 128 > 767)
            {
                Logger.Log(string.Format("Couldn't calculate correct DH offset (got {0}), exiting!", res));
                throw new Exception();
            }
            return res;
        }

        uint GetDigestOffset2(byte[] handshake, int bufferoffset, uint len)
        {
            uint offset = 0;
            bufferoffset += 772;
            //assert(12 <= len);

            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset];// (*ptr);

            uint res = (offset % 728) + 776;

            if (res + 32 > 1535)
            {
                Logger.Log(string.Format("Couldn't calculate correct digest offset (got {0}), exiting", res));
                throw new Exception();
            }
            return res;
        }

        void CalculateDigest(int digestPos, byte[] handshakeMessage, int handshakeOffset, byte[] key, int keyLen, byte[] digest, int digestOffset)
        {
            const int messageLen = RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH;
            byte[] message = new byte[messageLen];

            Array.Copy(handshakeMessage, handshakeOffset, message, 0, digestPos);
            Array.Copy(handshakeMessage, handshakeOffset + digestPos + SHA256_DIGEST_LENGTH, message, digestPos, messageLen - digestPos);

            HMACsha256(message, 0, messageLen, key, keyLen, digest, digestOffset);
        }

        bool VerifyDigest(int digestPos, byte[] handshakeMessage, byte[] key, int keyLen)
        {
            byte[] calcDigest = new byte[SHA256_DIGEST_LENGTH];

            CalculateDigest(digestPos, handshakeMessage, 0, key, keyLen, calcDigest, 0);

            for (int i = 0; i < SHA256_DIGEST_LENGTH; i++)
            {
                if (handshakeMessage[digestPos + i] != calcDigest[i]) return false;
            }
            return true;
        }

        void HMACsha256(byte[] message, int messageOffset, int messageLen, byte[] key, int keylen, byte[] digest, int digestOffset)
        {
            System.Security.Cryptography.HMAC hmac = System.Security.Cryptography.HMACSHA256.Create("HMACSHA256");
            byte[] actualKey = new byte[keylen]; Array.Copy(key, actualKey, keylen);
            hmac.Key = actualKey;

            byte[] actualMessage = new byte[messageLen];
            Array.Copy(message, messageOffset, actualMessage, 0, messageLen);

            byte[] calcDigest = hmac.ComputeHash(actualMessage);
            Array.Copy(calcDigest, 0, digest, digestOffset, calcDigest.Length);
        }

        void InitRC4Encryption(byte[] secretKey, byte[] pubKeyIn, int inOffset, byte[] pubKeyOut, int outOffset, out byte[] rc4keyIn, out byte[] rc4keyOut)
        {
            byte[] digest = new byte[SHA256_DIGEST_LENGTH];

            System.Security.Cryptography.HMAC hmac = System.Security.Cryptography.HMACSHA256.Create("HMACSHA256");
            hmac.Key = secretKey;

            byte[] actualpubKeyIn = new byte[128];
            Array.Copy(pubKeyIn, inOffset, actualpubKeyIn, 0, 128);
            digest = hmac.ComputeHash(actualpubKeyIn);

            rc4keyOut = new byte[16];
            Array.Copy(digest, rc4keyOut, 16);
            Logger.Log("RC4 Out Key: ");
            Logger.LogHex(rc4keyOut, 0, 16);

            hmac = System.Security.Cryptography.HMACSHA256.Create("HMACSHA256");
            hmac.Key = secretKey;

            byte[] actualpubKeyOut = new byte[128];
            Array.Copy(pubKeyOut, outOffset, actualpubKeyOut, 0, 128);
            digest = hmac.ComputeHash(actualpubKeyOut);

            rc4keyIn = new byte[16];
            Array.Copy(digest, rc4keyIn, 16);
            Logger.Log("RC4 In Key: ");
            Logger.LogHex(rc4keyIn, 0, 16);
        }

        /// <summary>
        /// RTMPE type 8 uses XTEA on the regular signature (http://en.wikipedia.org/wiki/XTEA)
        /// </summary>
        static void rtmpe8_sig(byte[] array, int offset, int keyid)
        {
            uint i, num_rounds = 32;
            uint v0, v1, sum = 0, delta = 0x9E3779B9;
            uint[] k;

            v0 = BitConverter.ToUInt32(array, offset);
            v1 = BitConverter.ToUInt32(array, offset + 4);

            k = rtmpe8_keys[keyid];

            for (i = 0; i < num_rounds; i++)
            {
                v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + k[sum & 3]);
                sum += delta;
                v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + k[(sum >> 11) & 3]);
            }

            Array.Copy(BitConverter.GetBytes(v0), 0, array, offset, 4);
            Array.Copy(BitConverter.GetBytes(v1), 0, array, offset + 4, 4);
        }

        const int BF_ROUNDS = 16;

        private struct bf_key
        {
            public uint[][] s;
            public uint[] p;
        }

        static uint BF_ENC(uint X, uint[][] S)
        {
            return (((S[0][X >> 24] + S[1][X >> 16 & 0xff]) ^ S[2][(X >> 8) & 0xff]) + S[3][X & 0xff]);
        }

        static void bf_enc(uint[] x, ref bf_key key)
        {
            uint Xl;
            uint Xr;
            uint temp;

            Xl = x[0];
            Xr = x[1];

            for (int i = 0; i < BF_ROUNDS; ++i)
            {
                Xl ^= key.p[i];
                Xr ^= BF_ENC(Xl, key.s);

                temp = Xl;
                Xl = Xr;
                Xr = temp;
            }

            Xl ^= key.p[BF_ROUNDS];
            Xr ^= key.p[BF_ROUNDS + 1];

            x[0] = Xr;
            x[1] = Xl;
        }

        static void bf_setkey(byte[] kp, out bf_key key)
        {
            uint[] d = new uint[2];

            key.p = new uint[BF_ROUNDS + 2];
            bf_pinit.CopyTo(key.p, 0);
            key.s = new uint[4][];
            for (int i = 0; i < 4; i++)
            {
                key.s[i] = new uint[256];
                bf_sinit[i].CopyTo(key.s[i], 0);
            }

            int j = 0;
            for (int i = 0; i < BF_ROUNDS + 2; ++i)
            {
                uint data = 0x00000000;
                for (int k = 0; k < 4; ++k)
                {
                    data = (data << 8) | kp[j];
                    j = j + 1;
                    if (j >= 24 /*keybytes*/)
                    {
                        j = 0;
                    }
                }
                key.p[i] ^= data;
            }

            d[0] = 0x00000000;
            d[1] = 0x00000000;

            for (int i = 0; i < BF_ROUNDS + 2; i += 2)
            {
                bf_enc(d, ref key);

                key.p[i] = d[0];
                key.p[i + 1] = d[1];
            }

            for (int i = 0; i < 4; ++i)
            {
                for (j = 0; j < 256; j += 2)
                {

                    bf_enc(d, ref key);

                    key.s[i][j] = d[0];
                    key.s[i][j + 1] = d[1];
                }
            }
        }

        /// <summary>
        /// RTMPE type 9 uses Blowfish on the regular signature ( http://en.wikipedia.org/wiki/Blowfish_(cipher) )
        /// </summary>
        static void rtmpe9_sig(byte[] array, int offset, int keyid)
        {
            Logger.Log("rtmp type 9");
            uint[] d = new uint[2];
            d[0] = BitConverter.ToUInt32(array, offset);
            d[1] = BitConverter.ToUInt32(array, offset + 4);

            bf_key key;

            bf_setkey(rtmpe9_keys[keyid], out key);

            /* input is little-endian * / 
            d[0] = in[0] | (in[1] << 8) | (in[2] << 16) | (in[3] << 24); 
            d[1] = in[4] | (in[5] << 8) | (in[6] << 16) | (in[7] << 24);
             */
            bf_enc(d, ref key);

            Array.Copy(BitConverter.GetBytes(d[0]), 0, array, offset, 4);
            Array.Copy(BitConverter.GetBytes(d[1]), 0, array, offset + 4, 4);
        }

        #endregion

        int ReadN(byte[] buffer, int offset, int size)
        {
            // keep reading until wanted amount has been received or timeout after nothing has been received is elapsed
            byte[] data = new byte[size];
            int readThisRun = 0;
            int i = receiveTimeoutMS / 100;
            while (readThisRun < size)
            {
                int read = tcpSocket.Receive(data, readThisRun, size - readThisRun, SocketFlags.None);

                // decrypt if needed
                if (read > 0)
                {
                    if (Link.rc4In != null)
                    {
                        Link.rc4In.ProcessBytes(data, readThisRun, read, buffer, offset + readThisRun);
                    }
                    else
                    {
                        Array.Copy(data, readThisRun, buffer, offset + readThisRun, read);
                    }

                    readThisRun += read;

                    bytesReadTotal += read;

                    if (bytesReadTotal > lastSentBytesRead + (m_nClientBW / 2)) SendBytesReceived(); // report bytes read

                    i = receiveTimeoutMS / 100; // we just got some data, reset the receive timeout
                }
                else
                {
                    i--;
                    System.Threading.Thread.Sleep(100);
                    if (i <= 0) return readThisRun;
                }
            }

            return readThisRun;
        }

        void WriteN(byte[] buffer, int offset, int size)
        {
            // encrypt if needed
            if (Link.rc4Out != null)
            {
                byte[] result = new byte[size];
                Link.rc4Out.ProcessBytes(buffer, offset, size, result, 0);
                tcpSocket.Send(result);
            }
            else
            {
                tcpSocket.Send(buffer, offset, size, SocketFlags.None);
            }
        }
    }
}
