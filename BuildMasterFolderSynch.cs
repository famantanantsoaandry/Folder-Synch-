//Copyright (c) Andry OELIHARIVONY 2019 

using System.IO;
using System.ServiceProcess;
using System.Configuration;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace BuildMaster_Backup
{
    partial class BuildMasterFolderSynch : ServiceBase
    {
       
        private readonly string masterBasePath = ConfigurationManager.AppSettings["MasterBasePath"].ToString();
        private  readonly  string slaveBasePath = ConfigurationManager.AppSettings["SlaveBasePath"].ToString();
        private readonly string[] folders = ConfigurationManager.AppSettings["FoldersToMonitors"].ToString().Split(',');
        private readonly Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();
        /// <summary>
        /// 
        /// </summary>
        public BuildMasterFolderSynch()
        {
            InitializeComponent();
            folders.ToList().ForEach(folder=> SetupWatcher(string.Concat(masterBasePath, "\\", folder), OnFolderChanged));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
         
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void OnStop()
        {
            foreach (var watcher in watchers)
                watcher.Value.Dispose();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        private FileSystemWatcher SetupWatcher(string source, Action<object,FileSystemEventArgs> callBack)
        {
            var watcher = new FileSystemWatcher();
            watcher.Filter = "*.*";
            watcher.Path = source;
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Changed += new FileSystemEventHandler(callBack);
            watcher.Created += new FileSystemEventHandler(callBack);
            watcher.Deleted += new FileSystemEventHandler(callBack);
            watcher.EnableRaisingEvents = true;

            return watcher;

        }

        #region Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void OnFolderChanged(object sender, FileSystemEventArgs e)
        {

            var path = e.FullPath;
            var type = e.ChangeType;
            var destpath = e.FullPath.Replace(masterBasePath, slaveBasePath);

            try
            {

                switch (type)
                {

                    case WatcherChangeTypes.Created:
                        if (Directory.Exists(path))
                            CopyFolder(path, destpath);
                        else if(IsFileClosed(path,true))
                            File.Copy(path, destpath, true);
                        break;
                    case WatcherChangeTypes.Changed:
                        if (Directory.Exists(path))
                            CopyFolder(path, destpath);
                        else if (IsFileClosed(path, true))
                            File.Copy(path, destpath, true);
                        break;
                    case WatcherChangeTypes.Deleted:
                        if (Directory.Exists(destpath))
                            Directory.Delete(destpath,true);
                        else if(File.Exists(destpath))
                            File.Delete(destpath);
                        break;

                }
                

            }
            catch (Exception ex)
            {
                return;
            }

        }
        #endregion

        #region I\O Utilities
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="destFolder"></param>

        private void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest);
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="wait"></param>
        /// <returns></returns>

        private  bool IsFileClosed(string filepath, bool wait)
        {
            bool fileClosed = false;
            int retries = 20;
            const int delay = 1000; // Max time spent here = retries*delay milliseconds

            if (!File.Exists(filepath))
                return false;

            do
            {
                try
                {
                    // Attempts to open then close the file in RW mode, denying other users to place any locks.
                    FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    fs.Flush();
                    fs.Close();
                    fileClosed = true; // success
                }
                catch (IOException) { }

                if (!wait) break;

                retries--;

                if (!fileClosed)
                    Thread.Sleep(delay);
            }
            while (!fileClosed && retries > 0);

            return fileClosed;
        }

        #endregion
    }
}
