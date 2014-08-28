﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace ChiuMartSAIS2.App
{
    public partial class frmPOS : Form
    {
        private Classes.Configuration conf;
        private AutoCompleteStringCollection prodSource = new AutoCompleteStringCollection();
        private AutoCompleteStringCollection unitSource = new AutoCompleteStringCollection();

        public frmPOS()
        {
            InitializeComponent();

            conf = new Classes.Configuration();
        }

        /// <summary>
        /// This function will get data from database and populate the textbox for client
        /// </summary>
        private void populateClientTextbox()
        {
            using (MySqlConnection Con = new MySqlConnection(conf.connectionstring))
            {
                try
                {
                    Con.Open();
                    string sqlQuery = "SELECT * FROM client WHERE status = 'active'";

                    MySqlCommand sqlCmd = new MySqlCommand(sqlQuery, Con);

                    MySqlDataReader reader = sqlCmd.ExecuteReader();

                    while (reader.Read())
                    {
                        string client = reader["clientName"] + " - " + reader["clientId"];
                        txtClient.AutoCompleteCustomSource.AddRange(new String[] { client });
                    }

                }
                catch (MySqlException ex)
                {
                    string errorCode = string.Format("Error Code : {0}", ex.Number);
                    MessageBox.Show(this, "Can't connect to database", errorCode, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// This function will get data from database and populate the product textbox
        /// </summary>
        private void populateProductTextbox()
        {
            using (MySqlConnection Con = new MySqlConnection(conf.connectionstring))
            {
                try
                {
                    Con.Open();
                    string sqlQuery = "SELECT p.*, u.*, c.* FROM products as p INNER JOIN units as u ON p.unitId = u.unitId INNER JOIN category as c ON p.categoryId = c.categoryId WHERE p.status = 'active'";

                    MySqlCommand sqlCmd = new MySqlCommand(sqlQuery, Con);

                    MySqlDataReader reader = sqlCmd.ExecuteReader();

                    while (reader.Read())
                    {
                        string product = reader["productName"].ToString();
                        prodSource.AddRange(new String[] { product });
                    }

                }
                catch (MySqlException ex)
                {
                    string errorCode = string.Format("Error Code : {0}", ex.Number);
                    MessageBox.Show(this, "Can't connect to database", errorCode, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Get the product by product name
        /// </summary>
        /// <param name="prodname">Name of the product</param>
        /// <returns>Product Array</returns>
        private String[] getProductByName(string prodname)
        {
            string[] result = new string[6];
            using (MySqlConnection Con = new MySqlConnection(conf.connectionstring))
            {
                try
                {
                    Con.Open();
                    string sqlQuery = "SELECT p.*, u.*, c.* FROM products as p INNER JOIN units as u ON p.unitId = u.unitId INNER JOIN category as c ON p.categoryId = c.categoryId WHERE p.status = 'active' AND p.productName = @prodname";

                    MySqlCommand sqlCmd = new MySqlCommand(sqlQuery, Con);
                    sqlCmd.Parameters.AddWithValue("prodname", prodname);

                    MySqlDataReader reader = sqlCmd.ExecuteReader();

                    while (reader.Read())
                    {
                        result[0] = reader["productId"].ToString();
                        result[1] = "1";
                        result[2] = reader["productName"].ToString();
                        result[3] = reader["unitDesc"].ToString();
                        result[4] = reader["productPrice"].ToString();

                        double total = double.Parse(result[1]) * double.Parse(result[4]);
                        result[5] = total.ToString();
                    }
                    return result;
                }
                catch (MySqlException ex)
                {
                    string errorCode = string.Format("Error Code : {0}", ex.Number);
                    MessageBox.Show(this, "Can't connect to database", errorCode, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return result;
                }
            }
        }

        /// <summary>
        /// Check the product stock
        /// </summary>
        /// <param name="id">The id of the product</param>
        /// <returns>Product Stock</returns>
        private int checkProductStockById(int id)
        {
            int result = 0;
            using (MySqlConnection Con = new MySqlConnection(conf.connectionstring))
            {
                try
                {
                    Con.Open();
                    string sqlQuery = "SELECT p.* FROM products as p WHERE p.status = 'active' AND p.productId = @id";

                    MySqlCommand sqlCmd = new MySqlCommand(sqlQuery, Con);
                    sqlCmd.Parameters.AddWithValue("id", id);

                    MySqlDataReader reader = sqlCmd.ExecuteReader();

                    while (reader.Read())
                    {
                        result = Int32.Parse(reader["productStock"].ToString());
                    }
                    return result;
                }
                catch (MySqlException ex)
                {
                    string errorCode = string.Format("Error Code : {0}", ex.Number);
                    MessageBox.Show(this, "Can't connect to database", errorCode, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return result;
                }
            }
        }

        private void frmPOS_Load(object sender, EventArgs e)
        {
            populateClientTextbox();
            populateProductTextbox();

            // GENERATE NEW OR
            txtOrNo.Text = generateOR();
        }

        private void txtClient_Click(object sender, EventArgs e)
        {
            txtClient.SelectAll();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Parse the product string
            if (txtAddress.Text != "")
            {
                string[] product = Regex.Split(txtAddress.Text, " - ");
                int id = Int32.Parse(product[1]);

                // update the full total price of items on the cart
                updateTotalPrice();
            }
            else
            {
                MessageBox.Show("You have to select a product first", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
        }

        private void txtProduct_Click(object sender, EventArgs e)
        {
            txtAddress.SelectAll();
        }

        private void dgvCart_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //cartUpdateTotal();
        }

        /// <summary>
        /// This will update the changes on the cart
        /// </summary>
        private void cartUpdateTotal()
        {
            //try
           // {
                // update for the total
                for (int i = 0; i < (dgvCart.Rows.Count - 1); i++)
                {
                    // Check the update on quantity if there's enough stocks
                    // if not, set it to maximum stocks
                    if (dgvCart.Rows[i].Cells[0].Value != null) {
                        int id = Int32.Parse(dgvCart.Rows[i].Cells[0].Value.ToString());
                        int stock = checkProductStockById(id);
                        int updatedStock = Int32.Parse(dgvCart.Rows[i].Cells[1].Value.ToString());

                        if (stock < updatedStock)
                        {
                            dgvCart.Rows[i].Cells[1].Value = stock;
                        }

                        // update the total
                        double total = double.Parse(dgvCart.Rows[i].Cells[4].Value.ToString()) * double.Parse(dgvCart.Rows[i].Cells[1].Value.ToString());
                        dgvCart.Rows[i].Cells[5].Value = total.ToString();
                    }

                }

                // update the full total price of items on the cart
                updateTotalPrice();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message.ToString());
            //}
        }

        private void updateTotalPrice()
        {
            try
            {
                double total = 0;
                for (int i = 0; i < (dgvCart.Rows.Count - 1); i++)
                {
                    total += double.Parse(dgvCart.Rows[i].Cells[5].Value.ToString());
                }

                lblTotal.Text = total.ToString();
            }
            catch(Exception ex)
            {

            }
        }

        private void btnCheckout_Click(object sender, EventArgs e)
        {
            // Check if ability to checkout
            if (dgvCart.Rows.Count <= 1)
            {
                MessageBox.Show("There's no item available for checkout because the cart is empty or the total is zero", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Open the checkout form
            Dialogs.dlgCheckout frm = new Dialogs.dlgCheckout();
            // Set the variables
            frm.total = lblTotal.Text;
            if (frm.ShowDialog() == DialogResult.OK)
            {

            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            // Open the transaction history form
            Dialogs.dlgTransactionHistoy frm = new Dialogs.dlgTransactionHistoy();
            // Set the variables
            if (frm.ShowDialog() == DialogResult.OK)
            {

            }
        }

        /// <summary>
        /// GENERATE NEW OR NUMBER
        /// </summary>
        private string generateOR()
        {
            string lastOrNumber = "";
            DateTime today = DateTime.Today;
            int currentYear = today.Year;
            int currentMonth = today.Month + 1;

            string generatedOR = "";

            // Get the last OR number
            try
            {
                using (MySqlConnection con = new MySqlConnection(conf.connectionstring))
                {
                    con.Open();
                    string sqlQuery = "SELECT * FROM `or` ORDER BY id ASC LIMIT 1";
                    MySqlCommand sqlCmd = new MySqlCommand(sqlQuery, con);
                    MySqlDataReader reader = sqlCmd.ExecuteReader();

                    while (reader.Read())
                    {
                        lastOrNumber = reader["ornumber"].ToString();
                    }
                }
            }
            catch (MySqlException ex)
            {
                string errorCode = string.Format("Error Code : {0}", ex.Number);
                MessageBox.Show(this, "Can't connect to database: " + ex.Message.ToString(), errorCode, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Check if there is no last OR number
            if (lastOrNumber == "")
            {
                // Create the first one
                generatedOR = currentYear.ToString() + currentMonth.ToString("00") + "0001";
            }
            else
            {
                // Create a new one
                string lastYear = lastOrNumber.Substring(0, 4);
                string lastMonth = lastOrNumber.Substring(4, 2);
                string lastOr = lastOrNumber.Substring(6, 4);

                if (currentYear.ToString() == lastYear)
                {
                    currentYear = Int32.Parse(lastYear);
                }
                else
                {
                    lastOr = "0000";
                }

                // convert the or number to int and increment it by 1
                int orNum = Int32.Parse(lastOr) + 1;

                generatedOR = currentYear.ToString() + currentMonth.ToString() + orNum.ToString("0000");
            }

            return generatedOR;
        }

        private void dgvCart_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            TextBox prodName = e.Control as TextBox;
            if (dgvCart.CurrentCell.ColumnIndex == 2)
            {
                if (prodName != null)
                {
                    prodName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    prodName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    prodName.AutoCompleteCustomSource = prodSource;
                }
            }
            else
            {
                prodName.AutoCompleteCustomSource = null;
            }
        }

        private void dgvCart_EditModeChanged(object sender, EventArgs e)
        {
            //MessageBox.Show("Done editing");
        }

        private void dgvCart_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvCart.CurrentCell.ColumnIndex == 2)
            {
                try
                {
                    string[] item = getProductByName(dgvCart.Rows[dgvCart.CurrentRow.Index].Cells[2].Value.ToString());

                    dgvCart.Rows[dgvCart.CurrentRow.Index].Cells[0].Value = item[0];
                    //dgvCart.Rows[dgvCart.CurrentRow.Index].Cells[1].Value = 1;
                    dgvCart.Rows[dgvCart.CurrentRow.Index].Cells[3].Value = item[3];
                    dgvCart.Rows[dgvCart.CurrentRow.Index].Cells[4].Value = item[4];
                    dgvCart.Rows[dgvCart.CurrentRow.Index].Cells[5].Value = item[5];

                    for (int i = 0; i < (dgvCart.Rows.Count - 1); i++)
                    {
                        // Check the update on quantity if there's enough stocks
                        // if not, set it to maximum stocks
                        int id = Int32.Parse(dgvCart.Rows[i].Cells[0].Value.ToString());
                        int stock = checkProductStockById(id);
                        int updatedStock = Int32.Parse(dgvCart.Rows[i].Cells[1].Value.ToString());

                        if (stock < updatedStock)
                        {
                            dgvCart.Rows[i].Cells[1].Value = stock;
                        }
                    }

                    dgvCart.Rows[dgvCart.CurrentRow.Index].Cells[1].Selected = true;
                    updateTotalPrice();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Please enter a product", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
            else
            {
                cartUpdateTotal();
                updateTotalPrice();
            }
            
        }

        private void btnNewTransaction_Click(object sender, EventArgs e)
        {
            dgvCart.Rows.Clear();
        }
    }
}
