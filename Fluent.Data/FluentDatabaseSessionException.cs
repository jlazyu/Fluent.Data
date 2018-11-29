using System;

namespace Fluent.Data
{
    public class FluentDatabaseSessionException : Exception
    {
        public string CommandAsString { get; }

        public FluentDatabaseSessionException(string message, string commandAsString = null) : base(message)
        {
            CommandAsString = commandAsString;
        }

        public FluentDatabaseSessionException(string message, Exception baseException, string commandAsString = null) :
            base(message, baseException)
        {
            CommandAsString = commandAsString;
        }
    }
}