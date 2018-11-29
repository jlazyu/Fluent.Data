namespace Fluent.Data.Interfaces
{
    public interface IManageTransactionOrCreateDbCommand : IBeginTransaction, ICreateDbCommand
    {
        
    }
}
