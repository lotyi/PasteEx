﻿using PasteEx.Core;
using PasteEx.Util;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PasteEx
{
    public partial class FormMain : Form
    {
        #region Init

        private static FormMain dialogue = null;

        private ClipboardData data;

        private string currentLocation;

        private string lastAutoGeneratedFileName;

        private HotkeyHook hotkeyHook = new HotkeyHook();

        public string CurrentLocation
        {
            get
            {
                return currentLocation;
            }
            set
            {
                currentLocation = value.EndsWith("\\") ? value : value + "\\";
                tsslCurrentLocation.ToolTipText = currentLocation;
                tsslCurrentLocation.Text = GenerateDisplayLocation(currentLocation);
            }
        }

        public static FormMain GetInstance()
        {
            return dialogue;
        }

        public FormMain()
        {
            dialogue = this;
            InitializeComponent();
            CurrentLocation = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        public FormMain(string location)
        {
            dialogue = this;
            InitializeComponent();
            CurrentLocation = location;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            data = new ClipboardData(Clipboard.GetDataObject());
            data.SaveCompleted += Exit; // exit when save completed
            string[] extensions = data.Analyze();
            cboExtension.Items.AddRange(extensions);
            if (extensions.Length > 0)
            {
                cboExtension.Text = extensions[0] ?? "";
            }
            else
            {
                if (MessageBox.Show(this, Resources.Strings.TipAnalyzeFailed, Resources.Strings.Title,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    btnChooseLocation.Enabled = false;
                    btnSave.Enabled = false;
                    txtFileName.Enabled = false;
                    cboExtension.Enabled = false;
                    tsslCurrentLocation.Text = Resources.Strings.TxtCanOnlyUse;
                }
                else
                {
                    Environment.Exit(0);
                }

            }

            lastAutoGeneratedFileName = GenerateFileName(CurrentLocation, cboExtension.Text);
            txtFileName.Text = lastAutoGeneratedFileName;
        }
        #endregion

        #region Generate path
        public static string GenerateFileName(string folder, string extension)
        {
            string defaultFileName = "Clipboard_" + DateTime.Now.ToString("yyyyMMdd");
            string path = folder + defaultFileName + "." + extension;

            string result;
            string newFileName = defaultFileName;
            int i = 0;
            while (true)
            {
                if (File.Exists(path))
                {
                    newFileName = defaultFileName + " (" + ++i + ")";
                    path = folder + newFileName + "." + extension;
                }
                else
                {
                    result = newFileName;
                    break;
                }

                if (i > 300)
                {
                    result = "Default";
                    break;
                }
            }
            return result;
        }

        private string GenerateDisplayLocation(string location)
        {
            const int maxLength = 47;
            const string ellipsis = "...";

            int length = Encoding.Default.GetBytes(location).Length;
            if (length <= maxLength)
            {
                return location;
            }

            // short display location
            int i;
            byte[] b;
            int tail = 0;
            char[] tailChars = new char[location.Length];
            int k = 0;
            for (i = location.Length - 1; i >= 0; i--)
            {
                b = Encoding.Default.GetBytes(location[i].ToString());
                if (b.Length > 1)
                {
                    tail += 2;
                }
                else
                {
                    tail++;
                }
                tailChars[k++] = location[i];
                if (location[i] == '\\' && i != location.Length - 1)
                {
                    break;
                }
            }
            int head = maxLength - ellipsis.Length - tail;
            if (head >= 3)
            {
                // c:\xxx\xxx\xx...\xxxxx\
                StringBuilder sb = new StringBuilder();
                sb.Append(StrCut(location, head));
                sb.Append(ellipsis);
                string tailStr = "";
                for (i = tailChars.Length - 1; i >= 0; i--)
                {
                    if (tailChars[i] != '\0')
                    {
                        tailStr += tailChars[i];
                    }
                }
                sb.Append(tailStr);
                return sb.ToString();
            }
            else
            {
                // c:\xxx\xxx\xxxx\xxxxx...
                return StrCut(location, maxLength - ellipsis.Length) + ellipsis;
            }
        }

        private string StrCut(string str, int length)
        {
            int len = 0;
            byte[] b;
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                b = Encoding.Default.GetBytes(str[i].ToString());
                if (b.Length > 1)
                {
                    len += 2;
                }
                else
                {
                    len++;
                }

                if (len >= length)
                {
                    break;
                }
                sb.Append(str[i]);
            }

            return sb.ToString();
        }
        #endregion

        #region UI event
        private void btnSave_Click(object sender, EventArgs e)
        {
            btnChooseLocation.Enabled = false;
            btnSettings.Enabled = false;
            btnSave.Enabled = false;

            string location = CurrentLocation.EndsWith("\\") ? CurrentLocation : CurrentLocation + "\\";
            string path = location + txtFileName.Text + "." + cboExtension.Text;

            if (File.Exists(path))
            {
                DialogResult result = MessageBox.Show(String.Format(Resources.Strings.TipTargetFileExisted, path),
                    Resources.Strings.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    data.SaveAsync(path, cboExtension.Text);
                }
                else if (result == DialogResult.No)
                {
                    btnChooseLocation.Enabled = true;
                    btnSettings.Enabled = true;
                    btnSave.Enabled = true;
                    return;
                }
            }
            else
            {
                data.SaveAsync(path, cboExtension.Text);
            }
        }

        private void btnChooseLocation_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, Resources.Strings.TipPathNotNull,
                        Resources.Strings.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else
                {
                    CurrentLocation = dialog.SelectedPath;
                }
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            Button btnSender = (Button)sender;
            System.Drawing.Point ptLowerLeft = new System.Drawing.Point(0, btnSender.Height);
            ptLowerLeft = btnSender.PointToScreen(ptLowerLeft);
            contextMenuStripSetting.Show(ptLowerLeft);


        }

        private void monitorModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartMonitorMode();
        }

        private void collectModeToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void settingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form f = FormSetting.GetInstance();
            f.ShowDialog();
            f.Activate();
        }

        private void cboExtension_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Re-Generate FileName
            if (lastAutoGeneratedFileName == txtFileName.Text)
            {
                lastAutoGeneratedFileName = GenerateFileName(CurrentLocation, cboExtension.Text);
                txtFileName.Text = lastAutoGeneratedFileName;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // (keyData == (Keys.Control | Keys.S))
            if (keyData == Keys.Enter)
            {
                btnSave_Click(null, null);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }


        public void ChangeTsslCurrentLocation(string str)
        {
            tsslCurrentLocation.Text = str;
        }

        private void autoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            autoToolStripMenuItem.Checked = !autoToolStripMenuItem.Checked;
            if (autoToolStripMenuItem.Checked)
            {

            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopMonitorMode();
            this.Close();
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Form f = FormSetting.GetInstance();
            f.Show();
            f.Activate();
        }

        private void stopMonitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClipboardMonitor.Stop();
        }

        #endregion

        #region QuickPasteEx
        public static void QuickPasteEx(string location, string fileName = null)
        {
            ManualResetEvent allDone = new ManualResetEvent(false);

            ClipboardData data = new ClipboardData(Clipboard.GetDataObject());
            data.SaveCompleted += () => allDone.Set();

            string[] extensions = data.Analyze();
            if (!String.IsNullOrEmpty(fileName))
            {
                string ext = Path.GetExtension(fileName);
                extensions = new string[1] { ext };
                if (Array.IndexOf(extensions, ext) == -1)
                {
                    // TODO
                    // maybe need some tips
                }
            }

            if (extensions.Length > 0)
            {
                // why the disk root directory has '"' ??
                if (location.LastIndexOf('"') == location.Length - 1)
                {
                    location = location.Substring(0, location.Length - 1);
                }
                string currentLocation = location.EndsWith("\\") ? location : location + "\\";

                string path = null;
                if (String.IsNullOrEmpty(fileName))
                {
                    path = currentLocation + GenerateFileName(currentLocation, extensions[0]) + "." + extensions[0];
                }
                else
                {
                    path = currentLocation + fileName;
                }

                Console.WriteLine(fileName);

                if (!Directory.Exists(currentLocation))
                {
                    Console.WriteLine(Resources.Strings.TipTargetPathNotExist);
                    MessageBox.Show(Resources.Strings.TipTargetPathNotExist,
                            Resources.Strings.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    if (File.Exists(path))
                    {
                        DialogResult result = MessageBox.Show(String.Format(Resources.Strings.TipTargetFileExisted, path),
                            Resources.Strings.Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            data.Save(path, extensions[0]);
                        }
                        else if (result == DialogResult.No)
                        {
                            return;
                        }
                    }
                    else
                    {
                        data.Save(path, extensions[0]);
                    }
                }
            }
            else
            {
                Console.WriteLine(Resources.Strings.TipAnalyzeFailedWithoutPrompt);
                MessageBox.Show(Resources.Strings.TipAnalyzeFailedWithoutPrompt,
                            Resources.Strings.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            allDone.WaitOne();
        }

        #endregion

        #region MonitorMode
        public static void StartMonitorMode()
        {
            if (dialogue == null)
            {
                dialogue = new FormMain();
            }

            // register the event that is fired after the key press.
            dialogue.hotkeyHook.KeyPressed += new EventHandler<KeyPressedEventArgs>(dialogue.btnSave_Click);
            try
            {
                dialogue.hotkeyHook.RegisterHotKey(Util.ModifierKeys.Control | Util.ModifierKeys.Alt, Keys.X);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            dialogue.data.SaveCompleted -= dialogue.Exit;
            dialogue.data.SaveCompleted += dialogue.ClipboardMonitor_OnPasteExSaveAsync;
            // start monitor
            ClipboardMonitor.OnClipboardChange += dialogue.ClipboardMonitor_OnClipboardChange;
            ClipboardMonitor.Start();

            // hide main window and display system tray icon
            dialogue.WindowState = FormWindowState.Minimized;
            dialogue.ShowInTaskbar = false;
            dialogue.Hide();
            dialogue.notifyIcon.Visible = true;
        }


        public static void StopMonitorMode()
        {
            if (dialogue == null)
            {
                MessageBox.Show("无法获取主界面");
                Application.Exit();
                return;
            }

            dialogue.hotkeyHook.UnregisterHotKey();

            dialogue.data.SaveCompleted -= dialogue.ClipboardMonitor_OnPasteExSaveAsync;
            dialogue.data.SaveCompleted += dialogue.Exit;
            ClipboardMonitor.OnClipboardChange -= dialogue.ClipboardMonitor_OnClipboardChange;
            ClipboardMonitor.Stop();

            //dialogue.Show();
            //dialogue.notifyIcon.Visible = false;
        }

        private string clipboardChangePath = null;

        private void ClipboardMonitor_OnClipboardChange()
        {
            data.IAcquisition  = Clipboard.GetDataObject();
            data.Storage = new DataObject();
            string[] exts = data.Analyze();
            if (exts.Length > 0)
            {
                String folder = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "User", "Temp") + "\\";
                clipboardChangePath = Path.Combine(folder, GenerateFileName(folder, exts[0]) + "." + exts[0]);

                ClipboardMonitor.Stop();
                data.SaveAsync(clipboardChangePath, exts[0]);
            }
        }

        private async void ClipboardMonitor_OnPasteExSaveAsync()
        {
            string[] paths = new string[] { clipboardChangePath };
            data.Storage.SetData(DataFormats.FileDrop, true, paths);
            await ThreadHelper.StartSTATask(() =>
            {
                Clipboard.SetDataObject(data.Storage, true);
            });
            ClipboardMonitor.Start();
        }

        private void Exit()
        {
            Application.Exit();
        }
        #endregion

        #region CollectionMode
        public static void StartCollectionMode()
        {

        }

        #endregion


    }
}
