using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNetCoreFeatureManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(
                    webBuilder =>
                        {
                            webBuilder
                                //.ConfigureAppConfiguration(
                                //    config =>
                                //        {
                                //            var settings = config.Build();

                                //            config.AddAzureAppConfiguration(
                                //                options =>
                                //                    {
                                //                        options.Connect(settings["ConnectionStrings:AppConfig"]).UseFeatureFlags(
                                //                            featureFlagOptions =>
                                //                                {
                                //                                    featureFlagOptions.CacheExpirationInterval = TimeSpan.FromMinutes(5);
                                //                                });
                                //                    });
                                //        })
                                .UseStartup<Startup>();
                        });
    }
}
