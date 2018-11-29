using System;
using Fluent.Data.Transaction;

namespace Fluent.Data.Interfaces
{
    public interface IBeginTransaction
    {
        ITransactionContext CreateTransaction(Action<string> logger = null);
    }
}
