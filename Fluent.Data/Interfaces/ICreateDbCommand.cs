using Fluent.Data.Transaction;

namespace Fluent.Data.Interfaces
{
    public interface ICreateDbCommand
    {
        IExecuteDbCommand CreateDbCommand(string sql);

        IExecuteDbCommand CreateDbCommand(string sql, ITransactionContext transactionContext);
    }
}