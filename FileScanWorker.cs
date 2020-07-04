using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Win10Clean
{
    public class FileScanWorker
    {
        private FileInfoDb _FileInfoDb = FileInfoDb.Intance;
        public event EventHandler<ProgressChangedArgs> ProgressChanged;

        protected void OnProgressChanged(ProgressChangedArgs e)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, e);
            }
        }

        public void StartWork(string folder)
        {
            DirectoryInfo info = new DirectoryInfo(folder);
            if (info.Exists)
            {
                List<FileInfo> fileInfoList = new List<FileInfo>();
                this.GetFiles(new DirectoryInfo[] { info }, fileInfoList);

                Parallel.ForEach(fileInfoList, ScanAndUpdate);
                OnProgressChanged(new ProgressChangedArgs(folder));
            }
        }

        private void GetFiles(DirectoryInfo[] subDirs, List<FileInfo> fileInfoList)
        {
            foreach (var item in subDirs)
            {
                try
                {
                    FileInfo[] files = item.GetFiles();
                    fileInfoList.AddRange(files);
                    GetFiles(item.GetDirectories(), fileInfoList);
                }
                catch (UnauthorizedAccessException e)
                {

                }
            }

        }

        private void ScanAndUpdate(FileInfo fileInfo)
        {
            FileIdentity id = new FileIdentity
            {
                CheckSum = 0,
                FilePath = fileInfo.FullName,
                LastModified = fileInfo.LastWriteTime,
                Name = fileInfo.Name,
                Size = fileInfo.Length,
                Type = fileInfo.Extension
            };

            this._FileInfoDb.AddOrUpdate(id);
        }
    }
}
