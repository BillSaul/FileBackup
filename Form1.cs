using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PhotoBackup
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		string photoPath;
		string backupPath;
		string TitleText = "欢迎使用！    +++ 软件版本v2.4 +++";
		List<string> list1 = new List<string>();  //存放需要备份的
		List<string> list2 = new List<string>();  //存放备份至的
		List<string> list3 = new List<string>();  //存放需要备份的文件名
		delegate void MyDelegate(string str);  //委托 日志输出
		delegate void ButtonDelegate();  //委托 按钮控制
		delegate void ProgressBarDelegate(int flag, int Count);  //委托 进度条
		private MyDelegate myDeleteage;
		private ButtonDelegate buttonDelegate;
		private ProgressBarDelegate progressBarDelegate;
		private Thread thread;

		/*******************************方法*******************************/
		public string Time()  //获取时间，用于日志输出
		{
			return DateTime.Now.ToString("[" + "HH:mm:ss" + "]");
		}

		public void logshow(string str)  //listBox自动到底部
		{
			bool scroll = false;
			if (this.listBox1.TopIndex == this.listBox1.Items.Count - (int)(this.listBox1.Height / this.listBox1.ItemHeight))
				scroll = true;
			this.listBox1.Items.Add(Time() + str);
			if (scroll)
				this.listBox1.TopIndex = this.listBox1.Items.Count - (int)(this.listBox1.Height / this.listBox1.ItemHeight);
		}

		public void ButtonSwitch()
		{
			button3.Enabled = true;
			button4.Enabled = true;
		}

		public void ProgressBarShow(int flag, int Count)
		{
			if (flag == 1)
				progressBar1.PerformStep();
			if (flag == 0)
			{
				progressBar1.Minimum = 1;  //最小值
				progressBar1.Maximum = Count;  //最大值
				progressBar1.Value = 1;  //初始值
				progressBar1.Step = 1;
			}
		}

		public string GetFileMD5(string name)  //获取文件MD5值
		{
			FileStream file2 = new FileStream(name, FileMode.Open);
			MD5 md5 = new MD5CryptoServiceProvider();
			byte[] retval = md5.ComputeHash(file2);
			file2.Close();
			StringBuilder sc = new StringBuilder();
			for (int i = 0; i < retval.Length; i++)
			{
				sc.Append(retval[i].ToString("x2"));
			}
			return sc.ToString();
		}

		private void VerifyFile()  //检验文件
		{
			var fileName1 = Directory.GetFiles(@photoPath);
			var fileName2 = Directory.GetFiles(@backupPath);
			var Count = fileName1.Length + fileName2.Length;
			this.Invoke(progressBarDelegate, 0, Count);
			foreach (var name in fileName1)  //获取需要备份的文件夹下的文件名和大小
			{
				string md5 = GetFileMD5(name);
				this.Invoke(myDeleteage, Path.GetFileName(name) + " ├─→ " + md5);
				list1.Add(Path.GetFileName(name) + " ├─→ " + md5);
				this.Invoke(progressBarDelegate, 1, Count);
			}
			this.Invoke(myDeleteage, "+++++++++++++++我是分割线+++++++++++++++");
			foreach (var name in fileName2)  //获取需要备份至的文件夹下的文件名和大小
			{
				string md5 = GetFileMD5(name);
				this.Invoke(myDeleteage, Path.GetFileName(name) + " ├─→ " + md5);
				list2.Add(Path.GetFileName(name) + " ├─→ " + md5);
				this.Invoke(progressBarDelegate, 1, Count);
			}
			this.Invoke(myDeleteage, "+++++++++++++++目标文件夹共计" + fileName1.Length + "个文件+++++++++++++++");
			this.Invoke(myDeleteage, "+++++++++++++++备份文件夹共计" + fileName2.Length + "个文件+++++++++++++++");
			list3 = list1.Except(list2).ToList();  //获取差值
			foreach (var name in list3)  //输出未同步的文件
			{
				this.Invoke(myDeleteage, "未同步文件：" + name);
			}
			if (list3.Count == 0)
			{
				this.Invoke(myDeleteage, "***************没有需要同步的文件***************");
			}
			else
				this.Invoke(myDeleteage, "+++++目标文件夹" + fileName1.Length + "个文件，备份文件夹" + fileName2.Length + "个文件，需要备份" + list3.Count + "个文件+++++");
			this.Invoke(buttonDelegate);
		}

		private void BackupFile()
		{
			var Count = list3.Count;
			this.Invoke(progressBarDelegate, 0, Count);
			foreach (var name in list3)
			{
				string filePath1 = photoPath + "\\" + name.Split('├')[0];
				string filePath2 = backupPath + "\\" + name.Split('├')[0];
				File.Copy(filePath1, filePath2, true);  //复制文件
				this.Invoke(myDeleteage, "已复制：" + name.Split('├')[0]);
				this.Invoke(progressBarDelegate, 1, Count);
			}
			this.Invoke(myDeleteage, "+++++++++++++++文件备份完毕！+++++++++++++++");
		}

		/*******************************事件*******************************/
		private void listBox1_DrawItem(object sender, DrawItemEventArgs e)  //改变listBox显示字体颜色
		{
			e.DrawBackground();
			if (listBox1.Items[e.Index].ToString().IndexOf("*") != -1)  //如果带*号，则红字显示
			{
				e.Graphics.DrawString(((ListBox)sender).Items[e.Index].ToString(), e.Font, new SolidBrush(Color.Red), e.Bounds);
			}
			else if (listBox1.Items[e.Index].ToString().IndexOf("+") != -1)  //带+号，显示蓝色
			{
				e.Graphics.DrawString(((ListBox)sender).Items[e.Index].ToString(), e.Font, new SolidBrush(Color.Blue), e.Bounds);
			}
			else
			{
				e.Graphics.DrawString(((ListBox)sender).Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds);
			}
			e.DrawFocusRectangle();
		}

		private void button1_Click(object sender, EventArgs e)  //选择需要备份的文件夹目录
		{
			FolderBrowserDialog dialog = new FolderBrowserDialog();
			dialog.Description = "请选择文件夹";
			if (dialog.ShowDialog() == DialogResult.OK)
			{
				photoPath = dialog.SelectedPath;
				textBox1.Text = photoPath;
				logshow("目标文件夹：" + photoPath);
			}
		}

		private void button2_Click(object sender, EventArgs e)  //选择需要备份到的文件夹
		{
			FolderBrowserDialog dialog = new FolderBrowserDialog();
			dialog.Description = "请选择文件夹";
			if (dialog.ShowDialog() == DialogResult.OK)
			{
				backupPath = dialog.SelectedPath;
				textBox2.Text = backupPath;
				logshow("备份至：" + backupPath);
			}
		}

		private void button3_Click(object sender, EventArgs e)  //校验文件
		{
			if (photoPath == null || backupPath == null)
			{
				logshow("**********未选择文件路径**********");
				MessageBox.Show("请选择文件路径！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			else
			{
				button3.Enabled = false;
				button4.Enabled = false;
				if (thread == null)
				{
					thread = new Thread(VerifyFile);
					thread.Start();
				}
			}
		}

		private void Form1_Load(object sender, EventArgs e)  //界面加载事件
		{
			logshow(TitleText);
			myDeleteage = logshow;
			buttonDelegate = ButtonSwitch;
			progressBarDelegate = ProgressBarShow;
		}

		private void button4_Click(object sender, EventArgs e)  //开始同步
		{
			if (photoPath == null || backupPath == null)
			{
				logshow("**********未选择文件路径**********");
				MessageBox.Show("请选择文件路径！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			else
			{
				if (list3.Count == 0)
				{
					logshow("**********未检验文件**********");
					MessageBox.Show("请先校验文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				else
				{
					if (thread == null)
					{
						thread = new Thread(BackupFile);
						thread.Start();
					}
					else
					{
						thread.Abort();
						thread = new Thread(BackupFile);
						thread.Start();
					}
				}
			}
		}

		private void button6_Click(object sender, EventArgs e)  //重置
		{
			listBox1.Items.Clear();
			logshow(TitleText);
			list1.Clear();
			list2.Clear();
			list3.Clear();
			photoPath = null;
			backupPath = null;
			textBox1.Text = null;
			textBox2.Text = null;
			button3.Enabled = true;
			button4.Enabled = true;
			progressBar1.Value = 1;
			if (thread != null)
				thread.Abort();
		}
	}
}
