using System;
using System.Windows.Forms;

namespace Morix
{
    public partial class FrmConfirm : Form
    {
        public FrmConfirm()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            this.Text = Program.Name;

            this.label2.Text = string.Format("A new version of {0} is available. Do you want to install it?", Program.Name);
            this.Activate();
            this.Focus();
        }
    }
}