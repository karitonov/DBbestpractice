using CSBestpPactice.Domain.Entities;

namespace App.WinForms.ManualDI
{
    public partial class ProductEditForm : Form
    {
        private readonly Guid _id;

        public ProductEditForm(Product? product = null)
        {
            InitializeComponent();

            if (product is not null)
            {
                _id = product.Id;
                txtName.Text = product.Name;
                txtDescription.Text = product.Description ?? string.Empty;
                nudUnitPrice.Value = product.UnitPrice;
                chkIsFeatured.Checked = product.IsFeatured;
                Text = "商品編集";
            }
            else
            {
                _id = Guid.NewGuid();
                Text = "商品追加";
            }
        }

        public Product GetProduct() => new Product
        {
            Id          = _id,
            Name        = txtName.Text.Trim(),
            Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim(),
            UnitPrice   = nudUnitPrice.Value,
            IsFeatured  = chkIsFeatured.Checked,
        };

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("商品名を入力してください。", "入力エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
