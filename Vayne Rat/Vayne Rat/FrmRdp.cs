using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vayne_Rat
{
    public partial class FrmRdp : Form
    {
        public Bitmap image;
        public FrmRdp()
        {
            InitializeComponent();
        }

        private void FrmRdp_Shown(object sender, EventArgs e)
        {
            //tSetImage.Enabled = true;
            backgroundWorker1.RunWorkerAsync();
        }

        private void tSetImage_Tick(object sender, EventArgs e)
        {
            if (image != null)
                pictureBox1.Image = image;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while(true)
            { 
                try
                { 

                    if(backgroundWorker1.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                    if (image != null)
                    {
                        pictureBox1.Invoke((MethodInvoker)delegate
                       {
                           pictureBox1.Image = image;

                       });
                    }

                    Thread.Sleep(1000);
                }
                catch (Exception)
                {

                };
            }
        }

        private void FrmRdp_FormClosing(object sender, FormClosingEventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }
    }
}
