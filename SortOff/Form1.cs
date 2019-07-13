using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SortOff
{
    public enum SizeFormat
    {
        Bytes = 0,
        KiloBytes = 1,
        MegaBytes = 2,
        GigaBytes = 3,
    }

    public partial class Form1 : Form
    {
        // the Stopwatch will monitor the time taken to process the files.
        Stopwatch stp = new Stopwatch();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (BGWorker.IsBusy)
            {
                BGWorker.CancelAsync();

            }
            Application.Exit();
        }

        // Reset Settings
        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Text = "C:\\";
            richTextBox1.Clear();
            slideButton1.IsOn = slideButton2.IsOn = true;
            slideButton3.IsOn = false;
        }

        // Start Operation
        private void button2_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(textBox1.Text.Trim()) == false) { MessageBox.Show("Invalid Directory Name or Path. Please Select a Valid Directory."); return;  }
            if (textBox1.Text == "C:\\") { MessageBox.Show("Invalid Directory Name or Path. Please Select a Valid Directory."); return;  }

            richTextBox1.Clear();
            circularProgressBar1.Value = 0;
            circularProgressBar1.Text = "0%";
            circularProgressBar1.Refresh();
            richTextBox1.AppendText("Process Started At : " + System.DateTime.Now.ToLocalTime().ToString() + '\n');
            stp.Start();
            BGWorker.RunWorkerAsync();
        }

        private void LogData()
        {   // In case of Errors, We won't break the program. Instead we will intimate the user with the error message. 
            try
            {
                StreamWriter str = File.CreateText(Application.StartupPath + "\\Stats.log");
                str.Write(richTextBox1.Text);
                str.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error : " + ex.Message);
            }

        }

        private void BGWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            circularProgressBar1.Value = e.ProgressPercentage;
            circularProgressBar1.Text = e.ProgressPercentage.ToString() + "%";
            circularProgressBar1.Refresh();
            Application.DoEvents();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select the Messed up directory";
                fbd.RootFolder = Environment.SpecialFolder.Desktop;
                fbd.ShowNewFolderButton = false;
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    textBox1.Text = fbd.SelectedPath;
                    textBox1.ScrollToCaret();
                }
            }
        }

        private void BGWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (textBox1.Text.Trim() == "") { BGWorker.CancelAsync(); MessageBox.Show("Cannot work on an empty directory. Select a valid Directory first."); }
            int i = 0;
            int count = 0;

            string[] files = { };

            // Obtain all the files from the selected directory
            if (slideButton3.IsOn) { files = System.IO.Directory.GetFiles(textBox1.Text.Trim(), "*.*", SearchOption.AllDirectories); }
            else { files = System.IO.Directory.GetFiles(textBox1.Text.Trim(), "*.*", SearchOption.TopDirectoryOnly); }

            // Get File Size
            long totalsize = GetSizeOfAllFiles(files.ToList<string>());
            long processedSize = 0;

            // UI Update
            int len = files.Length;
            richTextBox1.AppendText("Total Files Scanned : " + len + '\n');

            // Cycle through each filter to obtain Group specific files from the directory.
            for (i = 0; i < listView1.Items.Count - 1; i++)
            {
                // Split the filters into individual filter
                string[] filters = listView1.Items[i].SubItems[1].Text.Split(',');

                // Obtain the folder name from the filter group
                string folder = listView1.Items[i].Text.Trim();

                // Generate the complete folder path Dynamically
                string split_path = textBox1.Text.Trim() + "\\Arranged\\" + folder;

                // loop through each file to find the files that need to be split
                foreach (string fs in files)
                {
                    // Get extension of file without the DOT(.)
                    string ext = Path.GetExtension(fs).TrimStart('.').ToLower();

                    // If the file is suitable for processing then proceed for the process.
                    if (IsPresent(filters, ext))
                    {
                        // Validate the directory before starting file operation
                        // This is done here, so that no useless/empty folders are created. 
                        if (Directory.Exists(split_path) == false)
                        {
                            Directory.CreateDirectory(split_path);
                            richTextBox1.AppendText("Created Folder : " + folder + '\n');
                        }

                        processedSize += GetSizeOfFile(fs);

                        if (slideButton1.IsOn)
                        { File.Move(fs, split_path + "\\" + Path.GetFileName(fs)); }
                        else
                        { File.Copy(fs, split_path + "\\" + Path.GetFileName(fs)); }

                        count++;

                        // Progress Update
                        float prg = ((processedSize / totalsize) * 100);
                        BGWorker.ReportProgress((int) prg);
                    }
                }

                // UI Update
                richTextBox1.AppendText(folder + " : " + count + '\n');
                count = 0;
            }
        }

        private long GetSizeOfAllFiles(List<string> files)
        {
            long size = 0;
            foreach(string f in files)
            {
                size += GetSizeOfFile(f);
            }

            return size;
        }

        private long GetSizeOfFile(string file)
        {
            var fo = new FileInfo(file);
            return fo.Length;
        }

        private bool IsPresent(IEnumerable<string> er, string comp)
        {
            foreach (string elem in er)
            {
                int res = string.Compare(elem.Trim(), comp, true);
                if (res == 0) { return true; }
            }
            return false;
        }

        private void BGWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stp.Stop();
            richTextBox1.AppendText("Process Completed at : " + System.DateTime.Now.ToLocalTime().ToString() + '\n');
            richTextBox1.AppendText("Process Completed in : " + stp.Elapsed.ToString() + '\n');
            LogData();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://antifixofficial.wixsite.com/Antifix");
        }

        #region Movement

        bool IsDrag = false;
        int m_X, m_Y;

        [DebuggerStepThroughAttribute]
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            IsDrag = true;
            m_X = System.Windows.Forms.Cursor.Position.X - this.Left;
            m_Y = System.Windows.Forms.Cursor.Position.Y - this.Top;
        }

        [DebuggerStepThroughAttribute]
        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsDrag)
            {
                this.Left = System.Windows.Forms.Cursor.Position.X - m_X;
                this.Top = System.Windows.Forms.Cursor.Position.Y - m_Y;
            }
        }

        [DebuggerStepThroughAttribute]
        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            IsDrag = false;
        }
        #endregion

        private void button5_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            circularProgressBar1.Value = 0;
            circularProgressBar1.Text = "0%";
            circularProgressBar1.Refresh();
        }

    }
}
