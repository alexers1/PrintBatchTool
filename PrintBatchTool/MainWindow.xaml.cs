using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing; // 核心打印库
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
// 解决 CheckBox 引用冲突，明确指定使用 WPF 的 CheckBox
using CheckBox = System.Windows.Controls.CheckBox;

namespace BatchPrinterTool
{
    public partial class MainWindow : Window
    {
        // 目标默认打印机
        private const string TargetPrinterName = @"\\svzf2080\PRZF0001";
        private PrintQueue _selectedPrintQueue;
        private string _selectedRootFolder;
        private List<string> _allScannedFiles = new List<string>(); // 扫描到的所有文件

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CheckPrinterConnection();
        }

        #region 1. 打印机检测逻辑

        private async void CheckPrinterConnection()
        {
            TxtCurrentPrinter.Text = "正在连接 " + TargetPrinterName + "...";

            await Task.Run(() =>
            {
                try
                {
                    // 尝试查找指定打印机
                    var server = new LocalPrintServer();
                    var queues = server.GetPrintQueues();

                    // 模糊匹配或者精确匹配
                    var target = queues.FirstOrDefault(p => p.FullName.Contains(TargetPrinterName) || p.Name.Contains("PRZF0001"));

                    Dispatcher.Invoke(() =>
                    {
                        if (target != null)
                        {
                            _selectedPrintQueue = target;
                            TxtCurrentPrinter.Text = target.FullName + " (连接成功)";
                            TxtCurrentPrinter.Foreground = System.Windows.Media.Brushes.Green;
                            BtnChangePrinter.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            TxtCurrentPrinter.Text = "连接失败，请手动选择。";
                            TxtCurrentPrinter.Foreground = System.Windows.Media.Brushes.Red;
                            LoadInstalledPrinters();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtCurrentPrinter.Text = "错误: " + ex.Message;
                        LoadInstalledPrinters();
                    });
                }
            });
        }

        private void LoadInstalledPrinters()
        {
            BtnChangePrinter.Visibility = Visibility.Collapsed;
            PanelPrinterSelect.Visibility = Visibility.Visible;

            try
            {
                var server = new LocalPrintServer();
                var queues = server.GetPrintQueues();
                ComboPrinters.ItemsSource = queues;
                ComboPrinters.DisplayMemberPath = "FullName"; // 显示完整名称

                // 尝试选中默认打印机
                var defaultPrinter = LocalPrintServer.GetDefaultPrintQueue();
                foreach (PrintQueue q in ComboPrinters.Items)
                {
                    if (q.FullName == defaultPrinter.FullName)
                    {
                        ComboPrinters.SelectedItem = q;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取打印机列表失败：" + ex.Message);
            }
        }

        private void BtnChangePrinter_Click(object sender, RoutedEventArgs e)
        {
            LoadInstalledPrinters();
        }

        private void ComboPrinters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboPrinters.SelectedItem is PrintQueue queue)
            {
                _selectedPrintQueue = queue;
                TxtCurrentPrinter.Text = queue.FullName;
                TxtCurrentPrinter.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        #endregion

        #region 2. 文件扫描逻辑

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            // 使用 System.Windows.Forms 的 FolderBrowserDialog
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                _selectedRootFolder = dialog.SelectedPath;
                TxtFolderPath.Text = _selectedRootFolder;
                ScanFiles();
            }
        }

        private void ScanFiles()
        {
            if (string.IsNullOrEmpty(_selectedRootFolder)) return;

            string[] validExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".ppt", ".pptx" };
            _allScannedFiles.Clear();

            try
            {
                // 递归获取所有文件
                var files = Directory.GetFiles(_selectedRootFolder, "*.*", SearchOption.AllDirectories);

                // 筛选
                foreach (var file in files)
                {
                    if (validExtensions.Contains(Path.GetExtension(file).ToLower()))
                    {
                        _allScannedFiles.Add(file);
                    }
                }

                UpdateCounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("扫描文件出错: " + ex.Message);
            }
        }

        private void UpdateCounts()
        {
            // 辅助函数：统计特定后缀数量
            int CountExt(string ext) => _allScannedFiles.Count(f => Path.GetExtension(f).ToLower() == ext);

            UpdateCheckLabel(ChkPdf, ".pdf", CountExt(".pdf"));
            UpdateCheckLabel(ChkDoc, ".doc", CountExt(".doc"));
            UpdateCheckLabel(ChkDocx, ".docx", CountExt(".docx"));
            UpdateCheckLabel(ChkXls, ".xls", CountExt(".xls"));
            UpdateCheckLabel(ChkXlsx, ".xlsx", CountExt(".xlsx"));
            UpdateCheckLabel(ChkTxt, ".txt", CountExt(".txt"));
            UpdateCheckLabel(ChkPpt, ".ppt", CountExt(".ppt"));
            UpdateCheckLabel(ChkPptx, ".pptx", CountExt(".pptx"));

            // 每次重新扫描清空待打印列表
            ListFilesToPrint.Items.Clear();
            UpdatePrintButtonState();
        }

