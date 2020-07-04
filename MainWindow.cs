using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Win10Clean
{
    public partial class MainWindow : Form
    {
        private List<FileIdentityDuplicate> duplicateFileList = new List<FileIdentityDuplicate>();
        private FileInfoDb _FileInfoDb = FileInfoDb.Intance;
        public MainWindow()
        {
            InitializeComponent();
            this.GetDrive();
            this.listFiles.VirtualListSize = 1000000;
            this.listFiles.VirtualMode = true;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            this.toolStripStatusLabel2.Text = string.Empty;
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.ExitThread();
        }

        private void GetDrive()
        {
            string[] drives = Environment.GetLogicalDrives();

            foreach (string drive in drives)
            {
                DriveInfo di = new DriveInfo(drive);
                int driveImage;

                switch (di.DriveType)    //set the drive's icon
                {
                    case DriveType.CDRom:
                        driveImage = 3;
                        break;
                    case DriveType.Network:
                        driveImage = 6;
                        break;
                    case DriveType.NoRootDirectory:
                        driveImage = 8;
                        break;
                    case DriveType.Unknown:
                        driveImage = 8;
                        break;
                    default:
                        driveImage = 2;
                        break;
                }
                if (driveImage == 2)
                {
                    TreeNode node = new TreeNode(drive.Substring(0, 1), driveImage, driveImage);
                    node.Tag = drive;
                    this.tvFolder.Nodes.Add(node);
                }
            }
        }
        private void PopulateTreeView(string path, TreeNode rootNode)
        {
            DirectoryInfo info = new DirectoryInfo(path);
            if (info.Exists)
            {
                try
                {
                    GetDirectories(info.GetDirectories(), rootNode);
                }
                catch (UnauthorizedAccessException e)
                {

                }
            }
        }

        private void GetDirectories(DirectoryInfo[] subDirs,
            TreeNode nodeToAddTo)
        {
            TreeNode aNode;

            foreach (DirectoryInfo subDir in subDirs)
            {
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Tag = subDir.FullName;
                aNode.ImageKey = "FolderClosed";

                nodeToAddTo.Nodes.Add(aNode);
            }
        }

        private void tvFolder_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag != null && (e.Node.Nodes != null && e.Node.Nodes.Count > 0))
            {
                e.Node.Nodes.Clear();
            }
            this.PopulateTreeView(e.Node.Tag.ToString(), e.Node);
            if (e.Node.Tag.ToString().Length > 4)
            {
                this.PopulateListView(e.Node.Tag.ToString());
            }
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Tag.Equals("scan"))
            {
                this.toolStripButton1.Enabled = false;
                if (this.tvFolder.SelectedNode != null)
                {
                    string folder = this.tvFolder.SelectedNode.Tag.ToString();
                    this.toolStripStatusLabel1.Text = string.Empty;
                    FileScanWorker worker = new FileScanWorker();
                    worker.ProgressChanged += new EventHandler<ProgressChangedArgs>(OnWorkerProgressChanged);
                    Thread workerThread = new Thread(() => worker.StartWork(folder));
                    workerThread.Start();

                }
            }
        }

        private void OnWorkerProgressChanged(object sender, ProgressChangedArgs e)
        {
            // Cross thread - so you don't get the cross-threading exception
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    OnWorkerProgressChanged(sender, e);
                });
                return;
            }

            // Change control
            //this.label1.Text = e.Progress;
            this.toolStripStatusLabel1.Text = "Completed";

            this.PopulateListView(e.Progress);
        }

        
        private void PopulateListView(string folder)
        {
            long duplicateCount = 0;
            this.duplicateFileList.Clear();


            FileIdentityDuplicate lastItem = null;

            IEnumerable<FileIdentity> infos = this._FileInfoDb.FindAll(folder);
            foreach (var inf in infos)
            {
                FileIdentityDuplicate d = new FileIdentityDuplicate { item = inf, selected = false };
                this.duplicateFileList.Add(d);
                if (lastItem != null)
                {
                    FileIdentity fi = lastItem.item;
                    if (fi.Name.Equals(inf.Name) && fi.Size == inf.Size)
                    {
                       d.selected = true;
                        duplicateCount++;
                    }
                    else
                    {
                        if (!lastItem.selected)
                        {
                            this.duplicateFileList.Remove(lastItem);
                        }
                    }
                }

                lastItem = d;

            }

            this.toolStripStatusLabel2.Text = string.Empty;
            if (duplicateCount > 0)
            {
                this.toolStripButton1.Enabled = true;
            }
            else
            {
                this.toolStripButton1.Enabled = false;
                
            }
            this.toolStripStatusLabel2.Text = "Total File: " + infos.Count().ToString() + " Duplicate: " + duplicateCount;
            this.toolStripStatusLabel1.Text = "Completed";
            this.listFiles.VirtualListSize = this.duplicateFileList.Count;
        }

        private void listFiles_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (this.duplicateFileList.Count > e.ItemIndex)
            {

                FileIdentityDuplicate d = this.duplicateFileList[e.ItemIndex];
                FileIdentity inf = d.item;
                ListViewItem item = new ListViewItem(inf.Name, 1);
                item.Tag = inf;
                ListViewItem.ListViewSubItem[] subItems = new ListViewItem.ListViewSubItem[]
                { new ListViewItem.ListViewSubItem(item, inf.FilePath),
             new ListViewItem.ListViewSubItem(item,
                inf.LastModified.ToShortDateString()),
                    new ListViewItem.ListViewSubItem(item,
                inf.Size.ToString()),
                new ListViewItem.ListViewSubItem(item,
                inf.CheckSum.ToString())};

                item.SubItems.AddRange(subItems);
                if (d.selected)
                {
                    item.BackColor = Color.Red;
                    item.Checked = true;
                }
                e.Item = item;
            }
            else
            {
                ListViewItem item = new ListViewItem();
               
                ListViewItem.ListViewSubItem[] subItems = new ListViewItem.ListViewSubItem[]
                { new ListViewItem.ListViewSubItem(item, string.Empty),
             new ListViewItem.ListViewSubItem(item,
                string.Empty),
                    new ListViewItem.ListViewSubItem(item,
               string.Empty),
                new ListViewItem.ListViewSubItem(item,
                string.Empty)};

                item.SubItems.AddRange(subItems);
               
                e.Item = item;
            }
        }
    }

    class FileIdentityDuplicate
    {
        public FileIdentity item { get; set; }
        public bool selected { get; set; }
    }
}
