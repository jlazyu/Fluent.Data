using System.Data;

namespace Fluent.Data.Transaction
{
    internal class TransactionContext : ITransactionContext
    {
        private bool _isCommitted;

        public TransactionContext(IDbTransaction transaction)
        {
            Transaction = transaction;
        }

        public IDbTransaction Transaction{ get; }


        public void Complete()
        {
            if (_isCommitted)
            {
                throw new FluentDatabaseSessionException("DbTransaction already committed.");
            }

            Transaction?.Commit();
            _isCommitted = true;
            Transaction?.Connection?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }

            if (_isCommitted)
            {
                return;
            }

            Transaction?.Rollback();
            Transaction?.Connection?.Dispose();
        }
    }
}