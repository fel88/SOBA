using System;
using System.Drawing;
using System.Windows.Forms;

namespace Soba
{
    public partial class TagEditDialog : Form
    {
        public TagEditDialog()
        {
            InitializeComponent();
        }

        public void Set(string str)
        {
            textBox1.Text = str;
        }

        public string Value
        {
            get
            {
                return textBox1.Text;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                textBox1.BackColor = Color.Red;
                textBox1.ForeColor = Color.White;
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.BackColor = Color.White;
            textBox1.ForeColor = Color.Black;
        }
    }
}
