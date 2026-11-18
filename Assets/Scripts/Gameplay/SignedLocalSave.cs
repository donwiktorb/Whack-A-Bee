
using System;
using System.Security.Cryptography;
using System.Text;

public static class SignedLocalSave
{
    private const string DemoSecret =
        "WhackABee-Exercise7-HMAC-Demo-Key-2026-LocalSave";

    public static string Sign(string userId, string json)
    {
        byte[] signature = Calculate(userId, json);

        return Convert.ToBase64String(signature) + "|" + json;
    }

    public static bool TryVerify(
        string userId,
        string storedValue,
        out string json)
    {
        json = null;

        if (string.IsNullOrEmpty(storedValue))
            return false;

        int separator = storedValue.IndexOf('|');

        if (separator <= 0 || separator == storedValue.Length - 1)
            return false;

        string signatureText = storedValue.Substring(0, separator);
        string content = storedValue.Substring(separator + 1);

        byte[] receivedSignature;

        try
        {
            receivedSignature = Convert.FromBase64String(
                signatureText
            );
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] expectedSignature = Calculate(userId, content);

        if (receivedSignature.Length != expectedSignature.Length)
            return false;

        int difference = 0;

        for (int i = 0; i < expectedSignature.Length; i++)
            difference |= receivedSignature[i] ^ expectedSignature[i];

        if (difference != 0)
            return false;

        json = content;
        return true;
    }

    private static byte[] Calculate(string userId, string json)
    {
        byte[] secret = Encoding.UTF8.GetBytes(DemoSecret);

        
        byte[] content = Encoding.UTF8.GetBytes(
            userId + "\n" + json
        );

        using (HMACSHA256 hmac = new HMACSHA256(secret))
        {
            return hmac.ComputeHash(content);
        }
    }
}
