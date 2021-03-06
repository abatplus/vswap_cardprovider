using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CardExchangeService
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
      CreateHostBuilder(args, configuration).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
              var configuredPort = configuration["PORT_TCP_API"];
              webBuilder.UseStartup<Startup>();
              if (!string.IsNullOrEmpty(configuredPort) && int.TryParse(configuredPort, out var port))
              {
                webBuilder.UseUrls("http://0.0.0.0:" + port);
              }
            });
  }
}
