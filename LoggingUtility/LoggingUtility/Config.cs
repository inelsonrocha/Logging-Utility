using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace LoggingUtility
{
    internal class Settings
    {
        public static bool logEnabled = true;

        public static string loggerPaths = ConfigurationManager.AppSettings["LogPath"] + "";

        private static float MaxTextLogLength;
        public static float MaxTextLogLengthInMegaBytes
        {
            get
            {
                if(MaxTextLogLength == 0)
                {
                    string mlen = ConfigurationManager.AppSettings["MaxTextLogLengthInMegaBytes"] ?? "2";
                    MaxTextLogLength = float.Parse(mlen.Replace(".", ","), System.Globalization.NumberStyles.Float);
                }
                return MaxTextLogLength;
            }
            internal set
            {
                MaxTextLogLength = value;
            }
        }

        private static bool _ApenasExcepcoesJaLido = false;
        private static bool _LogApenasExcepcoes = false;
        public static bool LogApenasExcepcoes
        {
            get
            {
                if(!_ApenasExcepcoesJaLido)
                {
                    _LogApenasExcepcoes = ("" + ConfigurationManager.AppSettings["LogExceptionsOnly"]).ToLower().Contains("true");
                    _ApenasExcepcoesJaLido = true;
                }

                return _LogApenasExcepcoes;
            }
        }

        private static TimeSpan? FlushThreshold = null;
        public static TimeSpan LogFlushThreshold
        {
            get
            {
                if(FlushThreshold == null)
                {
                    string textFlushThreshold = ConfigurationManager.AppSettings["LogFlushThreshold"] ?? "";
                    int mili = 0;
                    if(int.TryParse(textFlushThreshold, out mili) && mili > 0)
                    {
                        FlushThreshold = new TimeSpan(0, 0, 0, 0, mili);
                    }
                    else
                    {
                        FlushThreshold = new TimeSpan(0, 0, 0, 0, 800);
                    }
                }
                return FlushThreshold.Value;
            }
        }

        private static TimeSpan? FlushRetryTimespan = null;
        public static TimeSpan LogFlushRetryTimespan
        {
            get
            {
                if(FlushRetryTimespan == null)
                {
                    string textFlushThreshold = ConfigurationManager.AppSettings["LogFlushRetryTimespan"] ?? "";
                    int mili = 0;
                    if(int.TryParse(textFlushThreshold, out mili) && mili > 0)
                    {
                        FlushRetryTimespan = new TimeSpan(0, 0, 0, 0, mili);
                    }
                    else
                    {
                        FlushRetryTimespan = new TimeSpan(0, 0, 0, 0, 150);
                    }
                }
                return FlushRetryTimespan.Value;
            }
        }

        private static string ArchiveTextLogConfig = ConfigurationManager.AppSettings["ArchiveTextLog"] ?? "";
        public static bool ArchiveTextLog
        {
            get
            {
                return !(ArchiveTextLogConfig.ToLower() == "false");
            }
        }
    }
}
