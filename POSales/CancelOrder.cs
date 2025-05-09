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
    public partial class CancelOrder : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        DailySale dailySale;
        public CancelOrder(DailySale sale)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            dailySale = sale;            
        }

        private void btnCOrder_Click(object sender, EventArgs e)
        {
            try
            {
                if(cboInventory.Text != string.Empty && udCancelQty.Value > 0 && txtReason.Text != string.Empty)
                {
                    if(int.Parse(txtQty.Text) >= udCancelQty.Value)
                    {
                        // Directly perform cancellation without Void form
                        SaveCancelOrder();
                        if(cboInventory.Text.ToLower() == "yes")
                        {
                            dbcon.ExecuteQuery("UPDATE tbProduct SET qty = qty + " + udCancelQty.Value + " where pcode= '" + txtPcode.Text + "'");
                        }
                        dbcon.ExecuteQuery("UPDATE tbCart SET qty = qty + " + udCancelQty.Value + " where id LIKE '" + txtId.Text + "'");
                        MessageBox.Show("Order transaction successfully cancelled!", "Cancel Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ReloadSoldList();
                        this.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void SaveCancelOrder()
        {
            try
            {
                cn.Open();
                cm = new SqlCommand("insert into tbCancel (transno, pcode, price, qty, total, sdate, cancelledby, reason, action) values (@transno, @pcode, @price, @qty, @total, @sdate, @cancelledby, @reason, @action)", cn);
                cm.Parameters.AddWithValue("@transno", txtTransno.Text);
                cm.Parameters.AddWithValue("@pcode", txtPcode.Text);
                cm.Parameters.AddWithValue("@price", double.Parse(txtPrice.Text));
                cm.Parameters.AddWithValue("@qty", int.Parse(txtQty.Text));
                cm.Parameters.AddWithValue("@total", double.Parse(txtTotal.Text));
                cm.Parameters.AddWithValue("@sdate", DateTime.Now);
                cm.Parameters.AddWithValue("@cancelledby", txtCancelBy.Text);
                cm.Parameters.AddWithValue("@reason", txtReason.Text);
                cm.Parameters.AddWithValue("@action", cboInventory.Text);
                cm.ExecuteNonQuery();
                cn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        public void ReloadSoldList()
        {
            dailySale.LoadSold();
        }

        private void picClose_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void cboInventory_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void CancelOrder_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Escape)
            {
                this.Dispose();
            }
        }

    }
}
