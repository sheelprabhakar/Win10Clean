using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;

namespace Win10Clean
{
    class FileIdentity
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }

        public long Size { get; set; }

        public DateTime LastModified { get; set; }

        public long CheckSum { get; set; }

        public string Type { get; set; }

        public DateTime LastUpdated { get; set; }

        
    }

    class FileInfoDb : IDisposable
    {
        private static FileInfoDb infoDb;
        private LiteDatabase db = new LiteDatabase(@"../database.db");

        private ILiteCollection<FileIdentity> fileCollection;
        private FileInfoDb()
        {
            this.fileCollection = this.GetFileInfoDBList();
        }

        public static FileInfoDb Intance
        {
            get
            {
                if (infoDb == null)
                {
                    infoDb = new FileInfoDb();
                }
                return infoDb;
            }
        }

        public void AddOrUpdate(FileIdentity file)
        {
            FileIdentity fileOld = this.Find(file.FilePath);
            file.LastModified = DateTime.Now;
            if (fileOld == null)
            {
                this.fileCollection.Insert(file);
                this.fileCollection.EnsureIndex(x => x.FilePath);
            }
            else
            {
                file.Id = fileOld.Id;
                this.fileCollection.Update(file);
            }
            this.db.Commit();
        }

        public FileIdentity Find(string path)
        {
            return this.fileCollection.FindOne(x => x.FilePath.Equals(path));
        }

        public IEnumerable<FileIdentity> FindAll(string folder)
        {
            return this.fileCollection.Find(x => x.FilePath.StartsWith(folder)).OrderByDescending(x => x.Name).ThenBy(x => x.Size);
        }
        private ILiteCollection<FileIdentity> GetFileInfoDBList()
        {
            return this.db.GetCollection<FileIdentity>(BsonAutoId.Int64);
        }
        public void Dispose()
        {
            if (this.db != null)
            {
                this.db.Commit();
                this.db.Dispose();
            }
            throw new NotImplementedException();
        }
    }
}
