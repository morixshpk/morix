using System;
using System.Windows.Forms;

namespace Morix
{
    public partial class FrmStatus : Form
    {
        public FrmStatus()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            this.Text = Program.Name;
            this.Activate();
            this.Focus();
        }

        public void SetStatus(string status)
        {
            this.label2.Text = status;
            Application.DoEvents();
            System.Threading.Thread.Sleep(500);
        }
    }
}