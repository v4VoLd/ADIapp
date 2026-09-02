using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ADIapp.Helpers
{
    /// <summary>
    /// Provides cross-platform, hardware-bound AES-256-GCM encrypted persistent token storage.
    /// Tokens are encrypted with a key derived from the machine's unique hardware hash + application salt,
    /// preventing unauthorized token extraction, tampering, or copying across machines/VMs.
    /// </summary>
    public static class SecureStorageHelper
    {
        private static readonly string SessionDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ADIapp"
        );

        private static readonly string SessionFilePath = Path.Combine(SessionDirectory, "session.dat");

        // Fixed application salt combined with machine hardware hash
        private static readonly byte[] AppSalt = Encoding.UTF8.GetBytes("ADIapp_Secured_Desktop_Session_Salt_2026");

        public static void SaveToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ClearToken();
                return;
            }

            try
            {
                Directory.CreateDirectory(SessionDirectory);

                byte[] plaintextBytes = Encoding.UTF8.GetBytes(token);
                byte[] key = DeriveMachineKey();

                // 96-bit (12 bytes) standard AES-GCM nonce
                byte[] nonce = new byte[12];
                RandomNumberGenerator.Fill(nonce);

                // 128-bit (16 bytes) authentication tag
                byte[] tag = new byte[16];
                byte[] ciphertext = new byte[plaintextBytes.Length];

                using (var aesGcm = new AesGcm(key, 16))
                {
                    aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
                }

                // File format: [Nonce (12B)] [Tag (16B)] [Ciphertext (NB)]
                using var fs = new FileStream(SessionFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                using var bw = new BinaryWriter(fs);
                bw.Write(nonce.Length);
                bw.Write(nonce);
                bw.Write(tag.Length);
                bw.Write(tag);
                bw.Write(ciphertext.Length);
                bw.Write(ciphertext);
            }
            catch (Exception ex)
            {
                Logger.Error($"[SecureStorageHelper] Failed to save encrypted token: {ex.Message}", ex);
            }
        }

        public static string? LoadToken()
        {
            if (!File.Exists(SessionFilePath))
            {
                return null;
            }

            try
            {
                using var fs = new FileStream(SessionFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var br = new BinaryReader(fs);

                int nonceLen = br.ReadInt32();
                if (nonceLen != 12) return null;
                byte[] nonce = br.ReadBytes(nonceLen);

                int tagLen = br.ReadInt32();
                if (tagLen != 16) return null;
                byte[] tag = br.ReadBytes(tagLen);

                int cipherLen = br.ReadInt32();
                if (cipherLen <= 0 || cipherLen > 1024 * 64) return null;
                byte[] ciphertext = br.ReadBytes(cipherLen);

                byte[] key = DeriveMachineKey();
                byte[] decryptedBytes = new byte[ciphertext.Length];

                using (var aesGcm = new AesGcm(key, 16))
                {
                    aesGcm.Decrypt(nonce, ciphertext, tag, decryptedBytes);
                }

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (CryptographicException)
            {
                Logger.Warning("[SecureStorageHelper] Session decryption failed (hardware signature mismatch or corrupted data). Clearing session.");
                ClearToken();
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"[SecureStorageHelper] Unexpected error loading session token: {ex.Message}", ex);
                ClearToken();
                return null;
            }
        }

        public static void ClearToken()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    File.Delete(SessionFilePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SecureStorageHelper] Failed to clear session file: {ex.Message}", ex);
            }
        }

        public static bool HasSavedSession()
        {
            return File.Exists(SessionFilePath);
        }

        private static byte[] DeriveMachineKey()
        {
            string hwHash = HardwareHelper.GetHardwareHash();
            if (string.IsNullOrEmpty(hwHash))
            {
                hwHash = "ADIAPP-GENERIC-FALLBACK-HASH";
            }

            // Derive a 256-bit (32 bytes) encryption key using HKDF-SHA256
            byte[] ikm = Encoding.UTF8.GetBytes(hwHash);
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 32, AppSalt, Encoding.UTF8.GetBytes("ADIapp_Session_Key"));
        }
    }
}
