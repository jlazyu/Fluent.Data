namespace Fluent.Data.Interfaces
{
    public interface IAmADatabaseSession : IManageTransactionOrCreateDbCommand, IExecuteDbCommand
    {
    }
}
