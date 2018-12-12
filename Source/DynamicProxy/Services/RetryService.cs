using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DynamicProxy.Services
{
    public class RetryService
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Excecute(int _retryMax, int _retryWait, Action _retryCode)
        {
            var count = 0;
            var timeElapsed = true;
            var timer = RetryService.GetTimer(_retryWait, () => { timeElapsed = true; count++; logger.Debug("RetryLogic.Excecute -> Awaited : " + _retryWait); });

            while (true)
            {
                if (!timeElapsed) continue;
                timeElapsed = false;
                try
                {
                    logger.Info("RetryLogic.Excecute -> Calling service");
                    _retryCode();
                    break;
                }
                catch (Exception ex)
                {
                    if (count < _retryMax)
                    {
                        //Log the try
                        logger.Info("RetryLogic.Excecute -> Retry N: " + (count + 1));
                        logger.Info(ex.Message);
                        timer.Start();
                        continue;
                    }
                    else
                    {
                        logger.Info("RetryLogic.Excecute -> Retried Max");
                        throw ex;
                    }
                }
            }
        }

        private static Timer GetTimer(int _awaitAmount, Action _onEnd)
        {
            var timer = new Timer(_awaitAmount * 1000);
            timer.Elapsed += (s, e) =>
            {
                _onEnd();
            };
            return timer;
        }
    }
}
