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

namespace App.WinForms.HostMinimal
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

            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = AppContext.BaseDirectory,
            });

            var connectionString = builder.Configuration.GetConnectionString("SQLite")!;

            builder.Services.AddSingleton<IDbConnectionFactory>(_ =>
                new SqliteConnectionFactory(connectionString));
            builder.Services.AddSingleton<IDbSession>(sp =>
                new DbSession(sp.GetRequiredService<IDbConnectionFactory>().CreateConnection()));

            //// Ado.NET, Dapper, EF Core repository registrations are mutually exclusive, choose one of them to register
            //builder.Services.AddTransient<IProductRepository, AdoNet.ProductRepository>();
            //builder.Services.AddTransient<IProductRepository, DapperRepo.ProductRepository>();
            builder.Services.AddDbContext<EfCore.AppDbContext>(
                options => options.UseSqlite(connectionString),
                ServiceLifetime.Singleton);
            builder.Services.AddTransient<IProductRepository, EfCore.ProductRepository>();

            builder.Services.AddTransient<IProductTableRepository, ProductTableRepository>();
            builder.Services.AddTransient<IProductService, ProductService>();
            builder.Services.AddTransient<IProductTableService, ProductTableService>();

            builder.Services.AddSingleton<Form1>();

            using var host = builder.Build();
            Application.Run(host.Services.GetRequiredService<Form1>());
        }
    }
}