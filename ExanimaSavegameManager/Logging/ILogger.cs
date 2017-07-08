using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExanimaSavegameManager
{
    public interface ILogger
    {
        Action<string> MessageAdded { get; set; }

        void LogMessage(string message);
    }
}
