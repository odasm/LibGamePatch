//
//  Patcher.cs
//
//  Author:
//       Markus Kostrzewski <mkostrzewski@phoenix-dev.de>
//
//  Copyright (c) 2016 Phoenix Development
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace LibGamePatch
{
    public class Patcher
    {
        public Patcher(string patchServer)
        {
            if (!string.IsNullOrWhiteSpace(patchServer))
            {
                server = patchServer;
            }
            else
            {
                throw new ArgumentException("Argument patchServer is Null or Whitespace.", (Exception)null);
            }
        }

        public event PatcherProgressChangedHandler PatcherProgressChanged;
        public event PatcherStatusChangedHandler PatcherStatusChanged;
        public event PatcherCompletedHandler PatcherCompleted;

        public void StartPatchAsync()
        {
            updater.DoWork += Updater_DoWork;
            updater.RunWorkerCompleted += Updater_RunWorkerCompleted;
            updater.ProgressChanged += Updater_ProgressChanged;
            if (PatcherProgressChanged != null)
            {
                PatcherProgressChanged(this, new PatcherProgressChangedArgs(0));
            }
            if (PatcherStatusChanged != null)
            {
                PatcherStatusChanged(this, new PatcherStatusChangedArgs("Checking for updates..."));
            }
            updater.WorkerReportsProgress = true;
            updater.RunWorkerAsync();
        }

        #region Private Code
        private readonly BackgroundWorker updater = new BackgroundWorker();

        private bool blocked = false;
        private int errortype = 0;
        private string server = "";
        private string file = "";
        private string patch = "";

        private void Updater_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            switch (errortype)
            {
                case 0:
                    if (PatcherCompleted != null)
                    {
                        PatcherCompleted(this, new PatcherCompletedArgs(0));
                    }
                    break;
                case 1:
                    if (PatcherCompleted != null)
                    {
                        PatcherCompleted(this, new PatcherCompletedArgs(1));
                    }
                    if (PatcherStatusChanged != null)
                    {
                        PatcherStatusChanged(this, new PatcherStatusChangedArgs("Error while getting the version information. Please try again later."));
                    }
                    break;
                case 2:
                    if (PatcherCompleted != null)
                    {
                        PatcherCompleted(this, new PatcherCompletedArgs(1));
                    }
                    if (PatcherStatusChanged != null)
                    {
                        PatcherStatusChanged(this, new PatcherStatusChangedArgs("Error while parsing update data. Please try again."));
                    }
                    break;
            }
        }

        private void Updater_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            PatcherProgressChanged(this, new PatcherProgressChangedArgs(e.ProgressPercentage));
        }

        private void Updater_DoWork(object sender, DoWorkEventArgs e)
        {

            int lver;
            int over;
            try
            {
                lver = int.Parse(File.ReadAllText("Update.dat"));
            }
            catch
            {
                lver = 0;
            }

            WebClient wcs = new WebClient();

            try
            {
                over = int.Parse(wcs.DownloadString(server + "version.txt"));
            }
            catch
            {
                over = 0;
                errortype = 1;
                return;
            }

            int missingvers = over - lver;
            int i = 1;
            if (missingvers != 0)
            {
                if (PatcherStatusChanged != null)
                {
                    PatcherStatusChanged(this, new PatcherStatusChangedArgs("Found " + missingvers + " missing updates"));
                }
            }

            while (lver != over)
            {
                List<Patch> patches = new List<Patch>();
                string[] patchstring = wcs.DownloadString(server + (lver + 1) + "/list.txt").Split(Environment.NewLine.ToCharArray());
                foreach (string s in patchstring)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        string local = s.Split(';')[0];
                        string patch = s.Split(';')[1];
                        if (s.Split(';')[2] == "true")
                        {
                            patches.Add(new Patch(local, patch, true));
                        }
                        else
                        {
                            patches.Add(new Patch(local, patch, false));
                        }
                    }
                }

                if (PatcherStatusChanged != null)
                {
                    PatcherStatusChanged(this, new PatcherStatusChangedArgs("Downloading update " + i + " of " + missingvers + "..."));
                }
                i++;
                int ip = 1;
                foreach (Patch p in patches)
                {
                    updater.ReportProgress(0);
                    if (PatcherStatusChanged != null)
                    {
                        PatcherStatusChanged(this, new PatcherStatusChangedArgs("Downloading patch " + ip + " of " + patches.Count + "..."));
                    }
                    ip++;
                    WebClient wc = new WebClient();
                    wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                    wc.DownloadFileCompleted += Wc_DownloadFileCompleted;

                    wc.DownloadFileAsync(new Uri(server + (lver + 1) + "/" + p.patchFile), p.patchFile);
                    blocked = true;
                    while (blocked)
                    {
                        //nop
                    }
                    if (p.isNewFile)
                    {
                        File.Move(p.patchFile, p.localFile);
                        if (PatcherStatusChanged != null)
                        {
                            PatcherStatusChanged(this, new PatcherStatusChangedArgs("Patch " + p.patchFile + " applied successfully!"));
                        }
                    }
                    else
                    {
                        file = p.localFile;
                        patch = p.patchFile;
                        PatchFile();
                        while (blocked)
                        {
                            //nop
                            if (errortype == 2)
                            {
                                return;
                            }
                        }
                        if (PatcherStatusChanged != null)
                        {
                            PatcherStatusChanged(this, new PatcherStatusChangedArgs("Patch " + patch + " applied successfully!"));
                        }
                    }
                }

                try
                {
                    lver = int.Parse(File.ReadAllText("Update.dat"));
                }
                catch
                {
                    errortype = 2;
                    return;
                }
            }
            if (missingvers == 0)
            {
                if (PatcherStatusChanged != null)
                {
                    PatcherStatusChanged(this, new PatcherStatusChangedArgs("No updates found!"));
                }
            }
            else
            {
                if (PatcherStatusChanged != null)
                {
                    PatcherStatusChanged(this, new PatcherStatusChangedArgs(missingvers + " updates applied successfully!"));
                }
            }
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            updater.ReportProgress(e.ProgressPercentage);
        }

        private void Wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            blocked = false;
        }

        private void PatchFile()
        {
            if (PatcherStatusChanged != null)
            {
                PatcherStatusChanged(this, new PatcherStatusChangedArgs("Applying patch " + patch + "..."));
            }
            try
            {

                FileInfo fi1 = new FileInfo(file);
                FileInfo fi2 = new FileInfo(patch);
                FileInfo fi3 = new FileInfo(file + ".tmp");

                while (true)
                {
                    if (IsFileAvailable(fi1) || IsFileAvailable(fi2) || IsFileAvailable(fi3))
                    {
                        using (var outStream = new FileStream(file + ".tmp", FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                        {
                            using (FileStream inStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                using (Stream patchStream = new FileStream(patch, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    outStream.Position = 0;
                                    Xdelta.Decoder decoder = new Xdelta.Decoder(inStream, patchStream, outStream);
                                    decoder.ProgressChanged += delegate (double p)
                                    {
                                        updater.ReportProgress((int)(p * 100));
                                    };
                                    decoder.Run();
                                    break;
                                }
                            }
                        }
                    }
                }

                while (true)
                {
                    if (IsFileAvailable(fi1) || IsFileAvailable(fi2) || IsFileAvailable(fi3))
                    {

                        File.Delete(file);
                        File.Delete(patch);
                        File.Move(file + ".tmp", file);
                        break;
                    }
                }
                blocked = false;
            }
            catch
            {
                errortype = 2;
                return;
            }
        }

        private bool IsFileAvailable(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return false;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return true;
        }
        #endregion
    }
}
