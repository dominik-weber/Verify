[MemoryDiagnoser(false)]
public class ProjectDirectoryFinderBenchmarks
{
    static string directory;

    static ProjectDirectoryFinderBenchmarks()
    {
        var solutionDirectory = SolutionDirectoryFinder.Find(Environment.CurrentDirectory);
        directory = Path.Combine(solutionDirectory, @"Verify.Tests\VerifyDirectoryTests.WithDirectory\nested.with.dot");
    }
    public int BirthYear {
        get => field;
        set {
            if (value < 0) // Validation code
                throw new ArgumentOutOfRangeException("Age cannot be negative.");
            field = value;
        }
    }
    [Benchmark]
    public string FindProjectDirectory() =>
        ProjectDirectoryFinder.Find(directory);
}