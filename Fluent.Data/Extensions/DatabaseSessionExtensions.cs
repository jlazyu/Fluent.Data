using System;
using Fluent.Data.Interfaces;

namespace Fluent.Data.Extensions
{
    public static class DatabaseSessionExtensions
    {
        public static IExecuteDbCommand AddDbParameter(this IExecuteDbCommand databaseSession, Func<bool> addIfTrue, string parameterName, object parameterValue)
        {
            return addIfTrue() ? databaseSession.AddDbParameter(parameterName, parameterValue) : databaseSession;
        }
    }
}
