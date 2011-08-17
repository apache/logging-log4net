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
