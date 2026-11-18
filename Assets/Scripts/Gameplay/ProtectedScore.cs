
using UnityEngine;

public sealed class ProtectedScore
{
    private int key;
    private int encodedValue;
    private uint checksum;

    public ProtectedScore()
    {
        Set(0);
    }

    public void Set(int value)
    {
        key = Random.Range(1, int.MaxValue);

        encodedValue = value ^ key;
        checksum = CalculateChecksum(value, key);
    }

    public bool IsValid(int expectedValue)
    {
        int decoded = encodedValue ^ key;

        return decoded == expectedValue &&
               checksum == CalculateChecksum(decoded, key);
    }

    private static uint CalculateChecksum(int value, int mask)
    {
        unchecked
        {
            uint hash = (uint)value * 0x9E3779B1u;
            hash ^= (uint)mask * 0x85EBCA6Bu;
            hash ^= hash >> 16;
            hash *= 0xC2B2AE35u;
            return hash ^ (hash >> 13);
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void SimulateTampering()
    {
        encodedValue ^= 16;
    }
#endif
}
