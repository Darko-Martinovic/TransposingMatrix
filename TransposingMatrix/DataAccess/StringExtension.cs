
namespace TransposingMatrix
{
    public static class StringExtensions
    {
        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null) return true;
            for (var i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                    return false;
            }
            return true;
        }
    }
}
