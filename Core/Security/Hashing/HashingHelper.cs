// Decompiled with JetBrains decompiler
// Type: NArchitecture.Core.Security.Hashing.HashingHelper
// Assembly: Core.Security, Version=1.3.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 74E12A4E-4974-4F10-BF25-9BCA9028BE85
// Assembly location: /Users/hgoksal/.nuget/packages/narchitecture.core.security/1.3.1/lib/net8.0/Core.Security.dll

#nullable enable
using System.Security.Cryptography;
using System.Text;

namespace Core.Security.Hashing;

public static class HashingHelper
{
    public static void CreatePasswordHash(
        string password,
        out byte[] passwordHash,
        out byte[] passwordSalt)
    {
        using (HMACSHA512 hmacshA512 = new HMACSHA512())
        {
            passwordSalt = hmacshA512.Key;
            passwordHash = hmacshA512.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }

    public static bool VerifyPasswordHash(
        string password,
        byte[] passwordHash,
        byte[] passwordSalt)
    {
        using (HMACSHA512 hmacshA512 = new HMACSHA512(passwordSalt))
            return hmacshA512.ComputeHash(Encoding.UTF8.GetBytes(password)).SequenceEqual(passwordHash);
    }
        
    public static string DecryptAes(string value, string key)
    {
        try
        {
            var decodedValue = Convert.FromBase64String(Uri.UnescapeDataString(value));

            using (var aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.Mode = CipherMode.CFB;

                var iv = new byte[aesAlg.BlockSize / 8];
                var cipherText = new byte[decodedValue.Length - iv.Length];

                Buffer.BlockCopy(decodedValue, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(decodedValue, iv.Length, cipherText, 0, cipherText.Length);

                aesAlg.IV = iv;

                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch 
        {
            return string.Empty;
        }

    }

    public static string ComputeMd5(string input)
    {
        using (var md5 = MD5.Create())
        {
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
        
    public static string EncryptAes(string value, string key)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.Mode = CipherMode.CFB;

            var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(value);
                    }
                }

                var iv = aesAlg.IV;
                var encryptedText = msEncrypt.ToArray();

                var result = new byte[iv.Length + encryptedText.Length];
                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(encryptedText, 0, result, iv.Length, encryptedText.Length);

                var encodedResult = Uri.EscapeDataString(Convert.ToBase64String(result));

                return encodedResult;
            }
        }
    }

}