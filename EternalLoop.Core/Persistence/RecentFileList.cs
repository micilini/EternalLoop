namespace EternalLoop.Core.Persistence;

public static class RecentFileList
{
    public const int MaxItems = 10;

    public static List<string> Add(string filePath, IEnumerable<string>? current)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var result = new List<string> { filePath };

        if (current is not null)
        {
            foreach (var item in current)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                if (StringComparer.OrdinalIgnoreCase.Equals(item, filePath))
                {
                    continue;
                }

                result.Add(item);
            }
        }

        return Normalize(result);
    }

    public static List<string> Normalize(IEnumerable<string>? current)
    {
        if (current is null)
        {
            return [];
        }

        return current
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxItems)
            .ToList();
    }
}
