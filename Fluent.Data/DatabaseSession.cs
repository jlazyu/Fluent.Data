﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Fluent.Data.Configuration;
using Fluent.Data.Extensions;
using Fluent.Data.Interfaces;
using Fluent.Data.Transaction;

namespace Fluent.Data
{
    /// <inheritdoc cref="IAmADatabaseSession" />
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// Fluent builder pattern implementation of ADO.Net database communication.  Uses the ADO.Net Db Provider
    /// abstraction to allow for easily switching database providers.
    /// </summary>
    public abstract class DatabaseSession : IDisposable, IAmADatabaseSession
    {
        private static IEnumerable<Type> SessionTypes { get; }

        protected DbProviderFactory DbProviderFactory { get; private set; }

        protected string ConnectionString { get; private set; }

        protected IDbConnection Connection { get; set; }

        protected IDbCommand Command { get; set; }

        protected FluentConnectionInformation ConnectionInformation { get; set; }

        /// <summary>
        /// static constructor will retrieve all types marked with <see cref="FluentDataProviderAttribute"/> which contains
        /// the provider invariant name which will map to a specific database session type.
        /// </summary>
        static DatabaseSession()
        {
            SessionTypes = typeof(DatabaseSession)
                .GetTypesWithCustomAttribute<FluentDataProviderAttribute>();
        }

        /// <summary>
        /// Create a fluent database session which will be tied to the provider specific implementation in the 
        /// given <paramref name="connectionInformation"/>.  This is the entry point into the fluent ADO.Net api.
        /// At this point, no connection, command, or transaction is created.
        /// </summary>
        /// <param name="connectionInformation"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException"></exception>
        public static IManageTransactionOrCreateDbCommand CreateSession(FluentConnectionInformation connectionInformation)
        {
            var sessionType =
                SessionTypes.GetTypeWithCustomAttributeValue<FluentDataProviderAttribute, string>(
                    x => x.DataProviderName, connectionInformation.ProviderName) ??
                throw new FluentDatabaseSessionException("A valid connection string must be supplied");

            //create the fluent database session
            var currentSession = (DatabaseSession)Activator.CreateInstance(sessionType);

            currentSession.ConnectionInformation = connectionInformation;
            //set the db provider factory which is retrieved from the factory
            currentSession.DbProviderFactory = DbProviderFactories.GetFactory(connectionInformation.ProviderName);

            //give the provider specific implementation a chance to manipulate the connection
            //string such as handling an encrypted password.
            currentSession.ConnectionString = currentSession.GetConnectionString(connectionInformation);

            return currentSession;
        }

