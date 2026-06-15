namespace EternalLoop.BranchAnalysis.Tests.Parity;

public static class SolutionRootLocator
{
    public static string Locate()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "EternalLoop.BranchAnalysis.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate EternalLoop.BranchAnalysis.slnx.");
    }
}
