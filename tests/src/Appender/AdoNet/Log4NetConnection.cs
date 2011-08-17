/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

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
