namespace AwPropdump;

using System.Text;

public static class Program {
    static Program()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static void Main(string[] args)
    {
        var test = new PropdumpParser(File.OpenRead("/Projects/AwPropdump/epsilon.txt"));

        var objects = test.ReadObjects().ToList();

        Console.WriteLine($"Objects parsed: {objects.Count}");
    }
}
