using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;


namespace RencodeMP4
{
    public partial class Form1 : Form
    {
        public static string path = "";
        public static string appplicationPath = "";

        public Form1()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();

            appplicationPath = Application.StartupPath
                .ToLower();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bw.RunWorkerAsync();
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            DialogResult dr = fbd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                Environment.SpecialFolder root = fbd.RootFolder;
                path = fbd.SelectedPath;
                lblSelectedFolder.Text = path;
            }
        }

        private void btnEncode_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(Encode);
        }

        private void Encode(object state)
        {
            string result = string.Empty;

            if (!string.IsNullOrWhiteSpace(appplicationPath) && !string.IsNullOrWhiteSpace(path))
            {
                var logFile = appplicationPath.Replace("\\bin\\debug", "") + "\\log.txt";
                var ffmpegPath = appplicationPath + "\\tools\\ffmpeg.exe";
                var qtPath = appplicationPath + "\\tools\\qt-faststart.exe";

                var log = "";

                var start = DateTime.Now;
                log += " Start time: " + start + Environment.NewLine + Environment.NewLine;

                var di = new DirectoryInfo(path);
                var list = di.GetFiles()
                    .Where(x => x.Extension == ".mp4")
                    .ToList();

                log += " Total video count: " + list.Count + Environment.NewLine + Environment.NewLine;
                log += " ---------------------" + Environment.NewLine;

                this.Invoke(new Action(() => this.progressBar1.Minimum = 0));
                this.Invoke(new Action(() => this.progressBar1.Maximum = list.Count));

                var i = 1;
                foreach (var item in list)
                {
                    this.Invoke(new Action(() => this.lblInfo.Text = i + ". video is re-encoding"));

                    var input = item.DirectoryName + "\\" + item.Name;
                    var output = item.DirectoryName + "\\" + item.Name.Replace(".mp4", "") + "-encoded" + ".mp4";

                    log += " Input: " + input + Environment.NewLine;
                    log += " Output: " + output + Environment.NewLine;
                    log += " Cmd: " + ffmpegPath + " -i \"" + input + "\"" + " -c:v libx264 -crf 20 -preset medium -c:a aac -strict experimental -b:a 192k -ac 2 -movflags faststart " + "\"" + output + "\"" + Environment.NewLine;

                    try
                    {
                        #region re-encoding file

                        var pi = new ProcessStartInfo(ffmpegPath, " -i \"" + input + "\"" + " -c:v libx264 -crf 18 -preset fast -c:a aac -strict experimental -b:a 192k -ac 2 -movflags faststart " + "\"" + output + "\"")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        var process = System.Diagnostics.Process.Start(pi);
                        result = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        process.Close();

                        log += " Re-encoding is done." + Environment.NewLine;

                        #endregion re-encoding file

                        var file = new FileInfo(input);
                        if (file.Exists)
                            file.Delete();

                        log += " Output file is created and input file is deleted." + Environment.NewLine;

                        #region moving atom file to start point

                        var isMoved = false;

                        pi = new ProcessStartInfo(qtPath, " \"" + output + "\"" + " " + "\"" + input + "\"")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        process = System.Diagnostics.Process.Start(pi);
                        result = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        process.Close();

                        if (!result.Contains("last atom in file was not a moov atom"))
                        {
                            isMoved = true;
                            log += " Moov atom successfuly moved and input is recreated." + Environment.NewLine;
                        }
                        else
                        {
                            log += " Last atom in file was not a moov atom." + Environment.NewLine;
                        }

                        #endregion moving atom file to start point

                        file = new FileInfo(output);
                        if (file.Exists && isMoved)
                        {
                            file.Delete();
                            log += " Output file is deleted." + Environment.NewLine;
                        }
                        else
                        {
                            log += " Last atom in file was not a moov atom, so just input is renamed." + Environment.NewLine;
                        }
                    }
                    catch (Exception ex)
                    {
                        string error = ex.Message;
                        log += " Error occured: " + error + Environment.NewLine;
                    }

                    this.Invoke(new Action(() => this.progressBar1.Value = i));
                    i++;

                    log += Environment.NewLine + " ********************************** " + Environment.NewLine;

                    Thread.Sleep(10);
                }

                var end = DateTime.Now;
                log += " End time: " + end + Environment.NewLine;

                log += " Total time: " + (end - start).Hours + " hour - " + (end - start).Minutes + " min" + Environment.NewLine + Environment.NewLine + Environment.NewLine + Environment.NewLine;
                File.WriteAllText(logFile, log);

                MessageBox.Show("Convertion is done. Please check files.");
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            var processes = Process.GetProcesses()
                .Where(x => x.ProcessName == "ffmpeg")
                .ToList();

            foreach (var process in processes)
            {
                process.Kill();
            }
        }
    }
}