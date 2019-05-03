using System;

namespace WaveFunctionCollapse
{
    
    public class Pattern : IEquatable<Pattern>
    {
        public int idx;

        public byte[] bytes;

        public Pattern(byte[] patternBytes)
        {
            this.bytes = patternBytes;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash = hash * 31 + bytes[i];
            }
            return hash;
        }

        public bool Equals(Pattern other)
        {
            if (bytes.Length != other.bytes.Length)
            {
                return false;
            }
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != other.bytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object other)
        {
            if (!(other is Pattern))
            {
                return false;
            }
            return Equals((Pattern)other);
        }
    }
}