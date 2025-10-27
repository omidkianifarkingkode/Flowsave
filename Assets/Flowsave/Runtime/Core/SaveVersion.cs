using System;

namespace FlowSave
{
    /// <summary>
    /// Represents the semantic version of a save payload.
    /// </summary>
    [Serializable]
    public readonly struct SaveVersion : IEquatable<SaveVersion>, IComparable<SaveVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public static SaveVersion Zero => new SaveVersion(0, 0, 0);

        public SaveVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public int CompareTo(SaveVersion other)
        {
            if (Major != other.Major)
            {
                return Major.CompareTo(other.Major);
            }

            if (Minor != other.Minor)
            {
                return Minor.CompareTo(other.Minor);
            }

            return Patch.CompareTo(other.Patch);
        }

        public bool Equals(SaveVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }

        public override bool Equals(object obj)
        {
            return obj is SaveVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Major;
                hash = (hash * 397) ^ Minor;
                hash = (hash * 397) ^ Patch;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }

        public static bool operator ==(SaveVersion left, SaveVersion right) => left.Equals(right);

        public static bool operator !=(SaveVersion left, SaveVersion right) => !left.Equals(right);

        public static bool operator <(SaveVersion left, SaveVersion right) => left.CompareTo(right) < 0;

        public static bool operator >(SaveVersion left, SaveVersion right) => left.CompareTo(right) > 0;

        public static bool operator <=(SaveVersion left, SaveVersion right) => left.CompareTo(right) <= 0;

        public static bool operator >=(SaveVersion left, SaveVersion right) => left.CompareTo(right) >= 0;
    }
}
