namespace App.WinForms.HostDI
{
    partial class ProductEditForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            tblMain = new TableLayoutPanel();
            lblName = new Label();
            txtName = new TextBox();
            lblDescription = new Label();
            txtDescription = new TextBox();
            lblUnitPrice = new Label();
            nudUnitPrice = new NumericUpDown();
            chkIsFeatured = new CheckBox();
            flpButtons = new FlowLayoutPanel();
            btnOk = new Button();
            btnCancel = new Button();
            tblMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudUnitPrice).BeginInit();
            flpButtons.SuspendLayout();
            SuspendLayout();

            // tblMain
            tblMain.ColumnCount = 2;
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMain.Controls.Add(lblName,        0, 0);
            tblMain.Controls.Add(txtName,        1, 0);
            tblMain.Controls.Add(lblDescription, 0, 1);
            tblMain.Controls.Add(txtDescription, 1, 1);
            tblMain.Controls.Add(lblUnitPrice,   0, 2);
            tblMain.Controls.Add(nudUnitPrice,   1, 2);
            tblMain.Controls.Add(chkIsFeatured,  1, 3);
            tblMain.Controls.Add(flpButtons,     0, 4);
            tblMain.SetColumnSpan(flpButtons, 2);
            tblMain.Dock = DockStyle.Fill;
            tblMain.Padding = new Padding(8);
            tblMain.RowCount = 5;
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            // lblName
            lblName.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            lblName.Text = "商品名";
            lblName.TextAlign = ContentAlignment.MiddleRight;
            lblName.Padding = new Padding(0, 0, 4, 0);

            // txtName
            txtName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtName.MaxLength = 200;

            // lblDescription
            lblDescription.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            lblDescription.Text = "説明";
            lblDescription.TextAlign = ContentAlignment.MiddleRight;
            lblDescription.Padding = new Padding(0, 0, 4, 0);

            // txtDescription
            txtDescription.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtDescription.MaxLength = 500;

            // lblUnitPrice
            lblUnitPrice.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            lblUnitPrice.Text = "単価";
            lblUnitPrice.TextAlign = ContentAlignment.MiddleRight;
            lblUnitPrice.Padding = new Padding(0, 0, 4, 0);

            // nudUnitPrice
            nudUnitPrice.Anchor = AnchorStyles.Left;
            nudUnitPrice.DecimalPlaces = 2;
            nudUnitPrice.Increment = new decimal(new int[] { 1, 0, 0, 131072 });   // 0.01
            nudUnitPrice.Maximum = new decimal(new int[] { 99999999, 0, 0, 131072 }); // 999999.99
            nudUnitPrice.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            nudUnitPrice.Size = new Size(130, 23);

            // chkIsFeatured
            chkIsFeatured.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            chkIsFeatured.Text = "おすすめ";
            chkIsFeatured.TextAlign = ContentAlignment.MiddleLeft;

            // flpButtons
            flpButtons.Dock = DockStyle.Fill;
            flpButtons.FlowDirection = FlowDirection.RightToLeft;
            flpButtons.WrapContents = false;
            flpButtons.Controls.Add(btnCancel);
            flpButtons.Controls.Add(btnOk);

            // btnOk
            btnOk.Size = new Size(80, 26);
            btnOk.Text = "OK";
            btnOk.Click += btnOk_Click;

            // btnCancel
            btnCancel.Size = new Size(90, 26);
            btnCancel.Text = "キャンセル";
            btnCancel.DialogResult = DialogResult.Cancel;

            // Form
            AcceptButton = btnOk;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(360, 196);
            Controls.Add(tblMain);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "商品追加";

            tblMain.ResumeLayout(false);
            tblMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudUnitPrice).EndInit();
            flpButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tblMain;
        private Label lblName;
        private TextBox txtName;
        private Label lblDescription;
        private TextBox txtDescription;
        private Label lblUnitPrice;
        private NumericUpDown nudUnitPrice;
        private CheckBox chkIsFeatured;
        private FlowLayoutPanel flpButtons;
        private Button btnOk;
        private Button btnCancel;
    }
}
