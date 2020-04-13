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
    public class Log4NetParameter : IDbDataParameter
    {
        #region AdoNetAppender

        private string parameterName;
        private byte precision;
        private byte scale;
        private int size;
        private DbType dbType;
        private object value;

        public string ParameterName
        {
            get { return parameterName; }
            set { parameterName = value; }
        }

        public byte Precision
        {
            get { return precision; }
            set { precision = value; }
        }

        public byte Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        public int Size
        {
            get { return size; }
            set { size = value; }
        }

        public DbType DbType
        {
            get { return dbType; }
            set { dbType = value; }
        }

        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        #endregion

        #region Not Implemented

        public ParameterDirection Direction
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsNullable
        {
            get { throw new NotImplementedException(); }
        }

        public string SourceColumn
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public DataRowVersion SourceVersion
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion
    }
}
