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
using System.Collections;
using System.Data;

namespace log4net.Tests.Appender.AdoNet
{
    public class Log4NetParameterCollection : CollectionBase, IDataParameterCollection
    {
        #region AdoNetAppender

        private readonly Hashtable parameterNameToIndex = new Hashtable();

        protected override void OnInsertComplete(int index, object value)
        {
            base.OnInsertComplete(index, value);

            IDataParameter param = (IDataParameter)value;
            parameterNameToIndex[param.ParameterName] = index;
        }

        public int IndexOf(string parameterName)
        {
            return (int)parameterNameToIndex[parameterName];
        }

        public object this[string parameterName]
        {
            get { return InnerList[IndexOf(parameterName)]; }
            set { InnerList[IndexOf(parameterName)] = value; }
        }

        #endregion

        #region Not Implemented

        public void RemoveAt(string parameterName)
        {
            throw new NotImplementedException();
        }

        public bool Contains(string parameterName)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
