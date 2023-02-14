using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OptionsExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Open the appsettings file from here to make changes: " + Directory.GetCurrentDirectory());
            Console.WriteLine();

            var configuation = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services
                .AddOptions()
                .AddSingleton<IConfiguration>(configuation) // adding configuration to DI so that I can use it the "setup" of options
                .AddSingleton<MetricsOptionsConfigurationManager>()
                .AddSingleton<IConfigureOptions<MetricsOptions>>(sp => sp.GetRequiredService<MetricsOptionsConfigurationManager>())
                .AddSingleton<IValidateOptions<MetricsOptions>>(sp => sp.GetRequiredService<MetricsOptionsConfigurationManager>())
                .AddSingleton<IPostConfigureOptions<MetricsOptions>>(sp => sp.GetRequiredService<MetricsOptionsConfigurationManager>())
                .AddSingleton<IOptionsChangeTokenSource<MetricsOptions>>(new ConfigurationChangeTokenSource<MetricsOptions>(configuation));

            var serviceResolver = services.BuildServiceProvider();
            while (true)
            {
                var metricsOptions = serviceResolver.GetService<IOptionsMonitor<MetricsOptions>>().CurrentValue;
                Console.WriteLine(metricsOptions.ApplicationVersion + ", " + metricsOptions.HostName);
                Thread.Sleep(1 * 1000);
            }
        }
    }

    class MetricsOptionsConfigurationManager :
        IConfigureOptions<MetricsOptions>,
        IValidateOptions<MetricsOptions>,
        IPostConfigureOptions<MetricsOptions>
    {
        private readonly IConfiguration _appConfig;

        public MetricsOptionsConfigurationManager(IConfiguration configuration) // inject configuration from DI
        {
            Console.WriteLine("ConfigureMetricsOptions constructor is called");

            _appConfig = configuration;
        }

        public void Configure(MetricsOptions options)
        {
            Console.WriteLine("ConfigureMetricsOptions.Configure method is called");

            options.ApplicationVersion = _appConfig["ApplicationVersion"];
            options.HostName = _appConfig["Metrics:HostName"];
        }

        public void PostConfigure(string name, MetricsOptions options)
        {
            Console.WriteLine("ConfigureMetricsOptions.PostConfigure method is called on name:" + name);
        }

        public ValidateOptionsResult Validate(string name, MetricsOptions options)
        {
            Console.WriteLine("ConfigureMetricsOptions.Validate method is called on name: " + name);

            if (options.ApplicationVersion == "1.1")
            {
                return ValidateOptionsResult.Fail("Application version 1.1 is not allowed");
            }

            if (options.HostName == "10.10.10.11")
            {
                return ValidateOptionsResult.Fail("HostName 10.10.10.11 is not allowed");
            }

            return ValidateOptionsResult.Success;
        }
    }

    class MetricsOptions : IOptions<MetricsOptions>
    {
        public MetricsOptions()
        {
            Console.WriteLine("MetricsOptions constructor is called");
        }

        public string ApplicationVersion { get; set; }

        public string HostName { get; set; }

        public MetricsOptions Value => this;
    }
}
