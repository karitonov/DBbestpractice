using CSBestpPactice.Service;

namespace App.WinForms.ManualDI
{
    public partial class Form1 : Form
    {
        private readonly IProductService _service;
        public Form1(IProductService service)
        {
            InitializeComponent();
            _service = service;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var products = _service.GetAll().ToList();
            dgvProducts.DataSource = products;
        }
    }
}
