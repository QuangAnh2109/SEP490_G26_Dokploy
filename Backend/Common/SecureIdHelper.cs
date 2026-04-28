using Eleven41.Skip32;

namespace Backend.Common
{
    public static class SecureIdHelper
    {
        private static readonly byte[] Key = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x01, 0x23 };
        private static readonly Skip32Cipher Cipher = new Skip32Cipher(Key);

        public static string EncryptId(int id)
        {
            int encryptedInt = Cipher.Encrypt(id);
            return encryptedInt.ToString("x8");
        }

        public static int? DecryptId(string hashedId)
        {
            if (string.IsNullOrEmpty(hashedId)) return null;
            try
            {
                int encryptedInt = Convert.ToInt32(hashedId, 16);
                return Cipher.Decrypt(encryptedInt);
            }
            catch { return null; }
        }
    }
}