using System;

namespace log4net.Appender
{
    /// <summary>
    /// Interface that can be implemented by Appenders that buffer logging data and expose a <see cref="Flush"/> method.
    /// </summary>
    public interface IFlushable
    {
        /// <summary>
        /// Flushes any buffered log data.
        /// </summary>
        /// <remarks>
        /// Appenders that implement the <see cref="Flush"/> method must do so in a thread-safe manner: it can be called concurrently with
        /// the <see cref="log4net.Appender.IAppender.DoAppend"/> method.
        /// <para>
        /// Typically this is done by locking on the Appender instance, e.g.:
        /// <code>
        /// <![CDATA[
        /// public bool Flush(int millisecondsTimeout)
        /// {
        ///     lock(this)
        ///     {
        ///         // Flush buffered logging data
        ///         ...
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </para>
        /// <para>
        /// The <paramref name="millisecondsTimeout"/> parameter is only relevant for appenders that process logging events asynchronously,
        /// such as <see cref="RemotingAppender"/>.
        /// </para>
        /// </remarks>
        /// <param name="millisecondsTimeout">The maximum time to wait for logging events to be flushed.</param>
        /// <returns><c>True</c> if all logging events were flushed successfully, else <c>false</c>.</returns>
        bool Flush(int millisecondsTimeout);
    }
}
