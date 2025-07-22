using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QuickDelete
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            tbFile.Text = emptyFile;
        }
        string filepath = "";

        const string emptyFile = "请选择文件夹";

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog fbd = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            if (fbd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (!string.IsNullOrEmpty(fbd.FileName))
                {
                    tbFile.Text = fbd.FileName;
                    if (!tbFile.Text.EndsWith("\\"))
                    {
                        tbFile.Text = fbd.FileName + "\\";
                    }
                    btnStart.IsEnabled = true;
                    filepath = tbFile.Text;
                }
            }
            else
            {
                tbFile.Text = emptyFile;
                btnStart.IsEnabled = false;
                filepath = "";
            }
            //System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            //fbd.ShowDialog();
            //if (!string.IsNullOrEmpty(fbd.SelectedPath))
            //{
            //    tbFile.Text = fbd.SelectedPath;
            //    if (!tbFile.Text.EndsWith("\\"))
            //    {
            //        tbFile.Text = fbd.SelectedPath + "\\";
            //    }
            //    btnStart.IsEnabled = true;
            //    filepath = tbFile.Text;
            //}
            //else
            //{
            //    tbFile.Text = emptyFile;
            //    btnStart.IsEnabled = false;
            //    filepath = "";
            //}

        }

        void DeleteBack(string path, bool isFile, bool succeed, uint count)
        {
            //if (!succeed)
            //{
            //    return;
            //}
            if (!isFile && !path.EndsWith("\\"))
            {
                path += "\\";
            }
            string s = string.Format("[{0}]({2}) {1}", succeed ? "成功" : "失败", path, count);
            appendText(s);
        }

        private static void DeleteDir(string dir, bool deleteSub, DeleteCallback back, ref uint count)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(dir);
                FileInfo[] fis = directoryInfo.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    count++;
                    try
                    {
                        fi.Delete();
                        back?.Invoke(fi.FullName, true, true, count);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        back?.Invoke(fi.FullName, true, false, count);
                    }
                }
                DirectoryInfo[] dis = directoryInfo.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    DeleteDir(di.FullName, deleteSub, back, ref count);
                }
                try
                {
                    directoryInfo.Delete();
                    back?.Invoke(directoryInfo.FullName, false, true, count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    back?.Invoke(directoryInfo.FullName, false, false, count);
                }
                count++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        delegate void DeleteCallback(string path, bool isFile, bool succeed, uint count);

        void appendText(string str)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                rtbInfo.AppendText(str + Environment.NewLine);
                //if (rtbInfo.VerticalOffset > 0.9)
                //{
                rtbInfo.ScrollToEnd();
                //}
                if (rtbInfo.LineCount > 5000)
                {
                    rtbInfo.Clear();
                }
            }), null);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBoxResult r = System.Windows.MessageBox.Show("文件会被直接删除，无法恢复，确定？", "提示", MessageBoxButton.YesNo);
            if (r != MessageBoxResult.Yes)
            {
                return;
            }
            btnStart.IsEnabled = false;
            btnFile.IsEnabled = false;

            ThreadPool.QueueUserWorkItem(new WaitCallback((x) =>
            {
                appendText(string.Format("[开始] {0}", filepath));
                uint t = 0;
                DeleteDir(filepath, true, DeleteBack, ref t);
                appendText(string.Format("[结束] {0}", filepath));
                this.Dispatcher.Invoke(new Action(() =>
                {
                    btnStart.IsEnabled = true;
                    btnFile.IsEnabled = true;
                }));
            }));
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string msg = ((System.Array)e.Data.GetData(System.Windows.DataFormats.FileDrop)).GetValue(0).ToString();
                if (Directory.Exists(msg) && btnFile.IsEnabled)
                {
                    tbFile.Text = msg;
                    filepath = msg;
                    btnStart.IsEnabled = true;
                }
            }
        }
    }
}
