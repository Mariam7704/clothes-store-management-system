using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace POSales
{
    public partial class Discount : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();        
        string stitle = "Point Of Sales";
        Cashier cashier;
        public Discount(Cashier cash)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            cashier = cash;            
            txtDiscount.Focus();
            this.KeyPreview = true;
            LoadCustomers();
        }

        private void LoadCustomers()
        {
            try
            {
                cn.Open();
                cm = new SqlCommand("SELECT id, name, phone, email FROM tbCustomer ORDER BY name", cn);
                SqlDataAdapter da = new SqlDataAdapter(cm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                
                // Clear existing rows
                dgvCustomers.Rows.Clear();
                
                // Add data to the grid
                foreach (DataRow row in dt.Rows)
                {
                    dgvCustomers.Rows.Add(
                        row["id"].ToString(),
                        row["name"].ToString(),
                        row["phone"].ToString(),
                        row["email"].ToString()
                    );
                }
                
                cn.Close();
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle);
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try
            {
                cn.Open();
                cm = new SqlCommand("SELECT id, name, phone, email FROM tbCustomer WHERE name LIKE @search OR phone LIKE @search OR email LIKE @search ORDER BY name", cn);
                cm.Parameters.AddWithValue("@search", "%" + txtSearch.Text + "%");
                SqlDataAdapter da = new SqlDataAdapter(cm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvCustomers.DataSource = dt;
                cn.Close();
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle);
            }
        }

        private void btnDeleteCustomer_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvCustomers.SelectedRows.Count > 0)
                {
                    if (MessageBox.Show("Are you sure you want to delete this customer?", "Delete Customer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        string customerId = dgvCustomers.SelectedRows[0].Cells[0].Value.ToString();
                        cn.Open();
                        cm = new SqlCommand("DELETE FROM tbCustomer WHERE id = @id", cn);
                        cm.Parameters.AddWithValue("@id", customerId);
                        cm.ExecuteNonQuery();
                        cn.Close();
                        LoadCustomers();
                        MessageBox.Show("Customer has been successfully deleted", "Delete Customer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a customer to delete", stitle);
                }
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle);
            }
        }

        private void picClose_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void Discount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) this.Dispose();
            else if (e.KeyCode == Keys.Enter) btnSave.PerformClick();
        }

        private void txtDiscount_TextChanged(object sender, EventArgs e)
        {
            try
            {
                // Only calculate discount if manually entered (not from customer selection)
                if (string.IsNullOrEmpty(txtDiscount.Text))
                {
                    txtDiscAmount.Text = "0.00";
                    return;
                }

                double totalPrice = double.Parse(txtTotalPrice.Text);
                double discountPercent = double.Parse(txtDiscount.Text);
                double disc = totalPrice * (discountPercent * 0.01);
                txtDiscAmount.Text = disc.ToString("#,##0.00");
            }
            catch (Exception)
            {
                txtDiscAmount.Text = "0.00";
            }
        }

        private void btnAddCustomer_Click(object sender, EventArgs e)
        {
            CustomerEntry customerEntry = new CustomerEntry();
            if (customerEntry.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    cn.Open();
                    cm = new SqlCommand("INSERT INTO tbCustomer (name, phone, email) VALUES (@name, @phone, @email)", cn);
                    cm.Parameters.AddWithValue("@name", customerEntry.CustomerName);
                    cm.Parameters.AddWithValue("@phone", customerEntry.Phone);
                    cm.Parameters.AddWithValue("@email", customerEntry.Email);
                    cm.ExecuteNonQuery();
                    cn.Close();
                    LoadCustomers();
                }
                catch (Exception ex)
                {
                    cn.Close();
                    MessageBox.Show(ex.Message, stitle);
                }
            }
        }

        private void btnSelectCustomer_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvCustomers.SelectedRows.Count > 0)
                {
                    string customerId = dgvCustomers.SelectedRows[0].Cells[0].Value.ToString();
                    
                    // Update tbCart with customer_id
                    cn.Open();
                    cm = new SqlCommand("UPDATE tbCart SET customer_id = @customer_id WHERE transno = @transno", cn);
                    cm.Parameters.AddWithValue("@customer_id", customerId);
                    cm.Parameters.AddWithValue("@transno", cashier.lblTranNo.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();

                    // Apply 10% discount for existing customers
                    cn.Open();
                    cm = new SqlCommand("UPDATE tbCart SET disc_percent = 10 WHERE transno = @transno", cn);
                    cm.Parameters.AddWithValue("@transno", cashier.lblTranNo.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();

                    cashier.LoadCart();
                    this.Dispose();
                }
                else
                {
                    MessageBox.Show("Please select a customer", stitle);
                }
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Add discount? Click yes to confirm", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    cm = new SqlCommand("UPDATE tbCart SET disc_percent=@disc_percent WHERE transno = @transno", cn);                    
                    cm.Parameters.AddWithValue("@disc_percent", double.Parse(txtDiscount.Text));
                    cm.Parameters.AddWithValue("@transno", cashier.lblTranNo.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();
                    cashier.LoadCart();
                    this.Dispose();
                }
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle);
            }
        }


    }

    public class CustomerEntry : Form
    {
        private TextBox txtName;
        private TextBox txtPhone;
        private TextBox txtEmail;
        private Button btnOK;
        private Button btnCancel;
        private Label lblName;
        private Label lblPhone;
        private Label lblEmail;

        public string CustomerName { get; private set; }
        public string Phone { get; private set; }
        public string Email { get; private set; }

        public CustomerEntry()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.txtName = new System.Windows.Forms.TextBox();
            this.txtPhone = new System.Windows.Forms.TextBox();
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblName = new System.Windows.Forms.Label();
            this.lblPhone = new System.Windows.Forms.Label();
            this.lblEmail = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 12);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(100, 20);
            this.lblName.Text = "Customer Name:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(12, 35);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(260, 24);
            this.txtName.TabIndex = 0;
            // 
            // lblPhone
            // 
            this.lblPhone.AutoSize = true;
            this.lblPhone.Location = new System.Drawing.Point(12, 65);
            this.lblPhone.Name = "lblPhone";
            this.lblPhone.Size = new System.Drawing.Size(100, 20);
            this.lblPhone.Text = "Phone Number:";
            // 
            // txtPhone
            // 
            this.txtPhone.Location = new System.Drawing.Point(12, 88);
            this.txtPhone.Name = "txtPhone";
            this.txtPhone.Size = new System.Drawing.Size(260, 24);
            this.txtPhone.TabIndex = 1;
            // 
            // lblEmail
            // 
            this.lblEmail.AutoSize = true;
            this.lblEmail.Location = new System.Drawing.Point(12, 118);
            this.lblEmail.Name = "lblEmail";
            this.lblEmail.Size = new System.Drawing.Size(100, 20);
            this.lblEmail.Text = "Email Address:";
            // 
            // txtEmail
            // 
            this.txtEmail.Location = new System.Drawing.Point(12, 141);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Size = new System.Drawing.Size(260, 24);
            this.txtEmail.TabIndex = 2;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(12, 171);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(125, 30);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(147, 171);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(125, 30);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // CustomerEntry
            // 
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(284, 213);
            this.Controls.Add(this.lblEmail);
            this.Controls.Add(this.lblPhone);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.txtPhone);
            this.Controls.Add(this.txtName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CustomerEntry";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add New Customer";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter customer name", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CustomerName = txtName.Text;
            Phone = txtPhone.Text;
            Email = txtEmail.Text;
            DialogResult = DialogResult.OK;
        }
    }
}
