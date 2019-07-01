using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chromium;
using Chromium.Remote;
using Chromium.WebBrowser;

namespace ChromiumFXAndCDP
{
    public partial class Form1 : Form
    {
        public ChromiumWebBrowser wb;
        public string txt = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            wb.MinimumSize = new Size(600, 600);
            textBox1.Text = txt;
        }
    }
}
