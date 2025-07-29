using Guna.UI2.WinForms;
using Helpers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Forms;

namespace GoldenEye_XBLA_Map_Updater
{
    public partial class Main : Form
    {
        private const string VERSION = "1.0.0";
        private const string API_URL = "https://api.github.com/repos/RetroDriven/GoldenEye-XBLA-Map-Updater/releases";
        private const string RELEASE_URL = "https://github.com/RetroDriven/GoldenEye-XBLA-Map-Updater/releases/latest";

        private const string Xenia_URL = "https://github.com/AdrianCassar/xenia-canary/releases/latest";
        private const string Graslu_URL = "https://youtu.be/jzpCpA-M_pU?si=wI5Z8X9ZtB8xizLt";
        private const string CE_URL = "https://goldeneyelive.com/GoldenEye-XBLA-CE.zip";
        private const string Save_URL = "http://updater.goldeneyelive.com/584108A9.zip";

        static readonly string TempDir = @"Maps_Temp";
        static readonly string LogsDir = @"RetroDriven_Logs\";
        static readonly string MapLink = "https://updater.goldeneyelive.com/RetroDriven_Maps.zip";
        static readonly string LogLink = "https://updater.goldeneyelive.com/Changelog.zip";
        static readonly string ChangeLog = LogsDir + "Changelog.txt";
        static private WebClient WebClient = null;

        public Main()
        {
            InitializeComponent();

            if (File.Exists(ChangeLog))
            {
                textBox1.Text = File.ReadAllText(ChangeLog);
            }

            if (Check_Internet())
            {
                //Check for App Updates
                try
                {
                    using (WebClient client2 = new WebClient())
                    {
                        _ = CheckVersion_Load();

                        Status.Text = "Checking for Map Updates...";
                        Download_Logs();
                    }
                }
                catch (Exception e)
                {
                    guna2MessageDialog1.Text = e.ToString();
                    guna2MessageDialog1.Parent = Form.ActiveForm;
                    guna2MessageDialog1.Show();
                }
            }
            else
            {
                No_Internet.Visible = true;
            }
        }
        public async Task CheckVersion_Load()
        {
            using (WebClient client = new WebClient())
            {
                if (await CheckVersion())
                {
                    try
                    {
                        Update_Available.Visible = true;
                        Update_Available.Enabled = true;
                    }
                    catch
                    {
                        No_Internet.Visible = true;
                    }
                }
            }
        }
        async static Task<bool> CheckVersion()
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(API_URL)

                };
                var agent = new ProductInfoHeaderValue("GoldenEye-XBLA-Map-Updater", "1.0");
                request.Headers.UserAgent.Add(agent);
                var response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                List<Helpers.Release>? releases = JsonSerializer.Deserialize<List<Helpers.Release>>(responseBody);

