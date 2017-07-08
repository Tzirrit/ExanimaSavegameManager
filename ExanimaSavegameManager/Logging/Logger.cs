using System;

namespace ExanimaSavegameManager
{
    public class Logger: ILogger
    {
        public Action<string> MessageAdded { get; set; }

        public void LogMessage(string message)
        {
            if(MessageAdded != null)
            {
                MessageAdded($"{DateTime.Now.ToLocalTime().ToLongTimeString()}  {message}");
            }
        }
    }
}
