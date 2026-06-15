using CSBestpPactice.Infrastructure.Repositories.DataTables;
using CSBestpPactice.Service;

namespace App.WinForms.ManualDI
{
    public partial class Form1 : Form
    {
        private readonly IProductService _service;
        private readonly IProductTableRepository _tableRepository;
        public Form1(IProductService service, IProductTableRepository tableRepository)
        {
            InitializeComponent();
            _service = service;
            _tableRepository = tableRepository;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var products = _service.GetAll().ToList();
            dgvProducts.DataSource = products;

            var productsTable = _tableRepository.GetAll();
            dgvProductsTable.DataSource = productsTable;
        }
    }
}
