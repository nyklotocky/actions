using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Nyk.Utilities
{
	public class SqlQuery : ISqlQuery
	{
		private readonly string _connectionString;

		public SqlQuery(string connectionString)
		{
			this._connectionString = connectionString;
		}

		public async Task<IEnumerable<TResult>> ReadAsync<TModel, TResult>(string query, TModel parameterModel, TResult resultSchema)
			where TModel : class
			where TResult : class
		{
			return await this.ExecuteParameterizedQuery(
				query: query,
				parameterModel: parameterModel,
				executionStep: async command =>
				{
					using (var dataReader = await command.ExecuteReaderAsync())
					{
						return this.ReadResults<TResult>(dataReader);
					}
				});
		}

		public async Task WriteAsync<TModel>(string query, TModel parameterModel) where TModel : class
		{
			await this.ExecuteParameterizedQuery(
				query: query,
				parameterModel: parameterModel,
				executionStep: async command =>
				{
					await command.ExecuteNonQueryAsync();
					return new object();
				});
		}

		private async Task<TReturn> ExecuteParameterizedQuery<TModel, TReturn>(string query, TModel parameterModel, Func<SqlCommand, Task<TReturn>> executionStep)
			where TModel : class
		{
			using (var connection = new SqlConnection(this._connectionString))
			{
				using (var command = new SqlCommand(query, connection))
				{
					this.ParameterizeCommand(command, parameterModel);
					connection.Open();
					return await executionStep(command);
				}
			}
		}

		private void ParameterizeCommand<TModel>(SqlCommand sqlCommand, TModel model)
			where TModel : class
		{
			typeof(TModel)
				.GetProperties()
				.Select(pi => new
				{
					ParameterName = $"@{pi.Name}",
					ParameterValue = pi.GetValue(model) ?? System.DBNull.Value
				})
				.ToList()
				.ForEach(p => sqlCommand.Parameters.AddWithValue(p.ParameterName, p.ParameterValue));
		}

		private IEnumerable<TResult> ReadResults<TResult>(SqlDataReader dataReader)
			where TResult : class
		{
			var columnSchema = dataReader
				.GetColumnSchema()
				.ToList();
			
			var properties = typeof(TResult)
				.GetProperties()
				.ToList();

			if (columnSchema.Count != properties.Count)
			{
				throw new ArgumentException("The result set and the result type expect a different number of columns");
			}

			var propertiesAndIndicies = properties
				.Join(columnSchema,
					pi => pi.Name.ToUpper(),
					c => c.ColumnName.ToUpper(),
					(pi, c) => new
						{
							PropertyInfo = pi,
							ColumnIndex = c.ColumnOrdinal.Value
						})
				.ToList();

			if (properties.Count != propertiesAndIndicies.Count)
			{
				throw new ArgumentException("The result type's properties do not match the returned columns");
			}

			var resultSet = new List<TResult>();

			var isAnonymousType = typeof(TResult)
				.GetConstructors()
				.FirstOrDefault()
				.GetParameters()
				.Count() == propertiesAndIndicies.Count;
			
			Func<object, object> handleDbNull = val => val == DBNull.Value ? null : val;

			var columnData = new object[dataReader.FieldCount];
			while (dataReader.Read())
			{
				TResult result;
				dataReader.GetValues(columnData);
				
				if (isAnonymousType)
				{
					//transpose to ensure correct ordering
					var objectParameters = propertiesAndIndicies
						.Select(i => handleDbNull(columnData[i.ColumnIndex]))
						.ToArray();
					
					result = Activator.CreateInstance(typeof(TResult), objectParameters) as TResult;
				}
				else // default constructor
				{
					result = Activator.CreateInstance(typeof(TResult)) as TResult;
					propertiesAndIndicies
						.ForEach(pi => pi.PropertyInfo
							.SetValue(result, handleDbNull(columnData[pi.ColumnIndex])));
				}
				resultSet.Add(result);
			}

			return resultSet;
		}
	}
}