        /// <summary>
        /// Begins a <see cref="DbTransaction"/> for this database session.  If a <paramref name="logger"/>
        /// is given, the opening of the <see cref="DbTransaction"/> will be logged.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException">This exception will be thrown for the following reasons:
        /// 1. The connection associated with this database session is null
        /// 2. If after a call to open the connection, on a connection that is not currently open, the connection is still not open</exception>
        public ITransactionContext CreateTransaction(Action<string> logger = null)
        {
            try
            {
                Connect(null);

                if (Connection == null)
                {
                    throw new FluentDatabaseSessionException("Connection is not valid");
                }

                if (Connection.State != ConnectionState.Open)
                {
                    Connection?.Open();
                }

                if (Connection.State != ConnectionState.Open)
                {
                    throw new FluentDatabaseSessionException("Unable to open connection successfully");
                }
                
                var transaction = Connection.BeginTransaction();

                var message = $"Transaction: {transaction.GetHashCode()} - Begin";

                var transactionContext = new TransactionContext(transaction);

                logger?.Invoke(message);

                return transactionContext;
            }
            catch (FluentDatabaseSessionException)
            {
                Dispose();
                throw;
            }
            catch (Exception ex)
            {
                Dispose();
                throw new FluentDatabaseSessionException("Error while beginning transaction.", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public IExecuteDbCommand CreateDbCommand(string sql)
        {
            return CreateDbCommand(sql, null);
        }

        /// <summary>
        /// Creates a <see cref="DbCommand"/> using the <see cref="DbConnection"/> created by calling <see cref="CreateSession"/> and
        /// sets the <see cref="DbCommand.CommandText"/> to be <paramref name="sql"/>.  
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="transactionContext"></param>
        /// <returns></returns>
        public IExecuteDbCommand CreateDbCommand(string sql, ITransactionContext transactionContext)
        {
            try
            {
                Connect(transactionContext);
                if (Connection == null)
                {
                    return null;
                }

                if (Command != null)
                {
                    Command.Dispose();
                    Command = null;
                }

                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
                }

                Command = Connection.CreateCommand();
                Command.CommandText = sql;
                DecorateCommand(Command);
                
                if (transactionContext?.Transaction != null)
                {
                    Command.Transaction = transactionContext.Transaction;
                }

                return this;
            }
            catch (Exception ex)
            {
                Dispose();
                throw new FluentDatabaseSessionException("Error while creating command", ex, Command?.ToCommandString());
            }
        }

        /// <summary>
        /// The <paramref name="commandTimeout"/> can be given to override the provider default 
        /// command execution time out.
        /// </summary>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public IExecuteDbCommand SetCommandTimeout(int commandTimeout)
        {
            Command.CommandTimeout = commandTimeout != -1 ? commandTimeout : Command.CommandTimeout;

            return this;
        }

        /// <summary>
        /// Adds a <see cref="DbParameter"/> to the current <see cref="DbCommand"/> using the given values as follows:
        /// 1. <paramref name="parameterName"/> as <see cref="DbParameter.ParameterName"/> 
        /// 2. <paramref name="parameterValue"/> as <see cref="DbParameter.Value"/>
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="parameterValue"></param>
        /// <param name="useParameterPrefix">Signals whether to include the provider parameter prefix when adding a parameter.  Useful when calling a stored procedure.</param>
        /// <returns></returns>        
        /// <exception cref="FluentDatabaseSessionException">Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created or if the <see cref="DbProviderFactory"/> is unable to create the <see cref="DbParameter"/></exception>
        public IExecuteDbCommand AddDbParameter(string parameterName, object parameterValue, bool useParameterPrefix = true)
        {
            try
            {
                if (Command == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A database command must be created before AddDbParameter can be called.");
                }

                var parameter = DbProviderFactory.CreateParameter();

                if (parameter == null)
                {
                    throw new FluentDatabaseSessionException(
                        $"Unable to create a database parameter named {parameterName} with the value {parameterValue} using the db provider factory {DbProviderFactory.GetType()}", Command?.ToCommandString());
                }

                parameter.ParameterName = parameterName.StartsWith(ParameterPrefix)
                    ? parameterName
                    : (useParameterPrefix ? $"{ParameterPrefix}{parameterName}" : $"{parameterName}");

                parameter.Value = parameterValue ?? DBNull.Value;

                Command.Parameters.Add(parameter);
            }
            catch (FluentDatabaseSessionException)
            {
                Dispose();
                throw;
            }
            catch (Exception ex)
            {
                var commandAsString = Command?.ToCommandString();
                Dispose();
                throw new FluentDatabaseSessionException(
                    $"Error while creating parameter.  ParamterName: {parameterName} - ParameterValue: {parameterValue}.",
                    ex, commandAsString);
            }

            return this;
        }

        /// <summary>
        /// Adds a <see cref="DbParameter"/> to the current <see cref="DbCommand"/> using the given values as follows:
        /// 1. <paramref name="parameterName"/> as <see cref="DbParameter.ParameterName"/> 
        /// 2. <paramref name="parameterValue"/> as <see cref="DbParameter.Value"/>
        /// 3. <paramref name="parameterType"/> as <see cref="DbParameter.DbType"/>
        /// 4. <paramref name="parameterDirection"/> as <see cref="DbParameter.Direction"/>
        /// 5. <paramref name="parameterSize"/> as <see cref="DbParameter.Size"/>
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="parameterValue"></param>
        /// <param name="parameterType"></param>
        /// <param name="parameterDirection"></param>
        /// <param name="parameterSize"></param>
        /// <param name="useParameterPrefix">Signals whether to include the provider parameter prefix when adding a parameter.  Useful when calling a stored procedure.</param>
        /// <returns></returns>        
        /// <exception cref="FluentDatabaseSessionException">Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created or if the <see cref="DbProviderFactory"/> is unable to create the <see cref="DbParameter"/></exception>
        public IExecuteDbCommand AddDbParameter(string parameterName, object parameterValue, DbType parameterType,
            ParameterDirection parameterDirection, int parameterSize, bool useParameterPrefix = true)
        {
            if (Command == null)
            {
                throw new FluentDatabaseSessionException(
                    "A database command must be created before AddDbParameter can be called.");
            }

            var parameter = DbProviderFactory.CreateParameter();

            if (parameter == null)
            {
                throw new FluentDatabaseSessionException(
                    $"Unable to create a database parameter named {parameterName} with the value {parameterValue} using the db provider factory {DbProviderFactory.GetType()}", Command?.ToCommandString());
            }

            try
            {
                parameter.ParameterName = parameterName.StartsWith(ParameterPrefix)
                    ? parameterName
                    : (useParameterPrefix ? $"{ParameterPrefix}{parameterName}" : $"{parameterName}");
                SetProviderSpecificParameterType(parameter, parameterType);
                parameter.Value = parameterValue ?? DBNull.Value;
                parameter.Direction = parameterDirection;
                parameter.Size = parameterSize;

                Command.Parameters.Add(parameter);

                return this;
            }
            catch (FluentDatabaseSessionException)
            {
                Dispose();
                throw;
            }
            catch (Exception ex)
            {
                var commandAsString = Command?.ToCommandString();
                Dispose();
                throw new FluentDatabaseSessionException(
                    $"Error while creating parameter.  ParamterName: {parameterName} - ParameterValue: {parameterValue}.",
                    ex, commandAsString);
            }
        }

        /// <summary>
        /// Calls <see cref="DbCommand.ExecuteScalar"/> on the <see cref="DbCommand"/> created by this 
        /// database session and returns the result of the call.
        /// If a <paramref name="logger"/> is given, the committing of the <see cref="DbTransaction"/> will be logged.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException">>Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created</exception>
        public async Task<T> ExecuteScalar<T>(Action<string> logger = null)
        {
            try
            {
                if (Command == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A database command must be created before ExecuteScalar can be called.");
                }

                var message = Command.Transaction != null
                    ? $"Transaction: {Command.Transaction.GetHashCode()} - {Command.ToCommandString()}"
                    : $" - {Command.ToCommandString()}";

                var result = await Task.Run(() => (T) Command.ExecuteScalar());

                logger?.Invoke(message);

                return result;
            }
            catch (FluentDatabaseSessionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FluentDatabaseSessionException("Error while executing scalar.", ex, Command?.ToCommandString());
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Calls <see cref="DbCommand.ExecuteReader()"/> on the <see cref="DbCommand"/> created by this 
        /// database session and returns the result of the call as an <see cref="IEnumerable{TEntity}"/>.  The 
        /// <see cref="IDataReader"/> will leave the connection open after being read.
        /// If a <paramref name="logger"/> is given, the committing of the <see cref="DbTransaction"/> will be logged.
        /// </summary>
        /// <typeparam name="TEntity">Type of object that the <see cref="IDataReader"/> will be converted to</typeparam>
        /// <param name="getEntityFromDataRecord"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException">Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created or if the <paramref name="getEntityFromDataRecord"/> is null.</exception>
        public async Task<IEnumerable<TEntity>> ExecuteDataReader<TEntity>(Func<IDataRecord, TEntity> getEntityFromDataRecord, Action<string> logger = null)
        {
            try
            {
                if (Command == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A DbCommand object must be created before using it to retrieve an IDataReader object.");
                }

                if (getEntityFromDataRecord == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A valid non-null Func<IDataRecord, TEntity> must be supplied in order to retrieve data from the IDataReader.", Command?.ToCommandString());
                }

                var message = Command.Transaction != null
                    ? $"Transaction: {Command.Transaction.GetHashCode()} - {Command.ToCommandString()}"
                    : $" - {Command.ToCommandString()}";

                var results = await Task.Run(() => Command
                    .ExecuteReader()
                    .AsEnumerable()
                    .Select(getEntityFromDataRecord)
                    .ToList());

                logger?.Invoke(message);

                return results;
            }
            catch (FluentDatabaseSessionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FluentDatabaseSessionException("Error while executing data reader.", ex, Command?.ToCommandString());
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Calls <see cref="DbCommand.ExecuteReader()"/> on the <see cref="DbCommand"/> created by this 
        /// database session and returns the result of the call as an <see cref="IEnumerable{TEntity}"/>.  The 
        /// <see cref="IDataReader"/> will close the connection open after being read.
        /// If a <paramref name="logger"/> is given, the committing of the <see cref="DbTransaction"/> will be logged.
        /// </summary>
        /// <typeparam name="TEntity">Type of object that the <see cref="IDataReader"/> will be converted to</typeparam>
        /// <param name="getEntityFromDataRecord"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException">Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created or if the <paramref name="getEntityFromDataRecord"/> is null.</exception>
        public IEnumerable<TEntity> ExecuteDataReaderStream<TEntity>(
            Func<IDataRecord, TEntity> getEntityFromDataRecord, Action<string> logger = null)
        {
            IEnumerable<TEntity> results;

            try
            {
                if (Command == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A DbCommand object must be created before using it to retrieve an IDataReader object.");
                }

                if (getEntityFromDataRecord == null)
                {

                    throw new FluentDatabaseSessionException(
                        "A valid non-null Func<IDataRecord, TEntity> must be supplied in order to retrieve data from the IDataReader.", Command?.ToCommandString());
                }

                var message = Command.Transaction != null
                    ? $"Transaction: {Command.Transaction.GetHashCode()} - {Command.ToCommandString()}"
                    : $" - {Command.ToCommandString()}";

                results = Command
                    .ExecuteReader(CommandBehavior.CloseConnection)
                    .AsEnumerable()
                    .Select(getEntityFromDataRecord);

                logger?.Invoke(message);
            }
            catch (FluentDatabaseSessionException)
            {
                Dispose();
                throw;
            }
            catch (Exception ex)
            {
                var commandAsString = Command?.ToCommandString();
                Dispose();
                throw new FluentDatabaseSessionException("Error while executing data reader close connection.", ex, commandAsString);
            }

            foreach (var result in results)
            {
                yield return result;
            }

            Dispose();
        }

        /// <summary>
        /// Calls <see cref="DbCommand.ExecuteNonQuery"/> on the <see cref="DbCommand"/> created by this 
        /// database session with a <see cref="DbCommand.CommandType"/> of <see cref="CommandType.StoredProcedure"/>  
        /// and returns the result of the call as an <see cref="IEnumerable{TEntity}"/>. The connection is closed after calling this method.
        /// If a <paramref name="logger"/> is given, the committing of the <see cref="DbTransaction"/> will be logged.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException">Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created</exception>
        public async Task<IEnumerable<DbParameter>> ExecuteStoredProcedure(Action<string> logger = null)
        {
            try
            {
                if (Command == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A DbCommand object must be created before using it to retrieve an IDataReader object.");
                }

                Command.CommandType = CommandType.StoredProcedure;

                var message = Command.Transaction != null
                    ? $"Transaction: {Command.Transaction.GetHashCode()} - {Command.ToCommandString()}"
                    : $" - {Command.ToCommandString()}";

                await Task.Run(() => Command.ExecuteNonQuery());

                var result = Command
                    .Parameters
                    .OfType<DbParameter>()
                    .Where(x => x.Direction == ParameterDirection.Output || x.Direction == ParameterDirection.Output)
                    .ToList();

                logger?.Invoke(message);

                return result;
            }
            catch (FluentDatabaseSessionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FluentDatabaseSessionException("Error while executing stored procedure.", ex,
                    Command?.ToCommandString());
            }
            finally
            {
                Dispose();
            }
        }
        
        /// <summary>
        /// Calls <see cref="DbCommand.ExecuteNonQuery"/> on the <see cref="DbCommand"/> created by this 
        /// database session with a <see cref="DbCommand.CommandType"/> of <see cref="CommandType.StoredProcedure"/>  
        /// and returns the number of rows affected. The connection is closed after calling this method.
        /// If a <paramref name="logger"/> is given, the committing of the <see cref="DbTransaction"/> will be logged.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException">Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created</exception>
        public async Task<int> ExecuteStoredProcedureRowCount(Action<string> logger = null)
        {
            try
            {
                if (Command == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A DbCommand object must be created before using it to retrieve an IDataReader object.");
                }

                Command.CommandType = CommandType.StoredProcedure;

                var message = Command.Transaction != null
                    ? $"Transaction: {Command.Transaction.GetHashCode()} - {Command.ToCommandString()}"
                    : $" - {Command.ToCommandString()}";

                var rowCount = await Task.Run(() => Command.ExecuteNonQuery());

                logger?.Invoke(message);

                return rowCount;
            }
            catch (FluentDatabaseSessionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FluentDatabaseSessionException("Error while executing stored procedure row count.", ex,
                    Command?.ToCommandString());
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Calls <see cref="DbCommand.ExecuteNonQuery"/> on the <see cref="DbCommand"/> created by this 
        /// database session and returns the result of the call.  The connection is closed after calling this method.
        /// If a <paramref name="logger"/> is given, the committing of the <see cref="DbTransaction"/> will be logged.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException">>Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created</exception>
        public async Task<int> ExecuteUpdate(Action<string> logger = null)
        {
            try
            {
                if (Command == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A DbCommand object must be created before using it to retrieve an IDataReader object.");
                }

                var message = Command.Transaction != null
                    ? $"Transaction: {Command.Transaction.GetHashCode()} - {Command.ToCommandString()}"
                    : $" - {Command.ToCommandString()}";

                var result = await Task.Run(() => Command.ExecuteNonQuery());

                logger?.Invoke(message);

                return result;
            }
            catch (FluentDatabaseSessionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FluentDatabaseSessionException("Error while executing update.", ex,
                    Command?.ToCommandString());
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Calls <see cref="DbCommand.ExecuteNonQuery"/> on the <see cref="DbCommand"/> created by this 
        /// database session and returns the result of the call.  The connection is closed after calling this method.
        /// If a <paramref name="logger"/> is given, the committing of the <see cref="DbTransaction"/> will be logged.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException">>Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created</exception>
        public async Task<int> ExecuteInsert(Action<string> logger = null)
        {
            try
            {
                if (Command == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A DbCommand object must be created before using it to retrieve an IDataReader object.");
                }

                var message = Command.Transaction != null
                    ? $"Transaction: {Command.Transaction.GetHashCode()} - {Command.ToCommandString()}"
                    : $" - {Command.ToCommandString()}";

                var result = await Task.Run(() => Command.ExecuteNonQuery());

                logger?.Invoke(message);

                return result;
            }
            catch (FluentDatabaseSessionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FluentDatabaseSessionException("Error while executing insert.", ex,
                    Command?.ToCommandString());
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Calls <see cref="DbCommand.ExecuteNonQuery"/> on the <see cref="DbCommand"/> created by this 
        /// database session and returns the result of the call.  The connection is closed after calling this method.
        /// If a <paramref name="logger"/> is given, the committing of the <see cref="DbTransaction"/> will be logged.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException">>Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created</exception>
        public async Task<int> ExecuteDelete(Action<string> logger = null)
        {
            try
            {
                if (Command == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A DbCommand object must be created before using it to retrieve an IDataReader object.");
                }

                var message = Command.Transaction != null
                    ? $"Transaction: {Command.Transaction.GetHashCode()} - {Command.ToCommandString()}"
                    : $" - {Command.ToCommandString()}";

                var result = await Task.Run(() => Command.ExecuteNonQuery());

                logger?.Invoke(message);

                return result;
            }
            catch (FluentDatabaseSessionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FluentDatabaseSessionException("Error while executing delete.", ex,
                    Command?.ToCommandString());
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Uses the <see cref="DbProviderFactory"/> to create a <see cref="DbDataAdapter"/> and then uses it 
        /// to fill and return a <see cref="DataSet"/>.  The connection is closed after calling this method.
        /// If a <paramref name="logger"/> is given, the committing of the <see cref="DbTransaction"/> will be logged.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="FluentDatabaseSessionException">>Will be thrown if a <see cref="DbCommand"/> (a call to CreateDbCommand) 
        /// has not been created</exception>
        public async Task<DataSet> GetDataSet(Action<string> logger = null)
        {
            try
            {
                if (Command == null)
                {
                    throw new FluentDatabaseSessionException(
                        "A DbCommand object must be created before using it to retrieve an IDataReader object.");
                }

                var adapter = DbProviderFactory.CreateDataAdapter();

                if (adapter == null)
                {
                    throw new FluentDatabaseSessionException(
                        $"Unable to create a database adapter using the db provider factory {DbProviderFactory.GetType()}");
                }

                adapter.SelectCommand = (DbCommand) Command;

                var message = Command.Transaction != null
                    ? $"Transaction: {Command.Transaction.GetHashCode()} - {Command.ToCommandString()}"
                    : $" - {Command.ToCommandString()}";

                var dataSet = new DataSet();
                await Task.Run(() => adapter.Fill(dataSet));

                logger?.Invoke(message);

                return dataSet;
            }
            catch (FluentDatabaseSessionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FluentDatabaseSessionException("Error while executing get data set.", ex,
                    Command?.ToCommandString());
            }
            finally
            {
                Dispose();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Diposes all three of the following if they exist:
        /// 1. <see cref="T:System.Data.Common.DbCommand" />
        /// 2. <see cref="T:System.Data.Common.DbTransaction" />
        /// 3. <see cref="T:System.Data.Common.DbConnection" />
        /// </summary>
        public void Dispose()
        {
            var disposeConnection = Command.Transaction == null;

            Command?.Dispose();

            if (disposeConnection)
            {
                Connection?.Dispose();
            }
        }

        protected abstract string GetConnectionString(FluentConnectionInformation connectionInformation);

        protected abstract string ParameterPrefix { get; }

        protected abstract void DecorateCommand(IDbCommand command);

        protected virtual void SetProviderSpecificParameterType(DbParameter parameter, DbType parameterType)
        {
            parameter.DbType = parameterType;
        }

        protected void Connect(ITransactionContext transactionContext)
        {
            try
            {
                if (transactionContext?.Transaction != null)
                {
                    Connection = transactionContext.Transaction.Connection;
                    return;
                }

                //create the database connection.
                Connection = DbProviderFactory.CreateConnection() ??
                             throw new FluentDatabaseSessionException(
                                 $"Unable to use the DbProviderFactory {DbProviderFactory.GetType()} to create a valid database connection.");

                //set the connection string on the connection.
                Connection.ConnectionString = ConnectionString;

                //if the connection is not open, which it shouldn't be, then open it
                if (Connection?.State != ConnectionState.Open)
                {
                    Connection?.Open();
                }
            }
            catch (FluentDatabaseSessionException)
            {
                Dispose();
                throw;
            }
            catch (Exception ex)
            {
                Dispose();
                throw new FluentDatabaseSessionException("Error while creating session.", ex, Command?.ToCommandString());
            }
        }
    }
}