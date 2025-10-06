using System;
using System.Security.Cryptography;
using System.Text;

namespace QuickTableProyect.Aplicacion
{
    public class CryptoService
    {
        private readonly byte[] key = Encoding.UTF8.GetBytes("QuickTable2024SecureKey123456!");

        public string EncriptarUID(string uidPlano)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();

                    var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    var plainTextBytes = Encoding.UTF8.GetBytes(uidPlano);

                    using (var msEncrypt = new System.IO.MemoryStream())
                    {
                        // Escribir IV al inicio
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(plainTextBytes, 0, plainTextBytes.Length);
                            csEncrypt.FlushFinalBlock();
                        }

                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch
            {
                // Si falla la encriptación, devolver el UID tal como está
                return uidPlano;
            }
        }

        public string DesencriptarUID(string uidEncriptado)
        {
            try
            {
                var cipherBytes = Convert.FromBase64String(uidEncriptado);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;

                    // Extraer IV (primeros 16 bytes)
                    var iv = new byte[16];
                    Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    // Los datos encriptados están después del IV
                    var cipherData = new byte[cipherBytes.Length - 16];
                    Array.Copy(cipherBytes, 16, cipherData, 0, cipherData.Length);

                    using (var msDecrypt = new System.IO.MemoryStream(cipherData))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var reader = new System.IO.StreamReader(csDecrypt))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                // Si falla la desencriptación, devolver el UID tal como está
                return uidEncriptado;
            }
        }
    }
}
//puto git