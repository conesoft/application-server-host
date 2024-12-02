namespace Conesoft.Server_Host.Helpers;

static class StringSplitHelpers
{
    public static string[] SplitExceptQuotes(this string input, string splitter)
    {
        List<string> splits = [];
        bool insideQuotes = false;
        var currentStart = 0;

        for (int i = 0; i < input.Length; i++)
        {
            if (i < currentStart)
            {
                continue;
            }
            if (input[i] == '\"')
            {
                insideQuotes = !insideQuotes;
                continue;
            }
            if (insideQuotes)
            {
                continue;
            }
            if (input[i..].StartsWith(splitter))
            {
                splits.Add(input[currentStart..i]);
                currentStart = i + splitter.Length;
            }
        }

        splits.Add(input[currentStart..]);

        return splits.Where(s => !string.IsNullOrEmpty(s)).ToArray();
    }
}