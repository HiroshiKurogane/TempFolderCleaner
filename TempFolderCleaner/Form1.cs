using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TempFolderCleaner
{
    public partial class Form1 : Form
    {
        public string selectedfolder = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Text = "Cleaning...";

            long totalDeletedSize = 0; // Variable to accumulate the total deleted size

            // List of temp folder paths
            string[] tempFolders = {
                Path.GetTempPath(),  // System temp folder
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "INetCache"), // User temp folder
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"), // User temp folder
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), // Windows temp folder
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download"), // Windows Update temp files
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Downloads")
            };

            // Add additional temp folders based on checkbox selection
            if (checkBox2.Checked)
                tempFolders = tempFolders.Concat(new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Steam", "htmlcache") }).ToArray();
            if (checkBox3.Checked)
                tempFolders = tempFolders.Concat(new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicGamesLauncher", "Saved") }).ToArray();
            if (checkBox4.Checked)
                tempFolders = tempFolders.Concat(new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Battle.net", "Cache") }).ToArray();
            if (checkBox6.Checked)
                tempFolders = tempFolders.Concat(new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "WER") }).ToArray();
            if (checkBox7.Checked)
                tempFolders = tempFolders.Concat(new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Code", "Cache") }).ToArray();
            if (checkBox8.Checked)
            {
                tempFolders = tempFolders.Concat(new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "Cache"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "Local Storage"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "logs")
                }).ToArray();
            }

            if (checkBox9.Checked)
            {
                tempFolders = tempFolders.Concat(new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Default", "Cache"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User", "Data", "Default", "Cache"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Default", "Service Worker")
                }).ToArray();
            }

            // Clean the temp folders
            await Task.Run(() => totalDeletedSize = CleanTempFolders(tempFolders));

            // If checkbox9 is checked, check for duplicate files
            if (checkBox5.Checked && selectedfolder != null)
            {
                await Task.Run(() =>
                {
                        if (Directory.Exists(selectedfolder))
                        {
                            totalDeletedSize += RemoveDuplicateFiles(selectedfolder);
                        }
                });
            }

            button1.Text = "Clean Temp Folders";

            // Show the total deleted size in the MessageBox
            MessageBox.Show($"Temp folders cleaned successfully. Total deleted size: {totalDeletedSize / (1024 * 1024)} MB.");
        }

        private long CleanTempFolders(string[] tempFolders)
        {
            long totalDeletedSize = 0;

            foreach (string tempFolderPath in tempFolders)
            {
                if (Directory.Exists(tempFolderPath))
                {
                    try
                    {
                        // Delete all files in the temp folder
                        foreach (string file in Directory.GetFiles(tempFolderPath))
                        {
                            try
                            {
                                File.SetAttributes(file, FileAttributes.Normal); // Remove read-only attribute
                                FileInfo fileInfo = new FileInfo(file);
                                totalDeletedSize += fileInfo.Length;  // Add the size of the file
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to delete file '{file}': {ex.Message}");
                            }
                        }

                        // Delete all subdirectories and their contents in the temp folder
                        foreach (string directory in Directory.GetDirectories(tempFolderPath))
                        {
                            try
                            {
                                DirectoryInfo dirInfo = new DirectoryInfo(directory);
                                totalDeletedSize += GetDirectorySize(dirInfo);  // Add the size of the directory and its contents
                                Directory.Delete(directory, true);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to delete directory '{directory}': {ex.Message}");
                            }
                        }

                        Console.WriteLine($"All items in '{tempFolderPath}' have been deleted successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred in folder '{tempFolderPath}': {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Folder '{tempFolderPath}' does not exist.");
                }
            }

            return totalDeletedSize;  // Return the total deleted size
        }

        private long RemoveDuplicateFiles(string folderPath)
        {
            long deletedSize = 0;
            var fileHashes = new Dictionary<string, FileInfo>();

            try
            {
                foreach (var filePath in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        string hash = GetFileHash(filePath);

                        if (!string.IsNullOrEmpty(hash))
                        {
                            if (fileHashes.ContainsKey(hash))
                            {
                                // Delete duplicate file
                                deletedSize += fileInfo.Length;
                                fileInfo.Delete();
                                Console.WriteLine($"Deleted duplicate file: {filePath}");
                            }
                            else
                            {
                                fileHashes[hash] = fileInfo;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing duplicates in folder {folderPath}: {ex.Message}");
            }

            return deletedSize;
        }

        private string GetFileHash(string filePath)
        {
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hashing file {filePath}: {ex.Message}");
                return null;
            }
        }

        private long GetDirectorySize(DirectoryInfo directoryInfo)
        {
            long size = 0;

            // Add the size of all files in the directory
            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                size += file.Length;
            }

            // Add the size of all files in subdirectories
            foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories())
            {
                size += GetDirectorySize(subDirectory);  // Recursively calculate size of subdirectories
            }

            return size;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            checkBox1.Checked = true;
            checkBox2.Checked = true;
            checkBox3.Checked = true;
            checkBox4.Checked = true;
            checkBox6.Checked = true;
            checkBox7.Checked = true;
            checkBox8.Checked = true;
            checkBox9.Checked = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            checkBox3.Checked = false;
            checkBox4.Checked = false;
            checkBox6.Checked = false;
            checkBox7.Checked = false;
            checkBox8.Checked = false;
            checkBox9.Checked = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            label1.Text = folderBrowserDialog1.SelectedPath;
            selectedfolder = folderBrowserDialog1.SelectedPath;
        }
    }
}
