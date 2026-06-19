using CSBestpPactice.Domain.Repositories;
using CSBestpPactice.Infrastructure.Data.Factories;
using CSBestpPactice.Infrastructure.Data.Sessions;
using CSBestpPactice.Infrastructure.Repositories.AdoNet;
using CSBestpPactice.Infrastructure.Repositories.DataTables;
using CSBestpPactice.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.WinForms.DIContainer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IDbConnectionFactory>(_ =>
                new SqliteConnectionFactory(configuration.GetConnectionString("SQLite")!));
            services.AddSingleton<IDbSession>(sp =>
                new DbSession(sp.GetRequiredService<IDbConnectionFactory>().CreateConnection()));
            services.AddTransient<IProductRepository, ProductRepository>();
            services.AddTransient<IProductTableRepository, ProductTableRepository>();
            services.AddTransient<IProductService, ProductService>();
            services.AddTransient<IProductTableService, ProductTableService>();
            services.AddTransient<Form1>();

            using var provider = services.BuildServiceProvider();
            Application.Run(provider.GetRequiredService<Form1>());
        }
    }
}
