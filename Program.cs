using System;
using System.Windows.Forms;

namespace PalServerManager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new FrmMain());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"³ÌÐòÆô¶¯Ê§°Ü£º{ex}", "ÖÂÃü´íÎó", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}