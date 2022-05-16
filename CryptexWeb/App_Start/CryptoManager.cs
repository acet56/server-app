using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace CryptexWeb
{
    public class CryptoManager : IDisposable
    {
        public CryptoManager(IdentityFactoryOptions<CryptoManager> options, IOwinContext context)
        {
        }

        public static CryptoManager Create(IdentityFactoryOptions<CryptoManager> options, IOwinContext context)
        {
            return new CryptoManager(options, context);
        }

        public static RsaKeyParameters loadPublicKey()
        {
            string pub = ConfigurationManager.AppSettings["ServerPublicKey"];
            return (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(pub));
        }

        public static RsaKeyParameters loadPrivateKey()
        {
            string pk = ConfigurationManager.AppSettings["ServerPrivateKey"];
            return (RsaKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(pk));
        }

        //Deszyfrowanie has³a uzytkownika
      public string DecryptUserPassword(string encryptedPassword)
      {
        if (string.IsNullOrEmpty(encryptedPassword))
          return null;

        var privateKey = loadPrivateKey();

        byte[] bytesToDecrypt = Convert.FromBase64String(encryptedPassword);


        var decryptEngine = new OaepEncoding(new RsaEngine(), new Sha256Digest());

        decryptEngine.Init(false, privateKey);

        var blockSize = decryptEngine.GetInputBlockSize();
        var result = new List<byte>();
        for (var i = 0; i < 0 + bytesToDecrypt.Length; i += blockSize)
        {
          var currentSize = Math.Min(blockSize, bytesToDecrypt.Length - i);
          result.AddRange(decryptEngine.ProcessBlock(bytesToDecrypt, i, currentSize));
        }
        var decrypted = Encoding.UTF8.GetString(result.ToArray());
        return decrypted;
      }

        public static AsymmetricCipherKeyPair GenerateKeyPair()
        {
            using (RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider(1024))
            {
                RSAParameters rsaKeyInfo = rsaProvider.ExportParameters(true);

                var asymmetricCipherKeyPair = DotNetUtilities.GetRsaKeyPair(rsaKeyInfo);


                return asymmetricCipherKeyPair;
            }
        }


        public string RsaDecryptWithPrivate(string base64Input, AsymmetricKeyParameter privateKey)
        {
            byte[] bytesToDecrypt = Convert.FromBase64String(base64Input);

//            AsymmetricCipherKeyPair keyPair;
//            var decryptEngine = new Pkcs1Encoding(new RsaEngine());
            var decryptEngine = new OaepEncoding(new RsaEngine(), new Sha256Digest());
//
//            using (var txtreader = new StringReader(privateKey))
//            {
//                keyPair = (AsymmetricCipherKeyPair)new PemReader(txtreader).ReadObject();
//
//            }
                decryptEngine.Init(false, privateKey);

            var blockSize = decryptEngine.GetInputBlockSize();
            var result = new List<byte>();
            for (var i = 0; i < 0 + bytesToDecrypt.Length; i += blockSize)
            {
                var currentSize = Math.Min(blockSize, bytesToDecrypt.Length - i);
                result.AddRange(decryptEngine.ProcessBlock(bytesToDecrypt, i, currentSize));
            }
            var decrypted = Encoding.UTF8.GetString(result.ToArray());
            return decrypted;
        }

        public string RsaDecryptWithPublic(string base64Input, AsymmetricKeyParameter publicKey)
        {
            var bytesToDecrypt = Convert.FromBase64String(base64Input);

            var decryptEngine = new OaepEncoding(new RsaEngine());

//            using (var txtreader = new StringReader(publicKey))
//            {
//                var keyParameter = (AsymmetricKeyParameter)new PemReader(txtreader).ReadObject();
//
//            }
            decryptEngine.Init(false, publicKey);

            var decrypted = Encoding.UTF8.GetString(decryptEngine.ProcessBlock(bytesToDecrypt, 0, bytesToDecrypt.Length));
            return decrypted;
        }


        public string RsaEncryptWithPublic(string clearText, AsymmetricKeyParameter publicKey)
        {
            var bytesToEncrypt = Encoding.UTF8.GetBytes(clearText);

            var encryptEngine = new OaepEncoding(new RsaEngine(), new Sha256Digest());
            encryptEngine.Init(true, publicKey);
            var blockSize = encryptEngine.GetInputBlockSize();
            var result = new List<byte>();
            for (var i = 0; i <  bytesToEncrypt.Length; i += blockSize)
            {
                var currentSize = Math.Min(blockSize, bytesToEncrypt.Length - i);
                result.AddRange(encryptEngine.ProcessBlock(bytesToEncrypt, i, currentSize));
            }
            var encrypted = Convert.ToBase64String(result.ToArray());
            return encrypted;

        }

        public string RsaEncryptWithPrivate(string clearText, AsymmetricKeyParameter privateKey)
        {
            var bytesToEncrypt = Encoding.UTF8.GetBytes(clearText);

            var encryptEngine = new OaepEncoding(new RsaEngine());

            encryptEngine.Init(true, privateKey);
            var encrypted = Convert.ToBase64String(encryptEngine.ProcessBlock(bytesToEncrypt, 0, bytesToEncrypt.Length));
            return encrypted;
        }











        public void Dispose()
        {
        }

        public static string ByteArrayToHex(byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];
            int b;
            for (int i = 0; i < bytes.Length; i++)
            {
                b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }
            return new string(c);
        }

    }
}