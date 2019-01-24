using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace MyBMFont
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        class BMFontInfo
        {
            public BMFontInfo(string pAbsolutePath)
            {
                SetUri(pAbsolutePath);
            }

            public BMFontInfo(Uri pAbsoluteUri)
            {
                SetUri(pAbsoluteUri);
            }

            /// <summary>
            /// 绝对路径
            /// </summary>
            public Uri pictureAbsoluteUri { private set; get; }
            /// <summary>
            /// 字符ID
            /// </summary>
            public int charID
            {
                get
                {
                    return (int)charSymbol;
                }
            }
            /// <summary>
            /// 字符符号
            /// </summary>
            public char charSymbol { private set; get; }
            public int xOffset { set; get; }
            public int yOffset { set; get; }
            public int advance { set; get; }

            public void SetUri(string pAbsolutePath)
            {
                SetUri(new Uri(pAbsolutePath, UriKind.Absolute));
            }

            public void SetUri(Uri pAbsoluteUri)
            {
                pictureAbsoluteUri = pAbsoluteUri;
            }

            public void SetCharSymbol(char pCharSymbol)
            {
                charSymbol = pCharSymbol;
            }

            public void SetCharSymbol(string pCharSymbol)
            {
                charSymbol = string.IsNullOrEmpty(pCharSymbol) ? (char)' ' : pCharSymbol[0];
            }

            public void SetOffsetX(string pValue)
            {
                xOffset = string.IsNullOrEmpty(pValue) ? 0 : int.Parse(pValue);
            }
            public void SetOffsetY(string pValue)
            {
                yOffset = string.IsNullOrEmpty(pValue) ? 0 : int.Parse(pValue);
            }
            public void SetAdvance(string pValue)
            {
                advance = string.IsNullOrEmpty(pValue) ? 0 : int.Parse(pValue);
            }
        }

        enum BMFontType
        {
            Config,
            ImageFolder,
        }

        DataTable mTable;
        Uri mGenerateAbsoluteUri;
        Uri generateAbsoluteUri
        {
            set
            {
                mGenerateAbsoluteUri = value;
                mGenerateName = mGenerateAbsoluteUri.AbsolutePath.Substring(mGenerateAbsoluteUri.AbsolutePath.LastIndexOf("/") + 1);
            }
            get
            {
                return mGenerateAbsoluteUri;
            }
        }
        string mGenerateName;
        List<BMFontInfo> mInfos = new List<BMFontInfo>();
        BMFontType mBMFontType;
        public const string cPictureAbsolutePath = "图片路径";
        public const string cCharSymbol = "字符符号";
        public const string cCharOffsetX = "Offset X";
        public const string cCharOffsetY = "Offset Y";
        public const string cCharAdvance = "Advance";

        public MainWindow()
        {
            InitializeComponent();

            mTable = new DataTable();
            dataGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
            dataGrid.SelectionMode = DataGridSelectionMode.Extended;
            dataGrid.IsReadOnly = false;

            mTable.Columns.Add(cPictureAbsolutePath, typeof(string));
            mTable.Columns.Add(cCharSymbol, typeof(char));
            mTable.Columns.Add(cCharOffsetX, typeof(int));
            mTable.Columns.Add(cCharOffsetY, typeof(int));
            mTable.Columns.Add(cCharAdvance, typeof(int));

            bmfontPathTextBox.Text = MyPrefs.GetString(nameof(bmfontPathTextBox));
            imageFolderNameTextBox.Text = MyPrefs.GetString(nameof(imageFolderNameTextBox));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mBMFontType = string.IsNullOrEmpty(MyPrefs.sConfigPath) ? BMFontType.ImageFolder : BMFontType.Config;
            InitConfig(MyPrefs.sConfigPath);
        }

        void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                MessageBox.Show("只支持拖拽包含Images的文件夹或者bmfc文件", "提示");
                return;
            }

            var tFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (tFiles.Length > 1)
            {
                MessageBox.Show("只支持拖拽包含Images的文件夹或者bmfc文件", "提示");
                return;
            }

            if (tFiles[0].EndsWith("bmfc") && File.Exists(tFiles[0]))
            {
                mBMFontType = BMFontType.Config;
                MyPrefs.sConfigPath = tFiles[0];
                InitConfig(tFiles[0]);
                return;
            }

            var tDirectoryInfo = new DirectoryInfo(tFiles[0]);
            if (!tDirectoryInfo.Exists)
            {
                MessageBox.Show("只支持拖拽包含Images的文件夹或者bmfc文件", "提示");
                return;
            }

            mBMFontType = BMFontType.ImageFolder;
            imageFolderNameTextBox.Text = tDirectoryInfo.Name;
            var tPath = tDirectoryInfo.FullName;
            tPath = tPath.Substring(0, tPath.LastIndexOf("\\"));
            generateAbsoluteUri = new Uri(tPath, UriKind.Absolute);
            var tPngFiles = tDirectoryInfo.GetFiles("*.png", SearchOption.AllDirectories);

            if (tPngFiles == null || tPngFiles.Length == 0)
            {
                MessageBox.Show("该文件夹下不包含一个png图片", "提示");
                return;
            }

            mInfos.Clear();
            foreach (var item in tPngFiles)
            {
                var tAbsoluteUri = new Uri(item.FullName, UriKind.Absolute);
                var tInfo = new BMFontInfo(tAbsoluteUri);
                tInfo.SetCharSymbol(string.Empty);
                mInfos.Add(tInfo);
            }

            RefreshDataGrid();
        }

        void InitConfig(string pConfigPath)
        {
            if (!string.IsNullOrEmpty(pConfigPath))
            {
                mInfos.Clear();

                var tFileInfo = new FileInfo(pConfigPath);
                generateAbsoluteUri = new Uri(tFileInfo.Directory.FullName, UriKind.Absolute);

                var tAllLines = File.ReadAllLines(pConfigPath);
                foreach (var tLine in tAllLines)
                {
                    if (!tLine.StartsWith("icon=")) continue;
                    var tMatch = Regex.Match(tLine, @"(?<=\"")(?<path>.*?)(?=\"")");
                    if (!tMatch.Success) continue;
                    var tInfo = new BMFontInfo(System.IO.Path.Combine(generateAbsoluteUri.AbsolutePath, tMatch.Groups["path"].Value));
                    tMatch = Regex.Match(tLine, @"(?<=\"",)(?<charID>\d+),(?<xoffset>.*?),(?<yoffset>.*?),(?<advance>.*?)$");
                    if (!tMatch.Success) continue;
                    tInfo.SetCharSymbol((char)int.Parse(tMatch.Groups["charID"].Value));
                    tInfo.SetOffsetX(tMatch.Groups["xoffset"].Value);
                    tInfo.SetOffsetY(tMatch.Groups["yoffset"].Value);
                    tInfo.SetAdvance(tMatch.Groups["advance"].Value);
                    mInfos.Add(tInfo);
                }
            }

            RefreshDataGrid();
        }

        void RefreshDataGrid()
        {
            fontInfoTextBox.Text = (generateAbsoluteUri == null ? string.Empty : generateAbsoluteUri.AbsolutePath);

            mTable.Clear();

            foreach (var tInfo in mInfos)
            {
                CreateRows(tInfo);
            }

            dataGrid.ItemsSource = mTable.DefaultView;
        }

        void CreateRows(BMFontInfo pInfo)
        {
            DataRow dr = mTable.NewRow();
            dr[cPictureAbsolutePath] = pInfo.pictureAbsoluteUri.AbsolutePath;
            dr[cCharSymbol] = pInfo.charSymbol.ToString();
            dr[cCharOffsetX] = pInfo.xOffset;
            dr[cCharOffsetY] = pInfo.yOffset;
            dr[cCharAdvance] = pInfo.advance;
            mTable.Rows.Add(dr);
        }

        private void generator_Click(object sender, RoutedEventArgs e)
        {
            var tExePath = bmfontPathTextBox.Text;
            if (!File.Exists(tExePath))
            {
                MessageBox.Show("填写bmfont.com文件的所在绝对路径", "提示");
                return;
            }

            var tInfoPath = fontInfoTextBox.Text;
            if (!Directory.Exists(tInfoPath))
            {
                MessageBox.Show("字体信息生成路径不是一个文件夹", "提示");
                return;
            }

            if (string.IsNullOrEmpty(imageFolderNameTextBox.Text.Trim()))
            {
                MessageBox.Show($"Image文件夹名字不能为空", "提示");
                return;
            }
            var tImageAbsoluteUri = new Uri(Path.Combine(generateAbsoluteUri.AbsolutePath, imageFolderNameTextBox.Text.Trim()), UriKind.Absolute);
            if (!Directory.Exists(tImageAbsoluteUri.AbsolutePath))
            {
                MessageBox.Show($"Image文件夹名字错误，不存在该路径“{tImageAbsoluteUri.AbsolutePath}“", "提示");
                return;
            }

            var tInfos = new List<BMFontInfo>();
            foreach (var item in dataGrid.Items)
            {
                var tRowView = item as DataRowView;
                if (tRowView == null) continue;
                var tInfo = new BMFontInfo(tRowView[cPictureAbsolutePath].ToString());
                if (!File.Exists(tInfo.pictureAbsoluteUri.AbsolutePath))
                {
                    MessageBox.Show($"含有不存在的路径 {tInfo.pictureAbsoluteUri.AbsolutePath}", "提示");
                    return;
                }

                tInfo.SetCharSymbol(tRowView[cCharSymbol].ToString());
                if (tInfo.charSymbol == ' ' || string.IsNullOrEmpty(tInfo.charSymbol.ToString()))
                {
                    MessageBox.Show($"有未填写的字符符号", "提示");
                    return;
                }

                tInfo.SetOffsetX(tRowView[cCharOffsetX].ToString());
                tInfo.SetOffsetY(tRowView[cCharOffsetY].ToString());
                tInfo.SetAdvance(tRowView[cCharAdvance].ToString());
                tInfos.Add(tInfo);
            }

            var tTempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyBMFont");
            if (!Directory.Exists(tTempPath))
            {
                Directory.CreateDirectory(tTempPath);
            }

            var tTempConfigPath_Cmd = Path.Combine(tTempPath, "tempCmd.bmfc");
            var tTempConfigPath_Source = Path.Combine(tTempPath, "tempSource.bmfc");
            if (File.Exists(tTempConfigPath_Cmd)) File.Delete(tTempConfigPath_Cmd);
            if (File.Exists(tTempConfigPath_Source)) File.Delete(tTempConfigPath_Source);

            List<string> tTempLines = null;

            if (mBMFontType == BMFontType.Config && File.Exists(MyPrefs.sConfigPath))
            {
                tTempLines = File.ReadAllLines(MyPrefs.sConfigPath).ToList();
            }
            else if (mBMFontType == BMFontType.ImageFolder)
            {
                var tBackupConfigUri = new Uri(@"pack://application:,,,/Resources/backup.txt");
                var tStream = Application.GetResourceStream(tBackupConfigUri);
                var tReader = new StreamReader(tStream.Stream);
                tTempLines = new List<string>();
                while (!tReader.EndOfStream)
                {
                    tTempLines.Add(tReader.ReadLine());
                }
            }

            for (int i = 0; i < tTempLines.Count;)
            {
                if (tTempLines[i].StartsWith("icon=\""))
                {
                    tTempLines.RemoveAt(i);
                    continue;
                }
                i++;
            }

            var tAllLines_Cmd = new List<string>(tTempLines);
            var tAllLines_Source = new List<string>(tTempLines);
            foreach (var tInfo in tInfos)
            {
                var tUri = tImageAbsoluteUri.MakeRelativeUri(tInfo.pictureAbsoluteUri);
                var tRelativePath = tUri.OriginalString;
                var tContent_Source = string.Format($"icon=\"{tRelativePath}\",{tInfo.charID},{tInfo.xOffset},{tInfo.yOffset},{tInfo.advance}");
                tAllLines_Source.Add(tContent_Source);

                var tContent_Cmd = string.Format($"icon=\"{tInfo.pictureAbsoluteUri.AbsolutePath}\",{tInfo.charID},{tInfo.xOffset},{tInfo.yOffset},{tInfo.advance}");
                tAllLines_Cmd.Add(tContent_Cmd);
            }

            File.WriteAllLines(tTempConfigPath_Cmd, tAllLines_Cmd);
            File.WriteAllLines(tTempConfigPath_Source, tAllLines_Source);

            CMD.ProcessCommand(tExePath, string.Format($"-c {tTempConfigPath_Cmd} -o {Path.Combine(generateAbsoluteUri.AbsolutePath.Replace("/", "\\"), mGenerateName + ".fnt")}"));

            var tNewPath = Path.Combine(generateAbsoluteUri.AbsolutePath, mGenerateName + ".bmfc");
            File.Copy(tTempConfigPath_Source, tNewPath, true);

            MyPrefs.SetString(nameof(bmfontPathTextBox), tExePath);
            MyPrefs.SetString(nameof(imageFolderNameTextBox), imageFolderNameTextBox.Text.Trim());
        }
    }
}
