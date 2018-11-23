using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace CoreDRXLibrary.Tests.Mocks
{
    class MockLogger : ILog
    {
        public bool IsDebugEnabled => true;

        public bool IsInfoEnabled => true;

        public bool IsWarnEnabled => true;

        public bool IsErrorEnabled => true;

        public bool IsFatalEnabled => true;

        public ILogger Logger { get => LoggerInt; set { LoggerInt = value; } }
        private ILogger LoggerInt;

        public MockLogger()
        {
            Logger = this as ILogger;
        }

        public void Debug(object message)
        {

        }

        public void Debug(object message, Exception exception)
        {

        }

        public void DebugFormat(string format, params object[] args)
        {

        }

        public void DebugFormat(string format, object arg0)
        {

        }

        public void DebugFormat(string format, object arg0, object arg1)
        {

        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {

        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {

        }

        public void Error(object message)
        {

        }

        public void Error(object message, Exception exception)
        {

        }

        public void ErrorFormat(string format, params object[] args)
        {
            
        }

        public void ErrorFormat(string format, object arg0)
        {
            
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            
        }

        public void Fatal(object message)
        {
            
        }

        public void Fatal(object message, Exception exception)
        {
            
        }

        public void FatalFormat(string format, params object[] args)
        {
            
        }

        public void FatalFormat(string format, object arg0)
        {
            
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            
        }

        public void Info(object message)
        {
            
        }

        public void Info(object message, Exception exception)
        {
            
        }

        public void InfoFormat(string format, params object[] args)
        {
            
        }

        public void InfoFormat(string format, object arg0)
        {
            
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            
        }

        public void Warn(object message)
        {
            
        }

        public void Warn(object message, Exception exception)
        {
            
        }

        public void WarnFormat(string format, params object[] args)
        {
            
        }

        public void WarnFormat(string format, object arg0)
        {
            
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            
        }
    }
}
