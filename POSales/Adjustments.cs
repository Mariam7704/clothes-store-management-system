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

namespace POSales
{
    public partial class Adjustments : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        MainForm main;
        int _qty;
        public Adjustments(MainForm mn)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            main = mn;
            ReferenceNo();
            LoadStock();
            lblUsername.Text = main.lblUsername.Text;
            SetupActionComboBox();
        }

        private void SetupActionComboBox()
        {
            cbAction.DropDownStyle = ComboBoxStyle.DropDownList;
            cbAction.Items.Clear();
            cbAction.Items.Add("Add To Inventory");
            cbAction.Items.Add("Remove From Inventory");
        }

        public void ReferenceNo()
        {
            Random rnd = new Random();
            lblRefNo.Text = rnd.Next().ToString();
        }

        public void LoadStock()
        {
            int i = 0;
            dgvAdjustment.Rows.Clear();
            cm = new SqlCommand("SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty FROM tbProduct AS p INNER JOIN tbBrand AS b ON b.id = p.bid INNER JOIN tbCategory AS c on c.id = p.cid WHERE CONCAT(p.pdesc, b.brand, c.category) LIKE '%" + txtSearch.Text + "%'", cn);
            cn.Open();
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                dgvAdjustment.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString());
            }
            dr.Close();
            cn.Close();
        }

        private void dgvAdjustment_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            string colName = dgvAdjustment.Columns[e.ColumnIndex].Name;
            if(colName=="Select")
            {
                lblPcode.Text = dgvAdjustment.Rows[e.RowIndex].Cells[1].Value.ToString();
                lblDesc.Text = dgvAdjustment.Rows[e.RowIndex].Cells[3].Value.ToString() + " " + " " + dgvAdjustment.Rows[e.RowIndex].Cells[5].Value.ToString();
                _qty = int.Parse(dgvAdjustment.Rows[e.RowIndex].Cells[7].Value.ToString());
                btnSave.Enabled = true;
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadStock();
        }

        public void Clear()
        {
            lblDesc.Text = "";
            lblPcode.Text = "";
            txtQty.Clear();
            txtRemark.Clear();
            cbAction.SelectedIndex = -1;
            ReferenceNo();
        }

        private void txtQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Only allow digits and control characters
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                //validation for empty field
                if(cbAction.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select action for add or reduce.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbAction.Focus();
                    return;
                }

                if(string.IsNullOrWhiteSpace(txtQty.Text))
                {
                    MessageBox.Show("Please input quantity for add or reduce.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtQty.Focus();
                    return;
                }

                int adjustmentQty = int.Parse(txtQty.Text);
                if(adjustmentQty <= 0)
                {
                    MessageBox.Show("Quantity must be greater than 0.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtQty.Focus();
                    return;
                }

                if(string.IsNullOrWhiteSpace(txtRemark.Text))
                {
                    MessageBox.Show("Need reason for stock adjustment.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRemark.Focus();
                    return;
                }

                //update stock
                if(cbAction.Text == "Remove From Inventory")
                {
                    if(adjustmentQty > _qty)
                    {
                        MessageBox.Show($"Cannot remove {adjustmentQty} items. Stock on hand is only {_qty}.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    dbcon.ExecuteQuery("UPDATE tbProduct SET qty = (qty - " + adjustmentQty + ") WHERE pcode LIKE '" + lblPcode.Text + "'");
                }
                else if(cbAction.Text == "Add To Inventory")
                {
                    dbcon.ExecuteQuery("UPDATE tbProduct SET qty = (qty + " + adjustmentQty + ") WHERE pcode LIKE '" + lblPcode.Text + "'");
                }

                dbcon.ExecuteQuery("INSERT INTO tbAdjustment(referenceno, pcode, qty, action, remarks, sdate, [user]) VALUES ('"+lblRefNo.Text+ "','" + lblPcode.Text + "','" + adjustmentQty + "', '" + cbAction.Text + "', '" + txtRemark.Text + "', '" + DateTime.Now.ToShortDateString() + "','" + lblUsername.Text + "')");
                MessageBox.Show("Stock has been successfully adjusted.", "Process completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadStock();
                Clear();
                btnSave.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
