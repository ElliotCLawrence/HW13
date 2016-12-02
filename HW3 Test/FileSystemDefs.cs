//Elliot Lawrence
//HW8
//CS 422
//10_21_2016




using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CS422
{
    public abstract class Dir422
    {
        public abstract string Name { get; }

        public abstract IList<Dir422> GetDirs();

        public abstract List<File422> GetFiles();

        public abstract Dir422 Parent { get; }

        public abstract bool ContainsFile(string fileName, bool recursive);

        public abstract bool ContainsDir(string dirName, bool recursive);

        public abstract Dir422 getDir(string name);

        public abstract File422 GetFile(string name);

        public abstract File422 CreateFile(string name);

        public abstract Dir422 CreateDir(string name);



    }

    public abstract class File422
    {
        public abstract string Name { get; }

        public abstract Dir422 Parent { get; }

        public abstract Stream OpenReadOnly();

        public abstract Stream OpenReadWrite();
    }

    public abstract class FileSys422
    {
        public abstract Dir422 GetRoot();

        public virtual bool Contains(File422 file)
        {
            return Contains(file.Parent);
        }

        public virtual bool Contains(Dir422 dir)
        {
            if (dir == null) { return false; }
            if (dir == this.GetRoot()) { return true; }
            return Contains(dir.Parent);
        }
    }



    public class StandardFileSystem : FileSys422
    {
        static Dir422 root; //shared between all FS

        public override Dir422 GetRoot()
        {
            return root;
        }

        public static StandardFileSystem Create(string rootDir)
        {
            root = new StdFSDir(rootDir);
            return new StandardFileSystem();
        }
    }

    public class StdFSDir : Dir422
    {
        private string m_path;       
        

        public StdFSDir(string path)
        {
            m_path = path;
        }

        public override string Name
        {
            get
            {
                return Path.GetFileName(m_path);
            }
        }

        public override Dir422 Parent
        {
            get
            {
                return new StdFSDir(Directory.GetParent(m_path).FullName);
            }
        }

        public override bool ContainsDir(string dirName, bool recursive) //checks if the directory contains the directory
        {
            if (dirName == null || dirName == "" || dirName.Contains("/") || dirName.Contains("\\"))
            {
                return false;
            }


            foreach (string dir in Directory.GetDirectories(m_path)) 
            {
                if (Path.GetDirectoryName(dir) == dirName)
                {
                    return true;
                }
            }

            if (recursive == true) //if recursive not true, return false
            {
                foreach (Dir422 child in this.GetDirs()) //if it is true, search children
                {
                    if (child.ContainsDir(dirName, true))
                        return true;
                }
            }

            return false;
        }

        public override bool ContainsFile(string fileName, bool recursive)//checks if the directory contains the file
        {
            if (fileName == null || fileName == "" || fileName.Contains("/") || fileName.Contains("\\"))
                return false;

            foreach (string file in Directory.GetFiles(m_path))
            {
                if (Path.GetFileName(file) == fileName)
                    return true;
            }
            return false;
        }

        public override Dir422 CreateDir(string name)//creates a directory and gives back a copy
        {
            if (name == null || name == "" || name.Contains("/") || name.Contains("\\"))
                return null;

            string fullName = m_path+"/"+name;
            if (Directory.CreateDirectory(fullName) != null)
                return new StdFSDir(fullName);
            else
                return null;

        }

        public override File422 CreateFile(string name)//creates a file and gives back a copy
        {
            if (name == null || name == "" || name.Contains("/") || name.Contains("\\"))
                return null;

            string fullName = m_path + "/" + name;
            if (File.Create(fullName) != null)
                return new StdFSFile(fullName);
            else
                return null;
        }

        public override Dir422 getDir(string name)//gets a directory if it exists in current directory
        {
            string fullName = m_path + "/" + name;
            string windowsName = m_path + "\\" + name;
            foreach (string dir in Directory.GetDirectories(m_path))
            {
                if (dir == fullName || dir == windowsName)
                    return new StdFSDir(fullName);
            }

                return null;
        }

        public override IList<Dir422> GetDirs()//gets a directory if it exists in current directory
        {
            List<Dir422> dirs = new List<Dir422>();
            foreach (string dir in Directory.GetDirectories(m_path))
            {
                dirs.Add(new StdFSDir(dir));
            }
            return dirs;
        }

        public override File422 GetFile(string name)//returns a file if it exists in current directory
        {
            string fullName = m_path + "/" + name;
            string windowsName = m_path + "\\" + name;

            foreach (string file in Directory.GetFiles(m_path))
            {
                if (file == fullName || file == windowsName)
                    return new StdFSFile(fullName);
            }

            return null;
        }

        public override List<File422> GetFiles() //returns all files in current directory
        {
            List<File422> files = new List<File422>();
            foreach (string file in Directory.GetFiles(m_path))
            {
                files.Add(new StdFSFile(file));
            }
            return files;
        }
    }

    public class StdFSFile : File422
    {
        private string m_path;

        public StdFSFile(string path) { m_path = path; }
        
        public override string Name
        {
            get
            {
                return Path.GetFileName(m_path);
            }
        }

        public override Dir422 Parent
        {
            get
            {
                return new StdFSDir(Directory.GetParent(m_path).FullName);
            }
        }

        public override Stream OpenReadOnly() //one line function return a stream with m_path in it
        {
            return File.Open(m_path, FileMode.Open, FileAccess.Read);
        }

        public override Stream OpenReadWrite() //one line function return a stream with m_path in it
                                               //if you fail to open this stream, return null, don't throw exception
        {
            return File.Open(m_path, FileMode.Open, FileAccess.ReadWrite);
        }
    }


    public class MemoryFileSystem : FileSys422
    {
        MemFSDir root; 

        public override Dir422 GetRoot()
        {
            return root;
        }

        public MemoryFileSystem()
        {
            root = new MemFSDir("/", null);
        }
    }

    public class MemFSDir : Dir422
    {
        private string dirName;
        private MemFSDir dirParent;
        private List<Dir422> directoryChildren;
        private List<File422> fileChildren;
        private string name;

        public MemFSDir(string name, MemFSDir parentD) //this is the constructor for this class
        {
            this.name = name;
            dirParent = parentD;
            directoryChildren = new List<Dir422>();
            fileChildren = new List<File422>();
        }

        public override string Name //getter for name
        {
            get
            {
                return dirName;
            }
        }

        public override Dir422 Parent //getter for parent
        {
            get
            {
                return dirParent;
            }
        }

        public override bool ContainsDir(string dirName, bool recursive) //checks if the directory contains the child
        {

            if (dirName == null || dirName == "" || dirName.Contains("/") || dirName.Contains("\\"))
                return false;


            foreach (MemFSDir child in directoryChildren) //check all the files in this folder for a match
            {
                if (child.Name == dirName)
                    return true;
            }
            
            if (recursive == true) //if recursive not true, return false
            {
                foreach (MemFSDir child in directoryChildren) //if it is true, search children
                {
                    if (child.ContainsDir(dirName, true))
                        return true;
                }
            }

            return false;
        }

        public override bool ContainsFile(string fileName, bool recursive) //checks if the directory contains the file
        {
            if (fileName == null || fileName == "" || fileName.Contains("/") || fileName.Contains("\\"))
                return false;

            foreach (MemFSFile child in fileChildren) //check all the files in this folder for a match
            {
                if (child.Name == fileName)
                    return true;
            }

            if (recursive == true) //if recursive not true, return false
            {
                foreach (MemFSDir child in directoryChildren) //if it is true, search children
                {
                    if (child.ContainsFile(dirName, true))
                        return true;
                }
            }

            return false;
        }

        public override Dir422 CreateDir(string name) //creates a directory and gives back a copy
        {
            if (name == null || name == "" || name.Contains("/") || name.Contains("\\"))
                return null;
            directoryChildren.Add(new MemFSDir(name, this));
            return directoryChildren[directoryChildren.Count-1];
        }

        public override File422 CreateFile(string name) //creates a file and gives back a copy
        {
            if (name == null || name == "" || name.Contains("/") || name.Contains("\\"))
                return null;
            fileChildren.Add(new MemFSFile(name, this));
            return fileChildren[fileChildren.Count - 1];
        }

        public override Dir422 getDir(string name) //gets a directory if it exists in current directory
        {
            for (int x = 0; x < directoryChildren.Count; x++)
            {
                if (directoryChildren[x].Name == name)
                    return directoryChildren[x];
            }

            return null;
        }

        public override IList<Dir422> GetDirs() //returns all directories in current directory
        {
            return directoryChildren;
        }

        public override File422 GetFile(string name) //returns a file if it exists in current directory
        {
            for (int x = 0; x < fileChildren.Count; x++)
            {
                if (fileChildren[x].Name == name)
                    return fileChildren[x];
            }

            return null;
        }

        public override List<File422> GetFiles() //returns all files in current directory
        {
            return fileChildren;
        }
    }

    public class MemFSFile : File422
    {
        private string fileName;
        MemFSDir parentDir;
        List<trackingMemStream> readers;
        List<trackingMemStream> writer; //this is a list so you can lock on it.
        MemoryStream data;

        public MemFSFile(string name, MemFSDir parent)
        {
            this.fileName = name;
            parentDir = parent;
            readers = new List<trackingMemStream>() ;
            writer = new List<trackingMemStream>(); 
            data = new MemoryStream();
        }

        public MemoryStream mdata
        {
            get
            {
                return data;
            }
        }

        public override string Name
        {
            get
            {
                return fileName;
            }
        }

        public override Dir422 Parent
        {
            get
            {
                return parentDir;
            }
        }

        public override Stream OpenReadOnly() 
        {
            lock (writer)
            {
                if (writer.Count == 0)
                    return new trackingMemStream(false, this);
                return null;
            }
               
            
        }

        public override Stream OpenReadWrite()
        {
           lock (writer)
            {
                if (writer.Count > 0 || readers.Count > 0) //don't create a writer if there is 1 reader or 1 writer
                    return null;
                else
                {
                    writer.Add(new trackingMemStream(true, this));
                    return writer[0]; //writer is only ever size 1 or 0
                }
            }
        }

        class trackingMemStream : MemoryStream //custom memory stream
        {
            MemoryStream actualStream;
            private bool canWrite;
            MemFSFile file;
            long position;

            public trackingMemStream(bool write, MemFSFile originFile) //constructor class
            {
                    actualStream = new MemoryStream();
                    originFile.data.CopyTo(actualStream);
                    actualStream.Position = 0;
                    canWrite = write;
                    file = originFile;
                    position = 0;
            }

            public override bool CanWrite
            {
                get
                {
                    if (canWrite && actualStream.CanWrite)
                        return true;
                    else
                        return false;
                }
            }

            public override bool CanRead
            {
                get
                {
                    return actualStream.CanRead;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return actualStream.CanSeek;
                }
            }

            public override long Length
            {
                get
                {
                    return actualStream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return actualStream.Position;
                }

                set
                {
                    actualStream.Position = value;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (this.CanRead)
                    return actualStream.Read(buffer, offset, count);
                return 0;
            }

            public override void Write(byte[] buffer, int offset, int count) //write to this stream, and the data stream which holds onto data just like a file would
            {
                if (this.CanWrite)
                {
                    file.mdata.Position = position; //set the position of the other stream before writing
                    actualStream.Write(buffer, offset, count);
                    file.mdata.Write(buffer, offset, count);
                    return;
                }
                    
                else
                    throw new NotSupportedException();
            }

            public override void Close()
            {
                
                if (this.canWrite)
                {
                    lock (file.writer)
                    {
                        file.mdata.Position = 0;
                        file.writer.Remove(this);
                    }
                    
                }
                else //reader
                {
                    lock (file.readers)
                    {
                        file.readers.Remove(this);
                    }
                }

                actualStream.Close();
                base.Close();
            }

            public override long Seek(long offset, SeekOrigin loc)
            {
                return actualStream.Seek(offset, loc);
            }

            public override void Flush()
            {
                actualStream.Flush();
            }

            public override void SetLength(long value)
            {
                actualStream.SetLength(value);
            }
        } //end special stream
    }
}

//overwrite observable stream class