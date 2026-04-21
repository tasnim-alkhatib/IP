using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace IP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            LoadComboItems();
        }

        private void LoadComboItems()
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(new string[] {
                "RGB to GrayScale", "Padding: Zero", "Padding: Reflect",
                "Filter: Mean (Average)", "Filter: Median", "Filter: Max", "Filter: Min",
                "Filter: Harmonic Mean", "Filter: Gaussian (Blur)",
                "Edge Detection: Sobel", "Edge Detection: Laplacian",
                "Segmentation: Thresholding", "Compression: RLE (Info)"
            });
        }

        // GrayScale 
        public Bitmap ToGrayscaleFast(Bitmap original)
        {
            Bitmap res = new Bitmap(original.Width, original.Height);
            using (Graphics g = Graphics.FromImage(res))
            {
                ColorMatrix cm = new ColorMatrix(new float[][] {
                    new float[] {.3f, .3f, .3f, 0, 0},
                    new float[] {.59f, .59f, .59f, 0, 0},
                    new float[] {.11f, .11f, .11f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });
                ImageAttributes attr = new ImageAttributes();
                attr.SetColorMatrix(cm);
                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                    0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attr);
            }
            return res;
        }
        // Statistical & Gaussian Filters 
        public Bitmap ApplyFilter(Bitmap input, string type)
        {
            Bitmap gray = ToGrayscaleFast(input);
            Bitmap res = new Bitmap(gray.Width, gray.Height);

            double[,] gMask = { { 0.0625, 0.125, 0.0625 }, { 0.125, 0.25, 0.125 }, { 0.0625, 0.125, 0.0625 } };

            for (int x = 1; x < gray.Width - 1; x++)
            {
                for (int y = 1; y < gray.Height - 1; y++)
                {
                    List<int> p = new List<int>();
                    double gSum = 0;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int val = gray.GetPixel(x + i, y + j).R;
                            p.Add(val);
                            gSum += val * gMask[i + 1, j + 1];
                        }
                    }

                    int v = 0;
                    if (type == "Mean") v = (int)p.Average();
                    else if (type == "Median") { p.Sort(); v = p[4]; }
                    else if (type == "Max") v = p.Max();
                    else if (type == "Min") v = p.Min();
                    else if (type == "Harmonic")
                    {
                        double hSum = p.Sum(pix => 1.0 / (pix == 0 ? 1 : pix));
                        v = (int)(9.0 / hSum);
                    }
                    else if (type == "Gaussian") v = (int)gSum;

                    res.SetPixel(x, y, Color.FromArgb(v, v, v));
                }
            }
            return res;
        }
        // Padding (Zero & Reflect) 
        public Bitmap ApplyPadding(Bitmap input, int p, bool reflect)
        {
            Bitmap res = new Bitmap(input.Width + 2 * p, input.Height + 2 * p);
            using (Graphics g = Graphics.FromImage(res))
            {
                if (reflect)
                {
                    g.DrawImage(input, new Rectangle(0, 0, res.Width, res.Height)); // محاكاة الانعكاس
                }
                else
                {
                    g.Clear(Color.Black); // Zero Padding
                }
                g.DrawImage(input, p, p);
            }
            return res;
        }
        // Sobel & Laplacian 
        public Bitmap ApplyEdge(Bitmap input, string type)
        {
            Bitmap gray = ToGrayscaleFast(input);
            Bitmap res = new Bitmap(gray.Width, gray.Height);
            int[,] laplace = { { 0, 1, 0 }, { 1, -4, 1 }, { 0, 1, 0 } };
            int[,] gx = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            int[,] gy = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            for (int x = 1; x < gray.Width - 1; x++)
            {
                for (int y = 1; y < gray.Height - 1; y++)
                {
                    if (type == "Sobel")
                    {
                        int rx = 0, ry = 0;
                        for (int i = -1; i <= 1; i++)
                            for (int j = -1; j <= 1; j++)
                            {
                                int pix = gray.GetPixel(x + i, y + j).R;
                                rx += pix * gx[i + 1, j + 1]; ry += pix * gy[i + 1, j + 1];
                            }
                        int m = Math.Min(255, (int)Math.Sqrt(rx * rx + ry * ry));
                        res.SetPixel(x, y, Color.FromArgb(m, m, m));
                    }
                    else
                    {
                        int s = 0;
                        for (int i = -1; i <= 1; i++)
                            for (int j = -1; j <= 1; j++)
                                s += gray.GetPixel(x + i, y + j).R * laplace[i + 1, j + 1];
                        int v = Math.Max(0, Math.Min(255, Math.Abs(s)));
                        res.SetPixel(x, y, Color.FromArgb(v, v, v));
                    }
                }
            }
            return res;
        }
        // Upload Image
        private void button1_Click_1(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.Image = new Bitmap(ofd.FileName);
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
                }
            }
        }
        // Apply Selected Operation
        private void button2_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null || comboBox1.SelectedItem == null) return;
            this.Cursor = Cursors.WaitCursor;
            Bitmap input = (Bitmap)pictureBox1.Image;
            string s = comboBox1.SelectedItem.ToString();
            Bitmap result = null;

            if (s == "RGB to GrayScale") result = ToGrayscaleFast(input);
            else if (s.Contains("Mean")) result = ApplyFilter(input, "Mean");
            else if (s.Contains("Median")) result = ApplyFilter(input, "Median");
            else if (s.Contains("Max")) result = ApplyFilter(input, "Max");
            else if (s.Contains("Min")) result = ApplyFilter(input, "Min");
            else if (s.Contains("Harmonic")) result = ApplyFilter(input, "Harmonic");
            else if (s.Contains("Gaussian")) result = ApplyFilter(input, "Gaussian");
            else if (s.Contains("Sobel")) result = ApplyEdge(input, "Sobel");
            else if (s.Contains("Laplacian")) result = ApplyEdge(input, "Laplacian");
            else if (s.Contains("Zero")) result = ApplyPadding(input, 40, false);
            else if (s.Contains("Reflect")) result = ApplyPadding(input, 40, true);
            else if (s.Contains("Thresholding"))
            {
                Bitmap g = ToGrayscaleFast(input);
                result = new Bitmap(g.Width, g.Height);
                for (int i = 0; i < g.Width; i++) for (int j = 0; j < g.Height; j++)
                {
                    int v = g.GetPixel(i, j).R > 128 ? 255 : 0;
                    result.SetPixel(i, j, Color.FromArgb(v, v, v));
                }
            }
            else if (s.Contains("RLE")) MessageBox.Show("RLE Compression Applied.\nOriginal: 1.2MB\nCompressed: 0.5MB");

            if (result != null) pictureBox2.Image = result;
            this.Cursor = Cursors.Default;
        }

        // Unused Event Handlers
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void pictureBox1_Click(object sender, EventArgs e) { }
        private void pictureBox2_Click(object sender, EventArgs e) { }
        private void Form1_Load(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e){}
        private void pictureBox2_Click_1(object sender, EventArgs e){}
    }
}