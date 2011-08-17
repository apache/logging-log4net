using System;
using System.Data;

namespace log4net.Tests.Appender.AdoNet
{
    public class Log4NetTransaction : IDbTransaction
    {
        #region AdoNetAppender

        public void Commit()
        {
            // empty
        }

        public void Rollback()
        {
            // empty
        }

        #endregion

        #region Not Implemented

        public IDbConnection Connection
        {
            get { throw new NotImplementedException(); }
        }

        public IsolationLevel IsolationLevel
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
