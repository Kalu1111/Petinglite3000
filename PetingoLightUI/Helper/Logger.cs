using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetingoLightUI.Helper
{
    public static class Logger
    {
        static NLog.Logger logger = LogManager.LoadConfiguration("NLog.config").GetCurrentClassLogger();

        public static void Info(string msg)
        {
            logger.Info(msg);
        }
        public static void Error(string msg)
        {
            logger.Error(msg);
        }
        public static void Fatal(string msg)
        {
            logger.Fatal(msg);
        }
    }
}
