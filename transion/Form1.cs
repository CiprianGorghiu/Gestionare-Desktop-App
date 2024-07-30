using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using LiteDB;

namespace transion
{
    public partial class Form1 : Form
    {
        private LiteDatabase database;
        private ILiteCollection<Product> productCollection;

        public Form1()
        {
            InitializeComponent();

            CheckDatabaseStatus();
           
            InitializeDataGridView();
            LoadData();
        }
        private void CheckDatabaseStatus()
        {
            string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            string dbPath = Path.Combine(dataDirectory, "Products.db");

            try
            {
                database = new LiteDatabase(dbPath);
                productCollection = database.GetCollection<Product>("products");
                SetDatabaseStatus(true);
            }
            catch (Exception)
            {
                SetDatabaseStatus(false);
            }
        }
        private void InitializeDataGridView()
        {
            dataGridView1.AutoGenerateColumns = false;
            int columnWidth = 100;

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Id",
                Name = "Id",
                HeaderText = "Id Produs",
                Width = columnWidth
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                Name = "Name",
                HeaderText = "Denumire",
                Width = columnWidth
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Quantity",
                Name = "Quantity",
                HeaderText = "Cantitate",
                Width = columnWidth
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Unit",
                Name = "Unit",
                HeaderText = "Unitate de masura",
                Width = columnWidth
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Date",
                Name = "Date",
                HeaderText = "Data",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "G" },
                Width = 200
            });
        }

        private void LoadData()
        {
            var products = productCollection.FindAll().ToList();
            dataGridView1.DataSource = products;
            UpdateMaxQuantityProductCard();
            UpdateMinQuantityProductCard();
            // Configurare chart
            ConfigureChart(products);
        }

        private void ConfigureChart(List<Product> products)
        {
            chartProducts.Series.Clear();
            chartProducts.ChartAreas.Clear();
            chartProducts.ChartAreas.Add(new ChartArea("MainArea"));

            // Crearea seriei pentru produse
            var productSeries = new Series("Products")
            {
                ChartType = SeriesChartType.Column,
                Color = Color.SteelBlue,
                BorderWidth = 2,
                IsValueShownAsLabel = true,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            chartProducts.Series.Add(productSeries);

            if (products.Count > 0)
            {
                var maxQuantityProduct = products.OrderByDescending(p => p.Quantity).First();
                var minQuantityProduct = products.OrderBy(p => p.Quantity).First();

                foreach (var product in products)
                {
                    var point = new DataPoint
                    {
                        AxisLabel = product.Name,
                        YValues = new double[] { product.Quantity },
                        Label = product.Quantity.ToString()
                    };

                    if (product.Id == maxQuantityProduct.Id)
                    {
                        point.Color = Color.Red; // Evidențiază produsul cu cantitatea maximă
                    }
                    else if (product.Id == minQuantityProduct.Id)
                    {
                        point.Color = Color.Orange; // Evidențiază produsul cu cantitatea minimă
                    }

                    productSeries.Points.Add(point);
                }
            }

            // Configurare axele chart-ului
            var chartArea = chartProducts.ChartAreas["MainArea"];
            chartArea.AxisX.Title = "Denumire Produs";
            chartArea.AxisX.TitleFont = new Font("Arial", 12, FontStyle.Bold);
            chartArea.AxisX.LabelStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            chartArea.AxisX.MajorGrid.Enabled = false;
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Angle = -45;  // Pentru a roti etichetele și a evita suprapunerea

            chartArea.AxisY.Title = "Cantitate";
            chartArea.AxisY.TitleFont = new Font("Arial", 12, FontStyle.Bold);
            chartArea.AxisY.LabelStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;

            // Configurare legendă
            chartProducts.Legends.Clear();
            var legend = new Legend
            {
                Docking = Docking.Top,
                Alignment = StringAlignment.Center,
                LegendStyle = LegendStyle.Row,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            chartProducts.Legends.Add(legend);

            // Personalizare suplimentară pentru un aspect mai profesional
            chartProducts.BackColor = Color.WhiteSmoke;
            chartProducts.ChartAreas[0].BackColor = Color.White;
            chartProducts.BorderlineColor = Color.Gray;
            chartProducts.BorderlineDashStyle = ChartDashStyle.Solid;
            chartProducts.BorderSkin.SkinStyle = BorderSkinStyle.Emboss;
        }




        private void btnHome_Click(object sender, EventArgs e)
        {
            pnlActiveHome.Visible = true;
            pnlActiveInv.Visible = false;
            mainPanel.Visible = true;
            inventoryPanel.Visible = false;
        }

        private void inventoryBtn_Click(object sender, EventArgs e)
        {
            pnlActiveInv.Visible = true;
            pnlActiveHome.Visible = false;
            inventoryPanel.Visible = true;
            mainPanel.Visible = false;
        }

        private void exitBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            int id = int.Parse(txtId.Text);
            var existingProduct = productCollection.FindById(id);
            if (existingProduct != null)
            {
                MessageBox.Show("Există deja un produs cu acest Id.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var product = new Product
            {
                Id = id,
                Name = txtName.Text,
                Quantity = int.Parse(txtQuantity.Text),
                Unit = txtUnit.Text,
                Date = DateTime.Now
            };

            productCollection.Insert(product);
            LoadData();
            ClearTextBoxes();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            int id = int.Parse(txtId.Text);
            var existingProduct = productCollection.FindById(id);
            if (existingProduct == null)
            {
                MessageBox.Show("Produsul nu există.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var product = new Product
            {
                Id = id,
                Name = txtName.Text,
                Quantity = int.Parse(txtQuantity.Text),
                Unit = txtUnit.Text,
                Date = DateTime.Now
            };

            productCollection.Update(product);
            LoadData();
            ClearTextBoxes();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(txtId.Text, out id))
            {
                MessageBox.Show("Id-ul trebuie să fie un număr valid.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var existingProduct = productCollection.FindById(id);
            if (existingProduct == null)
            {
                MessageBox.Show("Produsul nu există.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            productCollection.Delete(id);
            LoadData();
            ClearTextBoxes();
        }

        private void ClearTextBoxes()
        {
            txtId.Clear();
            txtName.Clear();
            txtQuantity.Clear();
            txtUnit.Clear();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dataGridView1.Rows[e.RowIndex];
                txtId.Text = row.Cells[0].Value.ToString(); // Id
                txtName.Text = row.Cells[1].Value.ToString(); // Name
                txtQuantity.Text = row.Cells[2].Value.ToString(); // Quantity
                txtUnit.Text = row.Cells[3].Value.ToString(); // Unit
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                MessageBox.Show("Câmpul Id nu poate fi gol.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!int.TryParse(txtId.Text, out _))
            {
                MessageBox.Show("Id-ul trebuie să fie un număr valid.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Câmpul Denumire nu poate fi gol.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtQuantity.Text))
            {
                MessageBox.Show("Câmpul Cantitate nu poate fi gol.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!int.TryParse(txtQuantity.Text, out _))
            {
                MessageBox.Show("Cantitatea trebuie să fie un număr valid.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtUnit.Text))
            {
                MessageBox.Show("Câmpul Unitate de masura nu poate fi gol.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private void SetDatabaseStatus(bool isConnected)
        {
            if (isConnected)
            {
                lblDatabaseStatus.Text = "Ok";
                lblDatabaseStatus.ForeColor = Color.Green;
            }
            else
            {
                lblDatabaseStatus.Text = "Error";
                lblDatabaseStatus.ForeColor = Color.Red;
            }
        }

        private void UpdateMinQuantityProductCard()
        {
            try
            {
                var products = productCollection.FindAll().ToList();
                if (products.Count > 0)
                {
                    var minQuantityProduct = products.OrderBy(p => p.Quantity).First();
                    lblLowProductQuanity.Text = minQuantityProduct.Name;
                }
                else
                {
                    lblLowProductQuanity.Text = "N/A";
                }
            }
            catch (Exception)
            {
                lblLowProductQuanity.Text = "Error";
            }
        }
        private void UpdateMaxQuantityProductCard()
        {
            try
            {
                var products = productCollection.FindAll().ToList();
                if (products.Count > 0)
                {
                    var maxQuantityProduct = products.OrderByDescending(p => p.Quantity).First();
                    lblMaxQuantityProductName.Text = maxQuantityProduct.Name;
                }
                else
                {
                    lblMaxQuantityProductName.Text = "N/A";
                }
            }
            catch (Exception)
            {
                lblMaxQuantityProductName.Text = "Error";
            }
        }

    }
 
    

}
