using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;


namespace LoggingUtility
{
    public class BasicTextLogger : ILogImplementation
    {
        private ReaderWriterLock WriterLock = new ReaderWriterLock();
        private DateTime LastArchiveTime = DateTime.Now.AddDays(-10);

        private TipoLog TipoDeLog { get; set; }
        public string LogPath { get; set; }

        private object toFlushLock = new object();

        internal BasicTextLogger(string FilePath, TipoLog tipoLog)
        {
            if(!string.IsNullOrEmpty(FilePath))
            {
                TipoDeLog = tipoLog;
                LogPath = FilePath;
            }
        }

        internal bool ArchiveLog()
        {
            if(Settings.ArchiveTextLog && File.Exists(LogPath) && DateTime.Now > LastArchiveTime.AddSeconds(20))
            {
                var finfo = new FileInfo(LogPath);
                var mega = 1048576f; // 1 megabyte
                var sizeCompare = (finfo.Length / mega);
                if(sizeCompare > Settings.MaxTextLogLengthInMegaBytes)
                {
                    string dir = Path.GetDirectoryName(LogPath);
                    string fileName = Path.GetFileName(LogPath);
                    string newName = String.Format("{0}\\_{1}_{2}{3}", dir, Path.GetFileNameWithoutExtension(LogPath), DateTime.Now.Ticks, Path.GetExtension(fileName));
                    File.Move(LogPath, newName);

                    Thread t = new Thread(ZipArchive);
                    t.Start(new string[] { dir, newName, Path.GetFileNameWithoutExtension(newName) });
                }
                LastArchiveTime = DateTime.Now;
            }
            return true;
        }

        private void ZipArchive(object names)
        {
            string[] namesArr = (string[])names;
            FastZip fZip = new FastZip();
            fZip.CreateZip(string.Format("{0}\\{1}.zip", namesArr[0], namesArr[2]), namesArr[0], false, Path.GetFileName(namesArr[1]));
            File.Delete(namesArr[1]);
        }

        public void FlushLog(List<Evento> buffer)
        {
            try
            {
                WriterLock.AcquireWriterLock(Timeout.Infinite);

                string bufferBlock = "";

                if(buffer.Count > 0)
                {
                    bufferBlock = string.Join(Environment.NewLine,
                        (from t in buffer
                         select t.ToString(TipoDeLog)).ToArray());
                }

                try
                {
                    if(!string.IsNullOrEmpty(bufferBlock))
                    {
                        lock(toFlushLock)
                        {
                            ArchiveLog();
                            using(StreamWriter sWriter = new StreamWriter(LogPath, true, Encoding.UTF8))
                            {
                                sWriter.WriteLine(bufferBlock);
                            }
                        }
                    }
                }
                finally
                {
                    WriterLock.ReleaseWriterLock();
                }
            }
            catch(IOException io)
            {
                Console.WriteLine(io.Message);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
