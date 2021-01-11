using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TexellCheckInMobile.Utils
{
    public static class CryptographyExtensions
    {
        public enum HashType
        {
            Md5,
            Sha1,
            Sha256,
            Sha384,
            Sha512
        }


        /// <summary>
        /// 	Calculates the MD5 hash for the given string.
        /// </summary>
        /// <returns>A 32 char long hash.</returns>
        public static string GetHashMd5(this string input)
        {
            return ComputeHash(HashType.Md5, input);
        }

        /// <summary>
        /// 	Calculates the SHA-1 hash for the given string.
        /// </summary>
        /// <returns>A 40 char long hash.</returns>
        public static string GetHashSha1(this string input)
        {
            return ComputeHash(HashType.Sha1, input);
        }

        /// <summary>
        /// 	Calculates the SHA-256 hash for the given string.
        /// </summary>
        /// <returns>A 64 char long hash.</returns>
        public static string GetHashSha256(this string input)
        {
            return ComputeHash(HashType.Sha256, input);
        }

        /// <summary>
        /// 	Calculates the SHA-384 hash for the given string.
        /// </summary>
        /// <returns>A 96 char long hash.</returns>
        public static string GetHashSha384(this string input)
        {
            return ComputeHash(HashType.Sha384, input);
        }

        /// <summary>
        /// 	Calculates the SHA-512 hash for the given string.
        /// </summary>
        /// <returns>A 128 char long hash.</returns>
        public static string GetHashSha512(this string input)
        {
            return ComputeHash(HashType.Sha512, input);
        }

        public static string ComputeHash(HashType hashType, string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            var hasher = GetHasher(hashType);
            var inputBytes = Encoding.UTF8.GetBytes(input);

            var hashBytes = hasher.ComputeHash(inputBytes);
            var hash = new StringBuilder();
            foreach (var b in hashBytes)
            {
                hash.Append(string.Format("{0:x2}", b));
            }

            return hash.ToString();
        }

        private static HashAlgorithm GetHasher(HashType hashType)
        {
            switch (hashType)
            {
                case HashType.Md5:
                    return new MD5CryptoServiceProvider();
                case HashType.Sha1:
                    return new SHA1Managed();
                case HashType.Sha256:
                    return new SHA256Managed();
                case HashType.Sha384:
                    return new SHA384Managed();
                case HashType.Sha512:
                    return new SHA512Managed();
                default:
                    throw new ArgumentOutOfRangeException("hashType");
            }
        }
        public static string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }

        // AES 256 key

        private const string AesIV256 = @"DT$UjU6ZobeHks2F";
        private const string AesKey256 = @"v8qCQthQVL6&$q@@7Gy435SyeL5Fmb7Y";


        public static string Encrypt256(this string text)
        {
            // AesCryptoServiceProvider
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            aes.BlockSize = 128;
            aes.KeySize = 256;
            aes.IV = Encoding.UTF8.GetBytes(AesIV256);
            aes.Key = Encoding.UTF8.GetBytes(AesKey256);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Convert string to byte array
            byte[] src = Encoding.Unicode.GetBytes(text);

            // encryption
            using (ICryptoTransform encrypt = aes.CreateEncryptor())
            {
                byte[] dest = encrypt.TransformFinalBlock(src, 0, src.Length);

                // Convert byte array to Base64 strings
                return Convert.ToBase64String(dest);
            }
        }
    }
   
}
