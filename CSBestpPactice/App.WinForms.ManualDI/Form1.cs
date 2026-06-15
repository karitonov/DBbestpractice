using CSBestpPactice.Domain.Entities;
using CSBestpPactice.Infrastructure.Repositories.DataTables;
using CSBestpPactice.Service;
using System.Data;

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
            Reload();
        }

        private void Reload()
        {
            dgvProducts.DataSource = _service.GetAll().ToList();
            dgvProductsTable.DataSource = _tableRepository.GetAll();
            dgvProductsTable.Columns["Id"].ReadOnly = true;
        }

        private Product? GetSelectedProduct()
        {
            return dgvProducts.CurrentRow?.DataBoundItem as Product;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new ProductEditForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                var product = form.GetProduct();
                _service.Register(product);
                Reload();
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var product = GetSelectedProduct();
            if (product is null)
            {
                MessageBox.Show("編集する商品を選択してください。");
                return;
            }

            using var form = new ProductEditForm(product);
            if (form.ShowDialog() == DialogResult.OK)
            {
                var updatedProduct = form.GetProduct();
                _service.Update(updatedProduct);
                Reload();
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var product = GetSelectedProduct();
            if (product is null)
            {
                MessageBox.Show("削除する商品を選択してください。");
                return;
            }

            var result = MessageBox.Show(
                $"「{product.Name}」を削除しますか？",
                "確認",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.OK)
            {
                _service.Delete(product.Id);
                Reload();
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            var table = (DataTable)dgvProductsTable.DataSource;

            foreach (DataRow row in table.Rows)
            {
                if (row.RowState is not DataRowState.Added and not DataRowState.Modified) continue;

                if (row["Name"] is DBNull || string.IsNullOrWhiteSpace(row["Name"]?.ToString()))
                {
                    MessageBox.Show("商品名を入力してください。", "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (row["UnitPrice"] is DBNull)
                {
                    MessageBox.Show("単価を入力してください。", "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            int count = _tableRepository.Update(table);
            MessageBox.Show($"{count} 件の変更を保存しました。");
            Reload();
        }
    }
}
