using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SaveLoadData // Functions that saving/loading data called from many locations.
{
    public static string savePath;
    public static string blueprintSavePath;
    private static byte[] encryptionKey;
    public static string steamId;
    public static string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = encryptionKey;  // Directly use the raw 32-byte key
            aes.IV = new byte[16];    // Set a fixed IV for simplicity (use a random IV in production)

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                return Convert.ToBase64String(encryptedBytes);  // Base64 for the encrypted text (not the key)
            }
        }
    }

    public static string Decrypt(string encryptedText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = encryptionKey; // Use the raw 32-byte key
            aes.IV = new byte[16];    // Same IV used during encryption

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText); // Convert encrypted data back
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }
    public static void SetUserEncryptionKey() // call only once.
    {
        steamId = SteamUser.GetSteamID().ToString(); // Get Steam ID as string

        CheckCreateDirectories();

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(steamId));
            byte[] keyBytes = new byte[32]; // AES-256 requires exactly 32 bytes
            Array.Copy(hashBytes, keyBytes, 32); // Take only the first 32 bytes
            encryptionKey = keyBytes; // Return raw 32-byte key
        }
    }

    private static void CheckCreateDirectories()
    {
        savePath = Application.persistentDataPath + "/data/";
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        savePath = Application.persistentDataPath + "/data/" + steamId + "/";
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        blueprintSavePath = savePath + "/blueprints/";
        if (!Directory.Exists(blueprintSavePath))
        {
            Directory.CreateDirectory(blueprintSavePath);
        }
        savePath = Application.persistentDataPath + "/data/" + steamId + "/" + "save.dat";
    }
}
