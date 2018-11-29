using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Fluent.Data.Interfaces
{
    public interface IExecuteDbCommand
    {
        IExecuteDbCommand SetCommandTimeout(int commandTimeout);

        IExecuteDbCommand AddDbParameter(string parameterName, object parameterValue, bool useParameterPrefix = true);

        IExecuteDbCommand AddDbParameter(string parameterName, object parameterValue, DbType parameterType,
            ParameterDirection parameterDirection, int parameterSize, bool useParameterPrefix = true);

        Task<T> ExecuteScalar<T>(Action<string> logger);

        Task<IEnumerable<TEntity>> ExecuteDataReader<TEntity>(Func<IDataRecord, TEntity> getEntityFromDataRecord,
            Action<string> logger = null);

        IEnumerable<TEntity> ExecuteDataReaderStream<TEntity>(
            Func<IDataRecord, TEntity> getEntityFromDataRecord, Action<string> logger = null);

        Task<IEnumerable<DbParameter>> ExecuteStoredProcedure(Action<string> logger = null);

        Task<int> ExecuteStoredProcedureRowCount(Action<string> logger = null);

        Task<int> ExecuteUpdate(Action<string> logger = null);

        Task<int> ExecuteInsert(Action<string> logger = null);

        Task<int> ExecuteDelete(Action<string> logger = null);

        Task<DataSet> GetDataSet(Action<string> logger = null);
    }
}