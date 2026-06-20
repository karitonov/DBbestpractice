using CSBestpPactice.Domain.Repositories;
using CSBestpPactice.Infrastructure.Data.Factories;
using CSBestpPactice.Infrastructure.Data.Sessions;
using CSBestpPactice.Infrastructure.Repositories.AdoNet;
using CSBestpPactice.Service;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AdoNet = CSBestpPactice.Infrastructure.Repositories.AdoNet;
using DapperRepo = CSBestpPactice.Infrastructure.Repositories.Dapper;
using EfCore = CSBestpPactice.Infrastructure.Repositories.EfCore;

namespace App.WinForms.HostDI
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

            SqlMapper.AddTypeHandler(new DapperRepo.GuidTypeHandler());
            SqlMapper.AddTypeHandler(new DapperRepo.DecimalTypeHandler());

            using IHost host = Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    var connectionString = context.Configuration.GetConnectionString("SQLite")!;

                    services.AddSingleton<IDbConnectionFactory>(_ =>
                        new SqliteConnectionFactory(connectionString));
                    services.AddSingleton<IDbSession>(sp =>
                        new DbSession(sp.GetRequiredService<IDbConnectionFactory>().CreateConnection()));

                    // Ado.NET, Dapper, EF Core repository registrations are mutually exclusive, choose one of them to register
                    //services.AddTransient<IProductRepository, AdoNet.ProductRepository>();
                    //services.AddTransient<IProductRepository, DapperRepo.ProductRepository>();
                    services.AddDbContext<EfCore.AppDbContext>(
                        options => options.UseSqlite(connectionString),
                        ServiceLifetime.Singleton);
                    services.AddTransient<IProductRepository, EfCore.ProductRepository>();

                    services.AddTransient<IProductTableRepository, ProductTableRepository>();
                    services.AddTransient<IProductService, ProductService>();
                    services.AddTransient<IProductTableService, ProductTableService>();

                    services.AddSingleton<Form1>();
                })
                .Build();

            Application.Run(host.Services.GetRequiredService<Form1>());
        }
    }
}