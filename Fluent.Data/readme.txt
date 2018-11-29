
-- Connection and Command are disposed after each time the database command is executed unless a database transaction is in flight
		and attached to the database command.

Usage:

DatabaseSession -- static reference to base database session factory
	
	.CreateSession(FluentConnectionInformation) 
		-- static method - creates a new provider specific session.
		-- entry point into fluent ado api

	.CreateTransaction([Action<string> logger])
		-- may be called after CreateSession(FluentConnectionInformation)
		-- connects to the database specified in the connection information given in the CreateSession(FluentConnectionInformation) method
		-- begins a transaction using the connection created in the above step and returns a transaction context to the caller
		-- the result of this call can be wrapped in a using statement which will gaurantee that the transaction will be rolled back
				unless the caller calls .Complete on the context

	.CreateDbCommand(string sql, ITransactionContext transactionContext)
		-- may be called after .CreateSession(FluentConnectionInformation) or .CreateTransaction([Action<string> logger])
		-- connects to the database specified in the connection information given in the CreateSession(FluentConnectionInformation) method 
				if a connection doesn't already exist on the session (connection would exist if a transaction was created)
		-- creates an database command and sets the command text to the given string
		-- if a transaction context is given then the created command is attatched to the transaction

	.CreateDbCommand(string sql)
		-- may be called after .CreateSession(FluentConnectionInformation) or .CreateTransaction([Action<string> logger])
		-- creation of a database command with out a transaction

	.SetCommandTimeout(int commandTimeout)
		-- may be called after .CreateDbCommand(string ITransactionContext)
		-- by default the provider specific default command timeout is used when calling .CreateDbCommand(string, ITransactionContext)
		-- result of calling this method overrides the default provider specific default command timeout

	.AddDbParameter(string parameterName, object parameterValue, bool useParameterPrefix)
		-- may be called after .CreateDbCommand(string, ITransactionContext), .CreateDbCommand(string), or .SetCommandTimeout(int)
		-- appends the provider specific parameter prefix if true otherwise false (default is true)
		-- adds a provider specific implementation of DbParameter to the database command with the
			- ParameterName = parameterName
			- Value = parameterValue

	.AddDbParameter(string parameterName, object parameterValue, DbType parameterType,
            ParameterDirection parameterDirection, int parameterSize, bool useParameterPrefix = true)
		-- may be called after .CreateDbCommand(string, ITransactionContext), .CreateDbCommand(string), or .SetCommandTimeout(int)
		-- appends the provider specific parameter prefix if true otherwise false (default is true)
		-- adds a provider specific implementation of DbParameter to the database command with the
			- ParameterName = parameterName
			- Value = parameterValue
			- DbType = provider specific rendered DbType based on the parameterType variable
			- Direction = parameterDirection
			- Size = parameterSize

	.ExecuteScalar<T>([Action<string> logger])
		-- may be called after .CreateDbCommand(string, ITransactionContext), .CreateDbCommand(string), .SetCommandTimeout(int),
				.AddDbParameter(string parameterName, object parameterValue, bool useParameterPrefix), or
				.AddDbParameter(string parameterName, object parameterValue, bool useParameterPrefix)
		-- executes database command by calling IDbCommand.ExecuteScalar()