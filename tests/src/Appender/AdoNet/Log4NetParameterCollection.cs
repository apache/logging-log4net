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
