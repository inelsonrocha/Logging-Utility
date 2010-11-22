using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace LoggingUtility
{
    internal enum TipoLog
    {
        json,
        texto,
        textoMinimo,
        xmlfragment
    }
    public sealed class Logger
    {
        private Logger()
        {
        }

        #region private fields
        private static bool _initDone = false;
        private static object initLock = new object();

        private static volatile List<ILogImplementation> _LogInstances;

        private static bool Dirty;
        private static object syncObj = new object();

        private static List<Evento> buffer = null;
        private static List<Evento> toWrite = new List<Evento>();
        private static object toWriteLock = new object();

        private static ReaderWriterLock TimerLock = new ReaderWriterLock();
        private static ReaderWriterLock WriterLock = new ReaderWriterLock();

        private static Timer tmr;

        private static List<ILogImplementation> LogInstances
        {
            get
            {
                if(_LogInstances == null && Settings.logEnabled)
                {
                    lock(syncObj)
                    {
                        InitLogInstances();
                    }
                }
                return _LogInstances;
            }
        }

        private static void InitLogInstances()
        {
            try
            {
                if(_LogInstances == null && Settings.logEnabled)
                {
                    _LogInstances = new List<ILogImplementation>();
                    InitTextLoggers();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Settings.logEnabled = false;
            }
        }

        private static void InitTextLoggers()
        {
            if(!string.IsNullOrEmpty(Settings.loggerPaths))
            {
                string[] logs = Settings.loggerPaths.Split(';');

                BasicTextLogger tLog;
                foreach(string loginstance in logs)
                {
                    if(loginstance.IndexOf(",") > 1)
                    {
                        string[] loginstanceSettings = loginstance.Trim(new char[] { '(', ')' }).Split(',');
                        TipoLog tipo = (TipoLog)Enum.Parse(typeof(TipoLog), loginstanceSettings[1]);
                        tLog = new BasicTextLogger(loginstanceSettings[0], tipo);
                        _LogInstances.Add(tLog);
                    }
                    else if(!string.IsNullOrEmpty(loginstance))
                    {
                        TipoLog tipo = TipoLog.textoMinimo;
                        tLog = new BasicTextLogger(loginstance.Trim(new char[] { '(', ')' }), tipo);
                        _LogInstances.Add(tLog);
                    }
                }
            }
        }

        private static void DisposeTimer()
        {
            if(toWrite.Count == 0 && tmr != null)
            {
                tmr.Dispose();
                tmr = null;
                Dirty = false;
            }
        }

        private static void InvoqueFlushControl()
        {
            if(!Dirty)
            {
                if(tmr == null)
                {
                    LockCookie up = TimerLock.UpgradeToWriterLock(200);
                    if(tmr == null)
                    {
                        Dirty = true;
                        TimerCallback tmrCallBack = new TimerCallback(Logger.Flush);
                        tmr = new Timer(tmrCallBack, null, TimeSpan.Zero, Settings.LogFlushRetryTimespan);
                    }
                    TimerLock.DowngradeFromWriterLock(ref up);
                }
            }
        }

        private class ToFlushData
        {
            public ILogImplementation Log;
            public List<Evento> Buffer;
        }

        private static void Flush(object state)
        {
            try
            {
                WriterLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    buffer = null;

                    if(toWrite.Count > 0)
                    {
                        lock(toWriteLock)
                        {
                            buffer = new List<Evento>(toWrite);
                            toWrite.Clear();
                        }
                    }

                    if(buffer != null && buffer.Count > 0)
                    {
                        foreach(var logs in LogInstances)
                        {
                            Thread ta = new Thread(new ParameterizedThreadStart(ThreadedFlushLog));
                            ta.Start(new ToFlushData() { Log = logs, Buffer = buffer });
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
                throw io;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }

            DisposeTimer();
        }

        private static void ThreadedFlushLog(object dat)
        {
            ToFlushData data = (ToFlushData)dat;

            data.Log.FlushLog(data.Buffer);
        }

        private static void Init()
        {
            if(!_initDone)
            {
                lock(initLock)
                {
                    if(!_initDone)
                    {
                        InitTextLogger();

                        _initDone = true;
                    }
                }
            }
        }

        private static void InitTextLogger()
        {
            var li = (from l in LogInstances
                      select l).OfType<BasicTextLogger>();

            foreach(var textL in li)
            {
                textL.ArchiveLog();
            }
        }
        #endregion

        public static bool Log(string message)
        {
            if(Settings.LogApenasExcepcoes)
                return true;

            return LogBoilerplate(new Evento(message));
        }

        public static bool Log(object obj)
        {
            if(Settings.LogApenasExcepcoes)
                return true;

            return LogBoilerplate(new Evento(obj.ToString()));
        }

        public static bool Log(Exception excepcao)
        {
            if(excepcao.InnerException != null)
            {
                Log(excepcao.InnerException);
            }

            return LogBoilerplate(new Evento(excepcao));
        }

        private static bool LogBoilerplate(Evento ev)
        {
            if(!_initDone)
            {
                Init();
            }
            InvoqueFlushControl();
            try
            {
                lock(toWriteLock)
                {
                    toWrite.Add(ev);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }

}
