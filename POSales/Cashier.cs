﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using DarrenLee.Media;

namespace POSales
{
    public partial class Cashier : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;

        int qty;
        string id;
        string price;

        Camera captureDevice = new Camera();
            
        string stitle = "Point Of Sales";
        public Cashier()
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            GetTranNo();
            lblDate.Text = DateTime.Now.ToShortDateString();
        }

        private void picClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Exit Application?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        public void slide(Button button)
        {
            panelSlide.BackColor = Color.White;
            panelSlide.Height = button.Height;
            panelSlide.Top = button.Top;
        }
        #region button
        private void btnNTran_Click(object sender, EventArgs e)
        {
            slide(btnNTran);
            GetTranNo();

        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            slide(btnSearch);
            LookUpProduct lookUp = new LookUpProduct(this);
            lookUp.LoadProduct();
            lookUp.ShowDialog();
        }

        private void btnDiscount_Click(object sender, EventArgs e)
        {
            slide(btnDiscount);
            Discount discount = new Discount(this);
            discount.lbId.Text = id;
            discount.txtTotalPrice.Text = price;
            discount.ShowDialog();            
        }

        private void btnSettle_Click(object sender, EventArgs e)
        {
            slide(btnSettle);
            Settle settle = new Settle(this);
            settle.txtSale.Text = lblDisplayTotal.Text;
            settle.ShowDialog();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            slide(btnClear);
            if (MessageBox.Show("Remove all items from cart?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                cn.Open();
                cm = new SqlCommand("Delete from tbCart where transno like'" + lblTranNo.Text + "'", cn);
                cm.ExecuteNonQuery();
                cn.Close();
                MessageBox.Show("All items has been successfully remove", "Remove item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCart();
            }
        }


        private void btnLogout_Click(object sender, EventArgs e)
        {
            slide(btnLogout);
            if(dgvCash.Rows.Count > 0)
            {
                MessageBox.Show("Unable to logout. Please cancel the transaction.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Logout Application?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Hide();
                Login login = new Login();
                login.Show();
                this.Close();
            }
        }
        #endregion button

        public void LoadCart()
        {
            try
            {
                Boolean hascart = false;
                Boolean hasCustomer = false;
                int i = 0;
                double total = 0;
                double discount = 0;
                dgvCash.Rows.Clear();
                cn.Open();
                cm = new SqlCommand("SELECT c.id, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total, c.customer_id FROM tbCart AS c INNER JOIN tbProduct AS p ON c.pcode=p.pcode WHERE c.transno LIKE @transno and c.status LIKE 'Pending'", cn);
                cm.Parameters.AddWithValue("@transno", lblTranNo.Text);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    double itemTotal = Convert.ToDouble(dr["price"].ToString()) * Convert.ToDouble(dr["qty"].ToString());
                    total += itemTotal;
                    discount += Convert.ToDouble(dr["disc"].ToString());
                    dgvCash.Rows.Add(i, dr["id"].ToString(), dr["pcode"].ToString(), dr["pdesc"].ToString(), dr["price"].ToString(), dr["qty"].ToString(), dr["disc"].ToString(), itemTotal.ToString("#,##0.00"));
                    hascart = true;
                    // Check if customer is selected
                    if (dr["customer_id"] != DBNull.Value && !string.IsNullOrEmpty(dr["customer_id"].ToString()))
                    {
                        hasCustomer = true;
                    }
                }
                dr.Close();
                cn.Close();
                lblSaleTotal.Text = total.ToString("#,##0.00");
                lblDiscount.Text = discount.ToString("#,##0.00");
                GetCartTotal();
                if (hascart) 
                { 
                    btnClear.Enabled = true; 
                    btnDiscount.Enabled = true;
                    btnSettle.Enabled = hasCustomer; // Enable settle only if customer is selected
                }
                else 
                { 
                    btnClear.Enabled = false; 
                    btnSettle.Enabled = false; 
                    btnDiscount.Enabled = false; 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle);
            }
        }

        public void GetCartTotal()
        {
            double discount = double.Parse(lblDiscount.Text);
            double sales = double.Parse(lblSaleTotal.Text);

            lblDisplayTotal.Text = (sales - discount).ToString("#,##0.00");
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            lblTimer.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        public void GetTranNo()
        {
            try
            {
                string sdate = DateTime.Now.ToString("yyyyMMdd");
                int count;
                string transno;
                cn.Open();
                cm = new SqlCommand("SELECT TOP 1 transno FROM tbCart WHERE transno LIKE '" + sdate + "%' ORDER BY id desc", cn);
                dr = cm.ExecuteReader();
                dr.Read();
                if (dr.HasRows)
                {
                    transno = dr[0].ToString();
                    count = int.Parse(transno.Substring(8, 4));
                    lblTranNo.Text = sdate + (count + 1);
                }
                else
                {
                    transno = sdate + "1001";
                    lblTranNo.Text = transno;
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {

                cn.Close();
                MessageBox.Show(ex.Message, stitle);

            }
            
        }

        private void txtBarcode_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtBarcode.Text == string.Empty) return;
                else
                {
                    string _pcode;
                    double _price;
                    int _qty;
                    cn.Open();
                    cm = new SqlCommand("SELECT * FROM tbProduct WHERE barcode LIKE '" + txtBarcode.Text + "'", cn);
                    dr = cm.ExecuteReader();
                    dr.Read();
                    if(dr.HasRows)
                    {
                        qty = int.Parse(dr["qty"].ToString());
                        _pcode = dr["pcode"].ToString();
                        _price = double.Parse(dr["price"].ToString());
                        _qty = int.Parse(txtQty.Text);
                       
                        dr.Close();
                        cn.Close();
                        //insert to tbCart
                        AddToCart(_pcode, _price, _qty);
                    }
                    dr.Close();
                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void AddToCart(string _pcode, double _price,int _qty)
        {
            try
            {
                string id = "";
                int cart_qty = 0;
                bool found = false;
                cn.Open();
                cm = new SqlCommand("Select * from tbCart Where transno = @transno and pcode = @pcode", cn);
                cm.Parameters.AddWithValue("@transno", lblTranNo.Text);
                cm.Parameters.AddWithValue("@pcode", _pcode);
                dr = cm.ExecuteReader();
                dr.Read();
                if (dr.HasRows)
                {
                    id = dr["id"].ToString();
                    cart_qty = int.Parse(dr["qty"].ToString());
                    found = true;
                }
                else found = false;
                dr.Close();
                cn.Close();

                if (found)
                {
                    if (qty < (int.Parse(txtQty.Text) + cart_qty))
                    {
                        MessageBox.Show("Unable to procced. Remaining quantity on hand is " + qty, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    cn.Open();
                    cm = new SqlCommand("Update tbCart set qty = (qty + " + _qty + ")Where id= '" + id + "'", cn);
                    cm.ExecuteReader();
                    cn.Close();
                    txtBarcode.SelectionStart = 0;
                    txtBarcode.SelectionLength = txtBarcode.Text.Length;
                    LoadCart();                    
                }
                else
                {
                    if (qty < (int.Parse(txtQty.Text) + cart_qty))
                    {
                        MessageBox.Show("Unable to procced. Remaining qty on hand is" + qty, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    cn.Open();
                    cm = new SqlCommand("INSERT INTO tbCart(transno, pcode, price, qty, sdate, cashier)VALUES(@transno, @pcode, @price, @qty, @sdate, @cashier)", cn);
                    cm.Parameters.AddWithValue("@transno", lblTranNo.Text);
                    cm.Parameters.AddWithValue("@pcode", _pcode);
                    cm.Parameters.AddWithValue("@price", _price);
                    cm.Parameters.AddWithValue("@qty", _qty);
                    cm.Parameters.AddWithValue("@sdate", DateTime.Now);
                    cm.Parameters.AddWithValue("@cashier", lblUsername.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();
                    LoadCart();
                }
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message, stitle);
            }
        }

        private void dgvCash_SelectionChanged(object sender, EventArgs e)
        {
            int i = dgvCash.CurrentRow.Index;
            id = dgvCash[1, i].Value.ToString();
            price = dgvCash[7, i].Value.ToString();
        }

        private void dgvCash_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            string colName = dgvCash.Columns[e.ColumnIndex].Name;
           

            if (colName == "Delete")
            {
                if (MessageBox.Show("Remove this item", "Remove item", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    dbcon.ExecuteQuery("Delete from tbCart where id like'" + dgvCash.Rows[e.RowIndex].Cells[1].Value.ToString() + "'");
                    MessageBox.Show("Items has been successfully remove", "Remove item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCart();
                }
            }
            else if (colName == "colAdd")
            {
                int i = 0;
                cn.Open();
                cm = new SqlCommand("SELECT SUM(qty) as qty FROM tbProduct WHERE pcode LIKE'" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "' GROUP BY pcode", cn);
                i = int.Parse(cm.ExecuteScalar().ToString());
                cn.Close();
                if (int.Parse(dgvCash.Rows[e.RowIndex].Cells[5].Value.ToString()) < i)
                {
                    dbcon.ExecuteQuery("UPDATE tbCart SET qty = qty + " + int.Parse(txtQty.Text) + " WHERE transno LIKE '" + lblTranNo.Text + "'  AND pcode LIKE '" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "'");
                    LoadCart();
                }
                else
                {
                    MessageBox.Show("Remaining qty on hand is " + i + "!", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (colName == "colReduce")
            {
                int currentQty = int.Parse(dgvCash.Rows[e.RowIndex].Cells[5].Value.ToString());
                int reduceAmount = int.Parse(txtQty.Text);
                
                // Check if reduce amount is valid
                if (reduceAmount <= 0)
                {
                    MessageBox.Show("Please enter a valid quantity to reduce", "Invalid Quantity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if reducing would result in 0 or negative quantity
                if (currentQty <= reduceAmount)
                {
                    MessageBox.Show("Cannot reduce quantity below 1", "Invalid Operation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get stock on hand
                cn.Open();
                cm = new SqlCommand("SELECT qty FROM tbProduct WHERE pcode LIKE @pcode", cn);
                cm.Parameters.AddWithValue("@pcode", dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString());
                int stockOnHand = int.Parse(cm.ExecuteScalar().ToString());
                cn.Close();

                // Check if reduced quantity would exceed stock on hand
                if ((currentQty - reduceAmount) > stockOnHand)
                {
                    MessageBox.Show($"Cannot reduce quantity. Stock on hand is {stockOnHand}", "Invalid Operation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Proceed with reduction
                dbcon.ExecuteQuery("UPDATE tbCart SET qty = qty - " + reduceAmount + " WHERE transno LIKE '" + lblTranNo.Text + "' AND pcode LIKE '" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "'");
                LoadCart();
            }
        }

        public void Noti()
        {
            int i = 0;
            cn.Open();
            cm = new SqlCommand("SELECT * FROM vwCriticalItems", cn);
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                Alert alert = new Alert(new MainForm());
                alert.lblPcode.Text = dr["pcode"].ToString();              
                alert.showAlert(i + ". " + dr["pdesc"].ToString() + " - " + dr["qty"].ToString());
            }
            dr.Close();
            cn.Close();
        }

        private void Cashier_Load(object sender, EventArgs e)
        {
            Noti();
        }

        private void Cashier_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F8)
            {
                captureDevice.OnFrameArrived += captureDevice_OnFrameArrived;
                captureDevice.Start();
            }
        }

        private void captureDevice_OnFrameArrived(object source, FrameArrivedEventArgs e)
        {
            Bitmap bitmap = (Bitmap)e.GetFrame();
            BarcodeReader barcodeReader = new BarcodeReader();
            Result result = barcodeReader.Decode(bitmap);

            if (result != null)
            {
                txtBarcode.Invoke(new MethodInvoker(delegate ()
                {
                    txtBarcode.Text = result.Text;
                }));
            }
        }

        private void Cashier_FormClosing(object sender, FormClosingEventArgs e)
        {
            captureDevice.Stop();
        }

        public void EnableSettleButton(bool enable)
        {
            btnSettle.Enabled = enable;
        }
    }
}
