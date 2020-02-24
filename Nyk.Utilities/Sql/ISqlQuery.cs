using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISqlQuery
{
	Task<IEnumerable<TResult>> ReadAsync<TModel, TResult>(string query, TModel parameterModel, TResult resultSchema)
		where TModel : class
		where TResult : class;
	Task WriteAsync<TModel>(string query, TModel parameterModel)
		where TModel : class;
}