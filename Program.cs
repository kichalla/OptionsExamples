namespace OptionsExample;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Open the appsettings file from here to make changes: " + Directory.GetCurrentDirectory());
        Console.WriteLine();

        Scenario1.Run();
    }
}
