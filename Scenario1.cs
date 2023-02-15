using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace OptionsExample;

internal static class Scenario1
{
    public static void Run()
    {
        var configuation = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        services.AddMetricsServices(configuation);
        services.Configure<MetricsOptions>(options =>
        {
            Console.WriteLine("Inside Configure<TOptions> lamda....");
        });
        var serviceResolver = services.BuildServiceProvider();

        while (true)
        {
            // Validation of options is done when resolving this service
            var metricsOptions = serviceResolver.GetRequiredService<IOptionsMonitor<MetricsOptions>>().CurrentValue;
            Console.WriteLine(metricsOptions.ApplicationVersion + ", " + metricsOptions.HostName);
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }

    // Extension method that might live in a shared library
    static IServiceCollection AddMetricsServices(this IServiceCollection services, IConfiguration configuation)
    {
        // adding configuration to DI so that I can use it the "setup" of options
        services.TryAddSingleton<IConfiguration>(configuation);
        services
            .AddOptions()
            .AddSingleton<MetricsOptionsConfigurationManager>()
            .AddSingleton<IConfigureOptions<MetricsOptions>>(sp => sp.GetRequiredService<MetricsOptionsConfigurationManager>())
            .AddSingleton<IValidateOptions<MetricsOptions>>(sp => sp.GetRequiredService<MetricsOptionsConfigurationManager>())
            .AddSingleton<IPostConfigureOptions<MetricsOptions>>(sp => sp.GetRequiredService<MetricsOptionsConfigurationManager>())
            .AddSingleton<IOptionsChangeTokenSource<MetricsOptions>>(new ConfigurationChangeTokenSource<MetricsOptions>(configuation));
        return services;
    }

    class MetricsOptionsConfigurationManager :
        IConfigureOptions<MetricsOptions>,
        IValidateOptions<MetricsOptions>,
        IPostConfigureOptions<MetricsOptions>
    {
        private readonly IConfiguration _appConfig;

        // inject configuration from DI
        public MetricsOptionsConfigurationManager(IConfiguration configuration)
        {
            Console.WriteLine("MetricsOptionsConfigurationManager constructor is called");

            _appConfig = configuration;
        }

        public void Configure(MetricsOptions options)
        {
            Console.WriteLine("MetricsOptionsConfigurationManager.Configure method is called");

            // Options author has the freedom to look within a particular section or beyond it
            // All of this is well and good as long as we adhere to a contract on the structure of
            // the configuraiton
            options.ApplicationVersion = _appConfig["ApplicationVersion"];
            options.HostName = _appConfig["Metrics:HostName"];
        }

        public void PostConfigure(string name, MetricsOptions options)
        {
            Console.WriteLine("MetricsOptionsConfigurationManager.PostConfigure method is called");
        }

        public ValidateOptionsResult Validate(string name, MetricsOptions options)
        {
            Console.WriteLine("MetricsOptionsConfigurationManager.Validate method is called");

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
