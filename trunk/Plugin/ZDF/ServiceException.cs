namespace ZDFMediathek2009.Code
{
    using System;

    public class ServiceException : Exception
    {
        private string _debugInfo;
        private ServiceExceptionType _exceptionType;

        public ServiceException()
        {
        }

        public ServiceException(string dubugInfo, ServiceExceptionType exceptionType)
        {
            this.DebugInfo = dubugInfo;
            this.ExceptionType = exceptionType;
        }

        public string DebugInfo
        {
            get
            {
                return this._debugInfo;
            }
            set
            {
                this._debugInfo = value;
            }
        }

        public ServiceExceptionType ExceptionType
        {
            get
            {
                return this._exceptionType;
            }
            set
            {
                this._exceptionType = value;
            }
        }
    }
}

