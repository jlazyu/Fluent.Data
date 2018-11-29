using System;
using Fluent.Data.Transaction;

namespace Fluent.Data.Interfaces
{
    public interface ICompleteTransaction
    {
        void CommitTransaction(ITransactionContext transactionContext, Action<string> logger = null);

        void RollbackTransaction(ITransactionContext transactionContext, Action<string> logger = null);
    }
}