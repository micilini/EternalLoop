namespace EternalLoop.Core.Diagnostics;

public static class CorruptFileBackup
{
    public static string? TryCreate(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            string backupPath = filePath + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
            File.Copy(filePath, backupPath, overwrite: false);
            return backupPath;
        }
        catch
        {
            return null;
        }
    }
}
