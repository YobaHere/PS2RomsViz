using Microsoft.WindowsAPICodePack.Dialogs;
using PS2RomsVisualizer.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PS2RomsVisualizer
{
	public class MainForm : Form
	{
		private class ListViewItemComparer : IComparer
		{
			private readonly int col;

			public ListViewItemComparer()
			{
				col = 0;
			}

			public ListViewItemComparer(int column)
			{
				col = column;
			}

			public int Compare(object x, object y)
			{
				return string.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
			}
		}

		public class Game
		{
			public string Name
			{
				get;
				set;
			}

			public Bitmap Image
			{
				get;
				set;
			}

			public Game(string name)
			{
				Name = name;
			}
		}
		private class AppSettings
        {
			public string gamesPath { get; set; }
			public string bannersPath { get; set; }
			public string pcsx2Path { get; set; }
			public AppSettings()
            {
				gamesPath = null;
				bannersPath = null;
				pcsx2Path = null;

			}
        }
		private AppSettings appSettings = new AppSettings();
		private int previousSelectedItemIndex;

		private IContainer components;

		private ImageList gamesImageList;

		private ColumnHeader columnName1;

		private ColumnHeader columnImage1;

		private ListViewEx listView1;

		public MainForm()
		{
			InitializeComponent();
			LoadSettings();
			listView1.HeaderStyle = ColumnHeaderStyle.None;
			List<Game> list = ParseGames(appSettings.gamesPath);
			int num = 0;
			foreach (Game item in list)
			{
				gamesImageList.Images.Add(item.Image);
				ListViewItem value = new ListViewItem
				{
					ImageIndex = num,
					SubItems =
					{
						item.Name
					}
				};
				listView1.Items.Add(value);
				num++;
			}
			listView1.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
			listView1.ListViewItemSorter = new ListViewItemComparer(1);
			int magicNumber = 16; // число которое позволяет иметь отступ справа достаточный, чтобы не было горизонтального скролла
			int width = base.Width = listView1.Columns[0].Width + listView1.Columns[1].Width + magicNumber;
			int currentHeight = list.Count * 49 + 39;
			int maxHeight = 983;
			if (currentHeight > maxHeight)
			{
				currentHeight = maxHeight;
			}
			base.Height = currentHeight;
			MinimumSize = new Size(width, 84);
		}

		void LoadSetting(Setting setting) {
			PropertyInfo propInfo = Settings.Default.GetType().GetProperty(setting.Name);
			string value = (string)propInfo.GetValue(Settings.Default);
			if (value == null || !Directory.Exists(value))
			{
				string selectedPath = "";
				if (setting.DialogType == DialogType.FolderDialog) selectedPath = GetFolderPath(setting.Msg);
				if (setting.DialogType == DialogType.FileDialog) selectedPath = GetFilePath(setting.Msg);
				propInfo.SetValue(Settings.Default, selectedPath);
				Settings.Default.Save();
			}
			// gets prop by name from type and sets value of that prop to prop on object
			PropertyInfo localSettingInfo = appSettings.GetType().GetProperty(setting.Name);
			localSettingInfo.SetValue(appSettings, (string)propInfo.GetValue(Settings.Default));

		}
		void SetSetting(Setting setting)
		{
			PropertyInfo propInfo = Settings.Default.GetType().GetProperty(setting.Name);
			string value = (string)propInfo.GetValue(Settings.Default);
			string selectedPath = "";
			if (setting.DialogType == DialogType.FolderDialog) selectedPath = GetFolderPath(setting.Msg);
			if (setting.DialogType == DialogType.FileDialog) selectedPath = GetFilePath(setting.Msg);
			propInfo.SetValue(Settings.Default, selectedPath);
			Settings.Default.Save();
			PropertyInfo localSettingInfo = appSettings.GetType().GetProperty(setting.Name);
			localSettingInfo.SetValue(appSettings, (string)propInfo.GetValue(Settings.Default));
			InitializeComponent();
		}

		public enum DialogType
        {
			FolderDialog,
			FileDialog
        }
		private class Setting
		{
			public string Name { get; set; }
			public string Msg { get; set; }
			public DialogType DialogType { get; set; }
			public Setting(string name, string msg, DialogType dialogType)
            {
				Name = name;
				Msg = msg;
				DialogType = dialogType;
            }
		}
		private void LoadSettings() {
			Setting[] settings = new[]  {
				new Setting ("gamesPath","Select a folder with .iso or .cso files", DialogType.FolderDialog),
				new Setting ("bannersPath", "Select a folder with 128x48 banners", DialogType.FolderDialog),
				new Setting ("pcsx2Path", "Select pcsx2.exe", DialogType.FileDialog)
			};
			foreach (Setting setting in settings) { LoadSetting(setting); }
        }

		private static string GetFolderPath(string desc)
		{
			CommonOpenFileDialog folderBrowserDialog = new CommonOpenFileDialog {
				IsFolderPicker = true, Title= desc
			};
			var form = new Form
			{
				TopMost = true,
				WindowState = FormWindowState.Minimized,
				Icon = Resources.MainIcon
			};
			var folderSelectResult = folderBrowserDialog.ShowDialog();
			if (folderSelectResult != CommonFileDialogResult.Ok)
			{
				var errorResult = MessageBox.Show(
					desc,
					"Error...",
					MessageBoxButtons.OKCancel,
					MessageBoxIcon.None,
					MessageBoxDefaultButton.Button1,
					MessageBoxOptions.DefaultDesktopOnly
				);
				if (errorResult == DialogResult.Cancel) Environment.Exit(0);
				else return GetFolderPath(desc);
			}
			return folderBrowserDialog.FileName;
		}
		private static string GetFilePath(string desc)
		{
			OpenFileDialog fileDialog = new OpenFileDialog
			{
				Filter = "pcsx2.exe|pcsx2.exe|pcsx2.exe (Any name)|*.exe",
				Title = desc
			};
			var form = new Form
			{
				TopMost = true,
				WindowState = FormWindowState.Minimized,
                Icon = Resources.MainIcon
			};
			DialogResult dialogRes = fileDialog.ShowDialog(form);
			if (dialogRes != DialogResult.OK)
			{
				var errorResult = MessageBox.Show(
					desc,
					"Error...",
					MessageBoxButtons.OKCancel,
					MessageBoxIcon.None,
					MessageBoxDefaultButton.Button1,
					MessageBoxOptions.DefaultDesktopOnly
				);
				if (errorResult == DialogResult.Cancel) Environment.Exit(0);
				else return GetFilePath(desc);
			}
			return fileDialog.FileName;
		}

		private List<Game> ParseGames(string path)
		{
			ICollection<FileInfo> list = (
				from file in new DirectoryInfo(path).GetFiles("*.*")
				where file.Name.ToLower().EndsWith("iso") || file.Name.ToLower().EndsWith("cso")
				select file
				).ToList();
			list.GroupBy(x => x.Name);
			List<Game> list2 = new List<Game>();
			foreach (FileInfo item in list)
			{
				Game game = new Game(item.Name.Remove(item.Name.Length - 4));
				list2.Add(game);
				if (File.Exists(appSettings.bannersPath + "\\" + game.Name + ".png"))
				{
					game.Image = new Bitmap(appSettings.bannersPath + "\\" + game.Name + ".png");
					continue;
				}
				Bitmap bitmap = new Bitmap(appSettings.bannersPath + "\\!none.png");
				bitmap.Save(appSettings.bannersPath + game.Name + ".png");
				bitmap = (game.Image = new Bitmap(appSettings.bannersPath + game.Name + ".png"));
			}
			return list2;
		}

		private void StartGame()
		{
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo();
				processStartInfo.FileName = appSettings.pcsx2Path;//"E:\\Emuls\\PCSX2 1.6.0\\pcsx2.exe";
																  // add check if file exist
																  // determine if iso or cso needed, prioritize iso
				bool isFileExist = false;
				string format = ".cso";
				string path = $"{appSettings.gamesPath}\\{listView1.FocusedItem.SubItems[1].Text}";
				if (File.Exists(path + ".cso")) isFileExist = true;
				if (!isFileExist && !File.Exists(path + ".iso")) throw new FileNotFoundException();
				processStartInfo.Arguments = $"\"{appSettings.gamesPath}\\{listView1.FocusedItem.SubItems[1].Text}.cso\"";
				File.Exists(processStartInfo.Arguments);
				Process.Start(processStartInfo);
				Application.Exit();
			}
			catch (FileNotFoundException)
			{
				MessageBox.Show(
					   "ISO/CSO file not found",
					   "Error...",
					   MessageBoxButtons.OK,
					   MessageBoxIcon.None,
					   MessageBoxDefaultButton.Button1,
					   MessageBoxOptions.DefaultDesktopOnly
				);
			}
			catch
			{
				MessageBox.Show(
					   "error happened lmao sorry",
					   "Error...",
					   MessageBoxButtons.OKCancel,
					   MessageBoxIcon.None,
					   MessageBoxDefaultButton.Button1,
					   MessageBoxOptions.DefaultDesktopOnly
				   );
			}

		}
		private void ListView_ItemDoubleClick(object sender, EventArgs e) {
			StartGame();
		}
		private void ListView_KeyUp(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.Enter) StartGame();
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			Color backColor = Color.FromArgb(32, 32, 32);
			Color grayText = SystemColors.GrayText;
			Color backColor2 = Color.FromArgb(64, 64, 64);
			Color foreColor = Color.FromArgb(192, 192, 192);
			ListViewItem listViewItem = listView1.Items[previousSelectedItemIndex];
			if (listViewItem != null)
			{
				listViewItem.BackColor = backColor;
				listViewItem.ForeColor = grayText;
			}
			ListViewItem focusedItem = listView1.FocusedItem;
			if (focusedItem != null)
			{
				focusedItem.Selected = false;
				if (focusedItem.Focused)
				{
					focusedItem.BackColor = backColor2;
					focusedItem.ForeColor = foreColor;
				}
				previousSelectedItemIndex = focusedItem.Index;
			}
		}

		protected override void Dispose(bool disposing) {
			if (disposing && components != null) components.Dispose();
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
            components = new Container();
            gamesImageList = new ImageList(components);
            listView1 = new ListViewEx();
            columnImage1 = new ColumnHeader();
            columnName1 = new ColumnHeader();
            SuspendLayout();
            // 
            // gamesImageList
            // 
            gamesImageList.ColorDepth = ColorDepth.Depth32Bit;
            gamesImageList.ImageSize = new Size(128, 48);
            gamesImageList.TransparentColor = Color.Transparent;
            // 
            // listView1
            // 
            listView1.Activation = ItemActivation.OneClick;
            listView1.BackColor = Color.FromArgb(32, 32, 32);
            listView1.BorderStyle = BorderStyle.None;
            listView1.Columns.AddRange(new ColumnHeader[] {columnImage1,columnName1});
            listView1.Dock = DockStyle.Fill;
            listView1.Font = new Font("Arial Unicode MS", 18F, FontStyle.Regular, GraphicsUnit.Pixel, 0);
            listView1.ForeColor = SystemColors.GrayText;
            listView1.FullRowSelect = true;
            listView1.HideSelection = false;
            listView1.LabelWrap = false;
            listView1.LargeImageList = gamesImageList;
            listView1.Location = new Point(0, 0);
            listView1.Margin = new Padding(0);
            listView1.MultiSelect = false;
            listView1.Name = "listView1";
            listView1.Size = new Size(381, 735);
            listView1.SmallImageList = gamesImageList;
            listView1.TabIndex = 1;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.View = View.Details;
            listView1.SelectedIndexChanged += new EventHandler(ListView_SelectedIndexChanged);
            listView1.DoubleClick += new EventHandler(ListView_ItemDoubleClick);
            listView1.KeyUp += new KeyEventHandler(ListView_KeyUp);
            // 
            // columnImage1
            // 
            columnImage1.Text = "Image";
            columnImage1.Width = 136;
            // 
            // columnName1
            // 
            columnName1.Text = "Name";
            columnName1.Width = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(listView1);
            Icon = Resources.MainIcon;
            Name = "MainForm";
            Text = "PlayStation 2 Launcher";
            Load += new EventHandler(MainForm_Load);
            ResumeLayout(false);
            ClientSize = new Size(350, 735);

		}

		public const int WM_SYSCOMMAND = 0x112;
		public const int MF_BYPOSITION = 0x400;
		public const int IsoMenu = 1000;
		public const int BannerMenu = 1001;
		public const int Pcsx2Menu = 1002;

		[DllImport("user32.dll")]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
		[DllImport("user32.dll")]
		private static extern bool InsertMenu(IntPtr hMenu, int wPosition, int wFlags, int wIDNewItem, string lpNewItem);
		[DllImport("user32.dll")]
		private static extern bool RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);


		protected override void WndProc(ref Message msg)
		{
			if (msg.Msg == WM_SYSCOMMAND)
			{
				switch (msg.WParam.ToInt32())
				{
				//new Setting("gamesPath", "Select a folder with .iso or .cso files", DialogType.FolderDialog),
				//new Setting("bannersPath", "Select a folder with 128x48 banners", DialogType.FolderDialog),
				//new Setting("pcsx2Path", "Select pcsx2.exe", DialogType.FileDialog)
					case IsoMenu:
						SetSetting(new Setting("gamesPath", "Select a folder with .iso or .cso files", DialogType.FolderDialog));
						return;
					case BannerMenu:
						SetSetting(new Setting("bannersPath", "Select a folder with 128x48 banners", DialogType.FolderDialog));
						return;
					case Pcsx2Menu:
						SetSetting(new Setting("pcsx2Path", "Select pcsx2.exe", DialogType.FileDialog));
						return;
					default:
						break;
				}
			}
			base.WndProc(ref msg);
		}
		private void MainForm_Load(object sender, EventArgs e)
        {
			IntPtr MenuHandle = GetSystemMenu(Handle, false);
			InsertMenu(MenuHandle, 0, MF_BYPOSITION, IsoMenu, "Change ISO folder path");
			InsertMenu(MenuHandle, 1, MF_BYPOSITION, BannerMenu, "Change banner folder path");
			InsertMenu(MenuHandle, 1, MF_BYPOSITION, Pcsx2Menu, "Change PCSX2 path");
		}
    }
}
