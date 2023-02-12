using Microsoft.Win32;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BenderMF
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        public MainWindow()
        {

            InitializeComponent();
            btxPath.Text = ConfigurationManager.AppSettings["JavaScriptFilePath"];
            ExtractIP();
            LoadWindowLocation();
            btxPort.PreviewTextInput += btxPort_PreviewTextInput;


        }

        private bool isWindowExpanded = false;
        private double normalWidth;
        private double normalHeight;

        private void btxPort_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                e.Handled = true;
            }
        }


        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender == btxHost)
            {
                ButtonHostSave.Visibility = Visibility.Visible;
            }
            else if (sender == btxPort)
            {
                ButtonPortSave.Visibility = Visibility.Visible;
            }
            else if (sender == btxUsername)
            {
                ButtonUsernameSave.Visibility = Visibility.Visible;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == btxHost)
            {
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 15);
                timer.Start();
                timer.Tick += (s, args) =>
                {
                    ButtonHostSave.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
            }
            else if (sender == btxPort)
            {
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 15);
                timer.Start();
                timer.Tick += (s, args) =>
                {
                    ButtonPortSave.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
            }
            else if (sender == btxUsername)
            {
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 15);
                timer.Start();
                timer.Tick += (s, args) =>
                {
                    ButtonUsernameSave.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
            }
        }


        private void SaveWindowLocation()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Remove("WindowLocation");
            config.AppSettings.Settings.Add("WindowLocation", this.Left + "," + this.Top);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void LoadWindowLocation()
        {
            string windowLocation = ConfigurationManager.AppSettings["WindowLocation"];
            if (!string.IsNullOrEmpty(windowLocation))
            {
                string[] location = windowLocation.Split(',');
                this.Left = int.Parse(location[0]);
                this.Top = int.Parse(location[1]);
            }
        }

        private void ExitButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SaveWindowLocation();
            Close();
        }

        private void MinButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ToolBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {

                DragMove();
            }

        }


        private void buttonPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JavaScript File (*.js)|*.js|All Files (*.*)|*.*";
            openFileDialog.InitialDirectory = ConfigurationManager.AppSettings["JavaScriptFilePath"];
            if (openFileDialog.ShowDialog() == true)
            {
                btxPath.Text = openFileDialog.FileName;
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["JavaScriptFilePath"].Value = openFileDialog.FileName;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                ExtractIP();
            }
        }
        private System.Diagnostics.Process process;
        private StringBuilder output = new StringBuilder();

        private bool botRunning = false;
        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(btxPath.Text))
            {
                MessageBox.Show("Please select the scripts.");
                return;
            }

            string scriptPath = btxPath.Text;

            if (botRunning)
            {
                if (process != null)
                {
                    process.Kill();
                    process = null;
                }
                botRunning = false;
                ButtonStart.Content = "Start";
                return;
            }

            var runningProcesses = Process.GetProcessesByName("node.exe");
            if (runningProcesses.Length > 0)
            {
                MessageBox.Show("A Node process is already running. Please stop it before starting a new one.");
                return;
            }

            process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "node.exe";
            process.StartInfo.Arguments = scriptPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            output = new StringBuilder();

            process.OutputDataReceived += (sender2, e2) =>
            {
                if (!string.IsNullOrEmpty(e2.Data))
                {
                    output.AppendLine(e2.Data);
                    this.Dispatcher.Invoke(() => RichTextBoxLog.AppendText(e2.Data + Environment.NewLine));
                }
            };

            process.ErrorDataReceived += (sender2, e2) =>
            {
                if (!string.IsNullOrEmpty(e2.Data))
                {
                    output.AppendLine(e2.Data);
                    this.Dispatcher.Invoke(() => RichTextBoxLog.AppendText(e2.Data + Environment.NewLine));
                }
            };

            process.EnableRaisingEvents = true;
            process.Exited += (sender2, e2) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (process != null && process.ExitCode != 0)
                    {
                        RichTextBoxLog.AppendText($"Process exited with error code {process.ExitCode}." + Environment.NewLine);
                    }
                    else
                    {
                        RichTextBoxLog.AppendText("Process exited." + Environment.NewLine);
                    }
                    botRunning = false;
                    ButtonStart.Content = "Start";
                });
            };

            Task.Run(() =>
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            });

            botRunning = true;

            ButtonStart.Content = "Stop";

        }






        private void ButtonHostSave_Click(object sender, EventArgs e)
        {
            string filePath = ConfigurationManager.AppSettings["JavaScriptFilePath"];
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                string pattern1 = @"host\s*:\s*'(.*?)'";
                Match match1 = Regex.Match(fileContent, pattern1);
                if (match1.Success)
                {
                    string newFileContent = Regex.Replace(fileContent, pattern1, "host: '" + btxHost.Text + "'");
                    File.WriteAllText(filePath, newFileContent);
                    MessageBox.Show("Host save successfully.");
                }
                else
                {
                    string pattern2 = @"host\s*:\s*""(.*?)""";
                    Match match2 = Regex.Match(fileContent, pattern2);
                    if (match2.Success)
                    {
                        string newFileContent = Regex.Replace(fileContent, pattern2, "host: \"" + btxHost.Text + "\"");
                        File.WriteAllText(filePath, newFileContent);
                        MessageBox.Show("Host save successfully");
                    }
                    else
                    {
                        MessageBox.Show("Host line not found in the file.");
                    }
                }
                ButtonHostSave.Visibility = Visibility.Collapsed;
            }
        }
        private void ButtonPortSave_Click(object sender, EventArgs e)
        {
            string filePath = ConfigurationManager.AppSettings["JavaScriptFilePath"];
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                string pattern1 = @"port\s*:\s*(\d+)";
                Match match1 = Regex.Match(fileContent, pattern1);
                if (match1.Success)
                {
                    string newFileContent = Regex.Replace(fileContent, pattern1, "port: " + btxPort.Text);
                    File.WriteAllText(filePath, newFileContent);
                    MessageBox.Show("Port save successfully.");
                }
                else
                {
                    string pattern2 = @"port\s*:\s*""(\d+)""";
                    Match match2 = Regex.Match(fileContent, pattern2);
                    if (match2.Success)
                    {
                        string newFileContent = Regex.Replace(fileContent, pattern2, "port: \"" + btxPort.Text + "\"");
                        File.WriteAllText(filePath, newFileContent);
                        MessageBox.Show("Port save successfully.");
                    }
                    else
                    {
                        string pattern3 = @"port\s*:\s*'(\d+)'";
                        Match match3 = Regex.Match(fileContent, pattern3);
                        if (match3.Success)
                        {
                            string newFileContent = Regex.Replace(fileContent, pattern3, "port: '" + btxPort.Text + "'");
                            File.WriteAllText(filePath, newFileContent);
                            MessageBox.Show("Port save successfully.");
                        }
                        else
                        {
                            MessageBox.Show("Port line not found in the file.");
                        }

                    }
                }
                ButtonPortSave.Visibility = Visibility.Collapsed;
            }
        }
        private void ButtonUsernameSave_Click(object sender, EventArgs e)
        {
            string filePath = ConfigurationManager.AppSettings["JavaScriptFilePath"];
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                string pattern1 = @"username\s*:\s*'(.*?)'";
                Match match1 = Regex.Match(fileContent, pattern1);
                if (match1.Success)
                {
                    string newFileContent = Regex.Replace(fileContent, pattern1, "username: '" + btxUsername.Text + "'");
                    File.WriteAllText(filePath, newFileContent);
                    MessageBox.Show("Username save successfully.");
                }
                else
                {
                    string pattern2 = @"username\s*:\s*""(.*?)""";
                    Match match2 = Regex.Match(fileContent, pattern2);
                    if (match2.Success)
                    {
                        string newFileContent = Regex.Replace(fileContent, pattern2, "username: \"" + btxUsername.Text + "\"");
                        File.WriteAllText(filePath, newFileContent);
                        MessageBox.Show("Username save successfully.");
                    }
                    else
                    {
                        MessageBox.Show("Username line not found in the file.");
                    }
                }
                ButtonUsernameSave.Visibility = Visibility.Collapsed;

            }

        }


        private void ExtractIP()
        {
            string filePath = ConfigurationManager.AppSettings["JavaScriptFilePath"];
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                string ipPattern1 = @"host\s*:\s*'(.*?)'";
                Match ipMatch1 = Regex.Match(fileContent, ipPattern1);
                if (ipMatch1.Success)
                {
                    btxHost.Text = ipMatch1.Groups[1].Value;
                }
                else
                {
                    string ipPattern2 = @"host\s*:\s*""(.*?)""";
                    Match ipMatch2 = Regex.Match(fileContent, ipPattern2);
                    if (ipMatch2.Success)
                    {
                        btxHost.Text = ipMatch2.Groups[1].Value;
                    }
                    else
                    {
                        MessageBox.Show("Host line not found in the file.");
                    }
                }
                File.WriteAllText(filePath, fileContent);
                string portPattern1 = @"port\s*:\s*'(.*?)'";
                Match portMatch1 = Regex.Match(fileContent, portPattern1);
                if (portMatch1.Success)
                {
                    btxPort.Text = portMatch1.Groups[1].Value;
                }
                else
                {
                    string portPattern2 = @"port\s*:\s*""(.*?)""";
                    Match portMatch2 = Regex.Match(fileContent, portPattern2);
                    if (portMatch2.Success)
                    {
                        btxPort.Text = portMatch2.Groups[1].Value;
                    }
                    else
                    {
                        string portPattern3 = @"port\s*:\s*(\d+)";
                        Match portMatch3 = Regex.Match(fileContent, portPattern3);
                        if (portMatch3.Success)
                        {
                            btxPort.Text = portMatch3.Groups[1].Value;
                        }
                        else
                        {
                            MessageBox.Show("Port line not found in the file.");
                        }
                    }

                }

                string usernamePattern1 = @"username\s*:\s*'(.*?)'";
                Match usernameMatch1 = Regex.Match(fileContent, usernamePattern1);
                if (usernameMatch1.Success)
                {
                    btxUsername.Text = usernameMatch1.Groups[1].Value;
                }
                else
                {
                    string usernamePattern2 = @"username\s*:\s*""(.*?)""";
                    Match usernameMatch2 = Regex.Match(fileContent, usernamePattern2);
                    if (usernameMatch2.Success)
                    {
                        btxUsername.Text = usernameMatch2.Groups[1].Value;
                    }
                    else
                    {
                        MessageBox.Show("Username line not found in the file.");
                    }
                }

                btxHost.InvalidateVisual();
                btxPort.InvalidateVisual();
                btxUsername.InvalidateVisual();
            }


        }

        private void buttonLog_Click(object sender, RoutedEventArgs e)
        {
            if (!isWindowExpanded)
            {
                
                normalWidth = this.Width;
                normalHeight = this.Height;

                
                this.Width = 365;
                this.Height = 515;

                isWindowExpanded = true;
            }
            else
            {
                
                this.Width = normalWidth;
                this.Height = normalHeight;

                isWindowExpanded = false;
            }
        }
        





    }
}
