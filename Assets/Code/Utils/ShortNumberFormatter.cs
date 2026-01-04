using System;

public static class ShortNumberFormatter
{
	public static string FormatShortNumber(float value)
	{
		// Keep values below 1000 as full numbers; thousands -> k, millions -> m, billions -> b, trillions -> t
		long absValue = Math.Abs((long)value);
		if (absValue < 1000)
		{
			return value.ToString();
		}

		string[] suffixes = new[] { "k", "m", "b", "t" };
		double d = absValue;
		int suffixIndex = -1;

		while (d >= 1000.0 && suffixIndex < suffixes.Length - 1)
		{
			d /= 1000.0;
			suffixIndex++;
		}

		// Round to one decimal place
		double rounded = Math.Round(d * 10.0) / 10.0;

		// Handle case where rounding bumps value to next suffix (e.g., 999.95 -> 1000.0)
		if (rounded >= 1000.0 && suffixIndex < suffixes.Length - 1)
		{
			rounded /= 1000.0;
			suffixIndex++;
		}

		long intPart = (long)rounded;
		int fracPart = (int)Math.Round((rounded - intPart) * 10.0);

		string numberPart = fracPart == 0 ? intPart.ToString() : intPart + "." + fracPart.ToString();

		string result = (value < 0 ? "-" : string.Empty) + numberPart + suffixes[suffixIndex];
		return result;
	}
}