using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Actions.Client.Data;

namespace Actions.Web.BusinessLogic
{
	public class ActionsTimingRetriever
	{
		private readonly ISqlQuery _sqlQuery;

		public ActionsTimingRetriever(ISqlQuery sqlQuery)
		{
			if (sqlQuery is null)
			{
				throw new ArgumentNullException(nameof(sqlQuery));
			}

			this._sqlQuery = sqlQuery;
		}

		public async Task<IEnumerable<ActionStatistics>> RetrieveAsync()
		{
			await this._sqlQuery.WriteAsync("SELECT 1", new {});
			throw new NotImplementedException();
		}
	}
}