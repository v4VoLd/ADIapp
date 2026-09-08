using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

namespace ADIapp.Helpers
{
    /// <summary>
    /// Provides secure, cross-platform encrypted persistent token storage.
    /// On Windows, uses native DPAPI (ProtectedData) bound to CurrentUser.
    /// On macOS / Linux, uses AES-256-GCM hardware-derived key.
    /// </summary>
    public static class SecureStorageHelper
    {
        private static readonly string SessionDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ADIapp"
        );

        private static readonly string SessionFilePath = Path.Combine(SessionDirectory, "session.dat");
        private static readonly string RememberedEmailPath = Path.Combine(SessionDirectory, "user.dat");

        // Fixed application salt combined with machine hardware hash
        private static readonly byte[] AppSalt = Encoding.UTF8.GetBytes("ADIapp_Secured_Desktop_Session_Salt_2026");

        private const byte FormatDpapi = 0x01;
        private const byte FormatAesGcm = 0x02;

        public static void SaveRememberedEmail(string email)
        {
            try
            {
                Directory.CreateDirectory(SessionDirectory);
                if (string.IsNullOrWhiteSpace(email))
                {
                    if (File.Exists(RememberedEmailPath)) File.Delete(RememberedEmailPath);
                }
                else
                {
                    File.WriteAllText(RememberedEmailPath, email.Trim(), Encoding.UTF8);
                }
            }
            catch { }
        }

        public static string? GetRememberedEmail()
        {
            try
            {
                if (File.Exists(RememberedEmailPath))
                {
                    return File.ReadAllText(RememberedEmailPath, Encoding.UTF8).Trim();
                }
            }
            catch { }
            return null;
        }

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

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        byte[] encrypted = ProtectedData.Protect(plaintextBytes, AppSalt, DataProtectionScope.CurrentUser);
                        using var fs = new FileStream(SessionFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        using var bw = new BinaryWriter(fs);
                        bw.Write(FormatDpapi);
                        bw.Write(encrypted.Length);
                        bw.Write(encrypted);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"[SecureStorageHelper] Windows DPAPI protect failed, falling back to AES-GCM: {ex.Message}");
                    }
                }

                // AES-GCM for macOS/Linux or fallback
                byte[] key = DeriveMachineKey();
                byte[] nonce = new byte[12];
                RandomNumberGenerator.Fill(nonce);
                byte[] tag = new byte[16];
                byte[] ciphertext = new byte[plaintextBytes.Length];

                using (var aesGcm = new AesGcm(key, 16))
                {
                    aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
                }

                using var fsFallback = new FileStream(SessionFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                using var bwFallback = new BinaryWriter(fsFallback);
                bwFallback.Write(FormatAesGcm);
                bwFallback.Write(nonce.Length);
                bwFallback.Write(nonce);
                bwFallback.Write(tag.Length);
                bwFallback.Write(tag);
                bwFallback.Write(ciphertext.Length);
                bwFallback.Write(ciphertext);
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
                byte[] fileBytes = File.ReadAllBytes(SessionFilePath);
                if (fileBytes.Length < 4) return null;

                using var ms = new MemoryStream(fileBytes);
                using var br = new BinaryReader(ms);

                byte firstByte = fileBytes[0];

                if (firstByte == FormatDpapi && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    br.ReadByte(); // skip format byte
                    int len = br.ReadInt32();
                    if (len <= 0 || len > fileBytes.Length) return null;
                    byte[] encrypted = br.ReadBytes(len);
                    byte[] decrypted = ProtectedData.Unprotect(encrypted, AppSalt, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decrypted);
                }

                if (firstByte == FormatAesGcm)
                {
                    br.ReadByte(); // skip format byte
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

                // Legacy format fallback (no format prefix byte; starts with nonceLen = 12 as 32-bit int)
                int legacyNonceLen = BitConverter.ToInt32(fileBytes, 0);
                if (legacyNonceLen == 12)
                {
                    ms.Position = 0;
                    int nonceLen = br.ReadInt32();
                    byte[] nonce = br.ReadBytes(nonceLen);
                    int tagLen = br.ReadInt32();
                    byte[] tag = br.ReadBytes(tagLen);
                    int cipherLen = br.ReadInt32();
                    byte[] ciphertext = br.ReadBytes(cipherLen);

                    byte[] key = DeriveMachineKey();
                    byte[] decryptedBytes = new byte[ciphertext.Length];

                    using (var aesGcm = new AesGcm(key, 16))
                    {
                        aesGcm.Decrypt(nonce, ciphertext, tag, decryptedBytes);
                    }

                    return Encoding.UTF8.GetString(decryptedBytes);
                }

                return null;
            }
            catch (CryptographicException ex)
            {
                Logger.Warning($"[SecureStorageHelper] Session decryption failed ({ex.Message}). Clearing session.");
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