        private void UpdateCheckLabel(CheckBox chk, string ext, int count)
        {
            chk.Content = $"{ext.ToUpper().Replace(".", "")} ({count})";
            chk.IsChecked = false; // 重置选择
        }

        #endregion

        #region 3. 文件选择逻辑

        private void ExtensionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            RefreshPrintListFromChecks();
        }

        private void RefreshPrintListFromChecks()
        {
            ListFilesToPrint.Items.Clear();

            AddFilesIfChecked(ChkPdf);
            AddFilesIfChecked(ChkDoc);
            AddFilesIfChecked(ChkDocx);
            AddFilesIfChecked(ChkXls);
            AddFilesIfChecked(ChkXlsx);
            AddFilesIfChecked(ChkTxt);
            AddFilesIfChecked(ChkPpt);
            AddFilesIfChecked(ChkPptx);

            UpdatePrintButtonState();
        }

        private void AddFilesIfChecked(CheckBox chk)
        {
            if (chk.IsChecked == true)
            {
                string ext = chk.Tag.ToString();
                var files = _allScannedFiles.Where(f => Path.GetExtension(f).ToLower() == ext);
                foreach (var f in files)
                {
                    if (!ListFilesToPrint.Items.Contains(f))
                        ListFilesToPrint.Items.Add(f);
                }
            }
        }

        private void BtnAddManualFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Office Files & PDF|*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx;*.pdf;*.txt";

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    if (!ListFilesToPrint.Items.Contains(filename))
                    {
                        ListFilesToPrint.Items.Add(filename);
                    }
                }
                UpdatePrintButtonState();
            }
        }

        private void BtnClearList_Click(object sender, RoutedEventArgs e)
        {
            ListFilesToPrint.Items.Clear();
            UncheckAll();
            UpdatePrintButtonState();
        }

        private void UncheckAll()
        {
            ChkPdf.IsChecked = ChkDoc.IsChecked = ChkDocx.IsChecked = false;
            ChkXls.IsChecked = ChkXlsx.IsChecked = ChkTxt.IsChecked = false;
            ChkPpt.IsChecked = ChkPptx.IsChecked = false;
        }

        private void UpdatePrintButtonState()
        {
            BtnStartPrint.IsEnabled = ListFilesToPrint.Items.Count > 0 && _selectedPrintQueue != null;
            BtnStartPrint.Content = _selectedPrintQueue == null ? "请先选择打印机" : $"开始打印 ({ListFilesToPrint.Items.Count} 个文件)";
        }

        #endregion

        #region 4. 打印执行逻辑

        private async void BtnStartPrint_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPrintQueue == null) return;

            var files = ListFilesToPrint.Items.Cast<string>().ToList();
            if (files.Count == 0) return;

            // 1. 提前在 UI 线程获取参数
            string targetPrinterName = _selectedPrintQueue.FullName;
            bool isDuplex = ComboDuplex.SelectedIndex > 0;
            Duplexing duplexMode = ComboDuplex.SelectedIndex == 1 ? Duplexing.TwoSidedLongEdge : (ComboDuplex.SelectedIndex == 2 ? Duplexing.TwoSidedShortEdge : Duplexing.OneSided);
            bool isColor = ComboColor.SelectedIndex == 1;

            // 获取是否开启交互模式
            bool isInteractive = ChkInteractiveMode.IsChecked == true;

            BtnStartPrint.IsEnabled = false;

            await Task.Run(() =>
            {
                try
                {
                    // 2. 关键修改：在后台线程重新获取 PrintQueue 实例
                    using (var server = new LocalPrintServer())
                    {
                        var threadSafeQueue = server.GetPrintQueue(targetPrinterName);

                        // 修改打印设置
                        var newTicket = threadSafeQueue.UserPrintTicket;
                        newTicket.Duplexing = duplexMode;
                        newTicket.OutputColor = isColor ? OutputColor.Color : OutputColor.Monochrome;

                        threadSafeQueue.UserPrintTicket = newTicket;
                        threadSafeQueue.Commit();

                        // 3. 循环打印
                        int count = 0;
                        bool stopPrinting = false; // 添加停止标志

                        foreach (string filePath in files)
                        {
                            if (stopPrinting) break;

                            string fileName = Path.GetFileName(filePath);
                            Dispatcher.Invoke(() => BtnStartPrint.Content = $"正在打印 ({++count}/{files.Count})...");

                            // 传入线程安全的队列名称
                            PrintSingleFile(filePath, targetPrinterName);

                            // ---- 核心修改开始：检测交互模式 ----
                            if (isInteractive)
                            {
                                // 切换回 UI 线程显示弹窗，这会阻塞当前后台循环，直到用户点击按钮
                                Dispatcher.Invoke(() =>
                                {
                                    var result = MessageBox.Show(
                                        $"文件 '{fileName}' 已发送到打印机。\n\n如果是 PDF/虚拟打印机，请在保存文件完成后，点击【确定】继续打印下一份。\n\n点击【取消】将停止剩余所有任务。",
                                        "等待人工处理 (交互模式)",
                                        MessageBoxButton.OKCancel,
                                        MessageBoxImage.Information);

                                    if (result == MessageBoxResult.Cancel)
                                    {
                                        stopPrinting = true;
                                    }
                                });
                            }
                            else
                            {
                                // 自动模式：仅休眠 2 秒
                                System.Threading.Thread.Sleep(2000);
                            }
                            // ---- 核心修改结束 ----
                        }

                        Dispatcher.Invoke(() =>
                        {
                            if (stopPrinting)
                                MessageBox.Show("打印任务已由用户取消。");
                            else
                                MessageBox.Show("所有打印任务已发送完毕！");
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show("打印过程中发生错误: " + ex.Message));
                }
                finally
                {
                    Dispatcher.Invoke(() => UpdatePrintButtonState());
                }
            });
        }

        private void PrintSingleFile(string filePath, string printerName)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = filePath;
                info.Verb = "printto";
                info.Arguments = $"\"{printerName}\"";

                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.UseShellExecute = true;

                using (Process p = Process.Start(info))
                {
                    p.WaitForInputIdle(5000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"文件 {filePath} 打印失败: {ex.Message}");
            }
        }

        #endregion
    }
}
/*
```eox

-- -

### 第四部分：如何打包成单文件 .exe (关键步骤)

要在 Win7 - Win11 上运行且不需要安装.NET 环境，必须执行 **发布 (Publish)** 操作。

1.  在 Visual Studio 右侧 **解决方案资源管理器** 中，**右键点击项目名称**，选择 **“发布” (Publish)**。
2.  **目标**：选择 **“文件夹”**。
3.  **特定目标**：选择 **“文件夹”**。
4.  点击 **“完成”**，然后点击 **“关闭”**。
5.  现在你会看到发布配置界面，点击 **“显示所有设置”** (Show all settings) 或者点击配置旁边的 **铅笔图标**。
6.  **修改配置 (必须严格按照以下设置)**：
    * **配置 (Configuration)**: `Release`
    * **目标框架 (Target Framework)**: `net6.0-windows` (或 net8.0)
    * **部署模式 (Deployment Mode)**: **独立(Self - contained) * **(这一步最重要，它会把.NET 运行时打包进去)*
    * **目标运行时 (Target Runtime)**: `win - x64` (如果此时为了兼容非常老的 32 位 Win7，可选 win-x86，但建议 x64)。
    * **文件发布选项 (File Publish Options)**:
        *勾选 * *生成单个文件(Produce single file) * *。
        *(可选)勾选 * *启用 ReadyToRun 编译** (启动更快)。
        * **不要** 勾选 “裁剪未使用的代码” (Trim unused code)，因为 WPF 反射机制多，裁剪容易导致程序崩溃。
7.  点击 **“保存”**。
8.  点击右上角的 **“发布” (Publish)** 按钮。

**结果**：
Visual Studio 会编译一段时间。完成后，点击输出路径（通常是 `bin\Release\net6.0-windows\win-x64\publish\`）。
在这个文件夹里，你会找到一个很大的 `.exe` 文件（大约 60MB-100MB，因为它包含了整个 .NET 运行环境）。

**这就是你要的文件，直接拷贝到任何 Win7 (需安装 KB2533623 补丁, 绝大多数 Win7 都有) 或 Win10/11 电脑上，双击即可运行，无需管理员权限，无需安装 Office (前提是文件关联正确，例如打印 docx 需要电脑上有 Word 或 WPS)。**

### 注意事项
* **Office 依赖**：代码中使用 `Process.Start` 进行打印。这意味着**运行该程序的电脑必须安装有能打开这些文件的软件**（例如打印 .docx 需要安装 Word 或 WPS，打印 .pdf 需要安装 PDF 阅读器）。这是不使用昂贵商业控件（如 Spire.Office）的唯一免费方案。
* **管理员权限**：本程序主要读取文件和调用打印服务，默认配置不需要管理员权限。如果在企业域控环境下遇到问题，可能需要检查打印机驱动权限。
*/