using Microsoft.AspNetCore;

namespace Nightfall.ResourceServer;

public static class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    private static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        return WebHost.CreateDefaultBuilder(args).ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddFile("logs/nf-{Date}.log");
        }).UseStartup<Startup>();
    }
}