using System;

namespace log4net.Util
{
    /// <summary>
    /// Wrapper class used to map converter names to converter types
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pattern converter info class used during configuration by custom
    /// PatternString and PatternLayer converters.
    /// </para>
    /// </remarks>
    public sealed class ConverterInfo
    {
        private string m_name;
        private Type m_type;
        private readonly PropertiesDictionary properties = new PropertiesDictionary();

        /// <summary>
        /// default constructor
        /// </summary>
        public ConverterInfo()
        {
        }

        /// <summary>
        /// Gets or sets the name of the conversion pattern
        /// </summary>
        /// <remarks>
        /// <para>
        /// The name of the pattern in the format string
        /// </para>
        /// </remarks>
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        /// <summary>
        /// Gets or sets the type of the converter
        /// </summary>
        /// <remarks>
        /// <para>
        /// The value specified must extend the 
        /// <see cref="PatternConverter"/> type.
        /// </para>
        /// </remarks>
        public Type Type
        {
            get { return m_type; }
            set { m_type = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        public void AddProperty(PropertyEntry entry)
        {
            properties[entry.Key] = entry.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        public PropertiesDictionary Properties
        {
            get { return properties; }
        }
    }
}
