using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoggingUtility
{
    public interface ILogImplementation
    {
        void FlushLog(List<Evento> buffer);
    }
}
