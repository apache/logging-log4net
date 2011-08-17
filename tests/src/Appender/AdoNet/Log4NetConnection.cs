using System;
using System.Data;

namespace log4net.Tests.Appender.AdoNet
{
    public class Log4NetConnection : IDbConnection
    {
        #region AdoNetAppender

        private static Log4NetConnection mostRecentInstance;

        private bool open;
        private string connectionString;

        public Log4NetConnection()
        {
            mostRecentInstance = this;
        }

        public void Close()
        {
            open = false;
        }

        public ConnectionState State
        {
            get 
            {
                return open ? ConnectionState.Open : ConnectionState.Closed;
            }
        }

        public string ConnectionString
        {
            get { return connectionString; }
            set { connectionString = value; }
        }

        public IDbTransaction BeginTransaction()
        {
            return new Log4NetTransaction();
        }

        public IDbCommand CreateCommand()
        {
            return new Log4NetCommand();
        }

        public void Open()
        {
            open = true;
        }

        public static Log4NetConnection MostRecentInstance
        {
            get { return mostRecentInstance; }
        }

        #endregion

        #region Not Implemented

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            throw new NotImplementedException();
        }

        public void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public int ConnectionTimeout
        {
            get { throw new NotImplementedException(); }
        }

        public string Database
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
