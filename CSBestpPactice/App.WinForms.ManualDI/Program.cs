using CSBestpPactice.Infrastructure.Data.Factories;
using CSBestpPactice.Infrastructure.Data.Sessions;
using CSBestpPactice.Infrastructure.Repositories.AdoNet;
using CSBestpPactice.Infrastructure.Repositories.DataTables;
using CSBestpPactice.Service;
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

            var connectionString = ConfigurationManager
                .ConnectionStrings["SQLite"].ConnectionString;

            var factory = new SqliteConnectionFactory(connectionString);
            var session = new DbSession(factory.CreateConnection());
            var repository = new ProductRepository(session);
            var tableRepository = new ProductTableRepository(session);
            var service = new ProductService(repository);
            var tableService = new ProductTableService(tableRepository);

            Application.Run(new Form1(service, tableService));
        }
    }
}