using CSBestpPactice.Infrastructure.Data.Factories;
using CSBestpPactice.Infrastructure.Data.Sessions;
using AdoNet = CSBestpPactice.Infrastructure.Repositories.AdoNet;
using DapperRepo = CSBestpPactice.Infrastructure.Repositories.Dapper;
using EfCore = CSBestpPactice.Infrastructure.Repositories.EfCore;
using CSBestpPactice.Service;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace App.WinForms.ManualDI
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

            // Dapper用
            SqlMapper.AddTypeHandler(new DapperRepo.GuidTypeHandler());
            SqlMapper.AddTypeHandler(new DapperRepo.DecimalTypeHandler());

            var connectionString = ConfigurationManager
                .ConnectionStrings["SQLite"].ConnectionString;

            var factory = new SqliteConnectionFactory(connectionString);
            var session = new DbSession(factory.CreateConnection());
            //var repository = new AdoNet.ProductRepository(session);
            //var repository = new DapperRepo.ProductRepository(factory);
            var options = new DbContextOptionsBuilder<EfCore.AppDbContext>()
                .UseSqlite(connectionString)
                .Options;
            var context = new EfCore.AppDbContext(options);
            var repository = new EfCore.ProductRepository(context);
            var tableRepository = new AdoNet.ProductTableRepository(session);
            var service = new ProductService(repository);
            var tableService = new ProductTableService(tableRepository);

            Application.Run(new Form1(service, tableService));
        }
    }
}