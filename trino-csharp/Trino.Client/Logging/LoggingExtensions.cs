using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;

namespace Trino.Client.Logging
{
    // Wraps logger extensions to avoid direct dependency on Microsoft.Extensions.Logging
    public static class LoggerExtensions
    {
        private static readonly Func<object, Exception, string> _messageFormatter = new Func<object, Exception, string>(LoggerExtensions.MessageFormatter);

        /// <summary>Formats and writes a debug log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogDebug(
          this ILoggerWrapper logger,
          EventId eventId,
          Exception exception,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Debug, eventId, (object)new FormattedLogValues(message, args), exception, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a debug log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogDebug(
          this ILoggerWrapper logger,
          EventId eventId,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Debug, eventId, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a debug log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogDebug(this ILoggerWrapper logger, string message, params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Debug, (EventId)0, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a trace log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogTrace(
          this ILoggerWrapper logger,
          EventId eventId,
          Exception exception,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Trace, eventId, (object)new FormattedLogValues(message, args), exception, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a trace log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogTrace(
          this ILoggerWrapper logger,
          EventId eventId,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Trace, eventId, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a trace log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogTrace(this ILoggerWrapper logger, string message, params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Trace, (EventId)0, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes an informational log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogInformation(
          this ILoggerWrapper logger,
          EventId eventId,
          Exception exception,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Information, eventId, (object)new FormattedLogValues(message, args), exception, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes an informational log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogInformation(
          this ILoggerWrapper logger,
          EventId eventId,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Information, eventId, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes an informational log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogInformation(this ILoggerWrapper logger, string message, params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Information, (EventId)0, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a warning log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogWarning(
          this ILoggerWrapper logger,
          EventId eventId,
          Exception exception,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Warning, eventId, (object)new FormattedLogValues(message, args), exception, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a warning log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogWarning(
          this ILoggerWrapper logger,
          EventId eventId,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Warning, eventId, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a warning log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogWarning(this ILoggerWrapper logger, string message, params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Warning, (EventId)0, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes an error log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogError(
          this ILoggerWrapper logger,
          EventId eventId,
          Exception exception,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Error, eventId, (object)new FormattedLogValues(message, args), exception, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes an error log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogError(
          this ILoggerWrapper logger,
          EventId eventId,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Error, eventId, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes an error log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogError(this ILoggerWrapper logger, string message, params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Error, (EventId)0, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a critical log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogCritical(
          this ILoggerWrapper logger,
          EventId eventId,
          Exception exception,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Critical, eventId, (object)new FormattedLogValues(message, args), exception, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a critical log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogCritical(
          this ILoggerWrapper logger,
          EventId eventId,
          string message,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Critical, eventId, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats and writes a critical log message.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to write to.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void LogCritical(this ILoggerWrapper logger, string message, params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            logger.Log<object>(LogLevel.Critical, (EventId)0, (object)new FormattedLogValues(message, args), (Exception)null, LoggerExtensions._messageFormatter);
        }

        /// <summary>Formats the message and creates a scope.</summary>
        /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILoggerWrapper" /> to create the scope in.</param>
        /// <param name="messageFormat">Format string of the scope message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A disposable scope object. Can be null.</returns>
        public static IDisposable BeginScope(
          this ILoggerWrapper logger,
          string messageFormat,
          params object[] args)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            return logger.BeginScope<FormattedLogValues>(new FormattedLogValues(messageFormat, args));
        }

        private static string MessageFormatter(object state, Exception error) => state.ToString();
    }
}
