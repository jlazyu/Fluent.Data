namespace Fluent.Data.Interfaces
{
    public interface IConnectToDatabase
    {
        IManageTransactionOrCreateDbCommand Connect(string connectionString);
    }
}