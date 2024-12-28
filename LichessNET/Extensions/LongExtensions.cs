namespace LichessNET.Extensions;

public static class LongExtensions
{
    private static readonly string[] Prefixes = { "", "k", "M", "G", "T", "P" };

    public static string ToSIPrefix(this long number)
    {
        if (number == 0) return "0";

        int exponent = (int)Math.Floor(Math.Log10(Math.Abs(number)) / 3);
        double scaledNumber = number / Math.Pow(1000, exponent);

        if (exponent < 0 || exponent >= Prefixes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(number),
                "Number is too large or too small to format with SI prefixes.");
        }

        return $"{scaledNumber:0.##} {Prefixes[exponent]}";
    }
}