                string tag_name = releases[0].tag_name;
                string? v = Helpers.SemverUtil.FindSemver(tag_name);
                if (v != null)
                {
                    return Helpers.SemverUtil.SemverCompare(v, VERSION);
                    //return SemverUtil.SemverCompare(v, "1.0");

                }
                return false;
            }
            catch (HttpRequestException e)
            {
                Guna2MessageDialog Error = new();
                Error.Text = e.ToString();
                Error.Style = MessageDialogStyle.Dark;
                Error.Parent = Form.ActiveForm;
                Error.Show();
                return false;

            }
        }
        public static bool Check_Internet()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead("http://www.google.com"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public void Download_Logs()
        {
            //Clean Up
            if (Directory.Exists(@"C:\RetroDriven Map Packs"))
            {
                Directory.Delete(@"C:\RetroDriven Map Packs", true);
            }

            if (Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, true);
            }
            if (File.Exists(@"RetroDriven_Maps.zip"))
            {
                File.Delete(@"RetroDriven_Maps.zip");
            }

            System.IO.Directory.CreateDirectory(TempDir);

            WebClient = new WebClient();
            WebClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
            //WebClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);

            try
            {
                // Start downloading the file
                WebClient.DownloadFileAsync(new Uri(LogLink), TempDir + @"\Changelog.zip");
            }
            catch (HttpRequestException e)
            {
                Guna2MessageDialog Error = new();
                Error.Text = e.ToString();
                Error.Style = MessageDialogStyle.Dark;
                Error.Parent = Form.ActiveForm;
                Error.Show();
            }
        }
        public void DownloadUpdates()
        {
            WebClient = new WebClient();
            WebClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed2);
            //WebClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);

            try
            {
                // Start downloading the file
                WebClient.DownloadFileAsync(new Uri(MapLink), @"RetroDriven_Maps.zip");
                //toolStripStatusLabel3.Text = "Updates Found - Downloading Map Pack Files...";

                //if (File.Exists(ChangeLog))
                //{
                //    textBox1.Text = File.ReadAllText(ChangeLog);
                //    textBox1.Refresh();
                //}
            }
            catch (HttpRequestException e)
            {
                Guna2MessageDialog Error = new();
                Error.Text = e.ToString();
                Error.Style = MessageDialogStyle.Dark;
                Error.Parent = Form.ActiveForm;
                Error.Show();
            }

            if (Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, true);
            }
        }
        public void Completed(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                WebClient = null;

                System.Threading.Thread.Sleep(3000);

                //Unzip Map Log Files
                string zipFilePath = TempDir + @"\Changelog.zip";
                ZipFile.ExtractToDirectory(zipFilePath, TempDir);

                //Get Filename from Map Log
                IEnumerable<string> files = Directory.EnumerateFiles(TempDir + @"\RetroDriven_Logs\", "*.txt");
                string MapLogName = files.First();

                MapLogName = MapLogName.Replace(TempDir + @"\RetroDriven_Logs\", "");

                if (File.Exists(LogsDir + MapLogName))
                {
                    //Clean Up
                    if (Directory.Exists(TempDir))
                    {
                        Directory.Delete(TempDir, true);
                    }
                    //System.IO.File.Delete(TempDir + @"\Logs.zip");

                    //toolStripStatusLabel3.Text = "No Updates Found!";
                    Status.Text = "No Map Updates Found!";


                }
                else
                {
                    //toolStripStatusLabel3.Text = "Updates Found - Downloading Map Pack Files...";
                    Status.Text = "New Map Updates Found!";
                    guna2Button1.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Guna2MessageDialog Error = new();
                Error.Text = ex.ToString();
                Error.Style = MessageDialogStyle.Dark;
                Error.Parent = Form.ActiveForm;
                Error.Show();
            }
        }
        public void Completed2(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                WebClient = null;

                //Unzip Map Files
                string zipFilePath2 = @"RetroDriven_Maps.zip";
                string extractionPath2 = TempDir;
                ZipFile.ExtractToDirectory(zipFilePath2, extractionPath2);

                //Copy MAP Files
                string source_dir = TempDir;
                string destination_dir = Directory.GetCurrentDirectory();


                CopyFolder(source_dir, destination_dir);

                //Cleanup
                System.IO.File.Delete(@"RetroDriven_Maps.zip");

                if (Directory.Exists(TempDir))
                {
                    Directory.Delete(TempDir, true);
                }
                //System.IO.File.Delete(TempDir + @"\Logs.zip");

                Status.Text = "Updates Complete!";
                guna2Button1.Enabled = false;

                if (File.Exists(ChangeLog))
                {
                    textBox1.Text = File.ReadAllText(ChangeLog);
                    //textBox1.Refresh();
                }
            }
            catch (Exception ex)
            {
                Guna2MessageDialog Error = new();
                Error.Text = ex.ToString();
                Error.Style = MessageDialogStyle.Dark;
                Error.Parent = Form.ActiveForm;
                Error.Show();
            }
        }
        public void CopyFolder(string sourceFolder, string destFolder)
        {
            try
            {

                if (!Directory.Exists(destFolder))
                    Directory.CreateDirectory(destFolder);
                string[] files = Directory.GetFiles(sourceFolder);
                foreach (string file in files)
                {
                    string name = System.IO.Path.GetFileName(file);
                    string dest = System.IO.Path.Combine(destFolder, name);
                    File.Copy(file, dest, true);
                }
                string[] folders = Directory.GetDirectories(sourceFolder);
                foreach (string folder in folders)
                {
                    string name = System.IO.Path.GetFileName(folder);
                    string dest = System.IO.Path.Combine(destFolder, name);
                    CopyFolder(folder, dest);
                }
            }
            catch (Exception ex)
            {
                Guna2MessageDialog Error = new();
                Error.Text = ex.ToString();
                Error.Style = MessageDialogStyle.Dark;
                Error.Parent = Form.ActiveForm;
                Error.Show();
            }
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Graslu_URL,
                UseShellExecute = true,
            });
        }

        private void guna2Button5_Click(object sender, EventArgs e)
        {
            Process.Start("explorer", Xenia_URL);
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            Process.Start("explorer", CE_URL);
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            Process.Start("explorer", Save_URL);
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            guna2Button1.Enabled = false;
            Status.Text = "Downloading Map Updates...";
            DownloadUpdates();

        }

        private void Update_Available_Click(object sender, EventArgs e)
        {
            Process.Start("explorer", RELEASE_URL);
        }
    }
}
