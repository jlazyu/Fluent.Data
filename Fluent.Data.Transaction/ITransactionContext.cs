using System;
using System.Data;

namespace Fluent.Data.Transaction
{
    public interface ITransactionContext : IDisposable
    {
        IDbTransaction Transaction { get; }

        void Complete();
    }
}