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
			return await this._sqlQuery.ReadAsync(@"
				SELECT
					MAX(a.actionName) AS [name]
					, AVG(act.actionTime) AS [avg]
				FROM dbo.actionTimes act
				JOIN dbo.actions a
					ON a.actionId = act.actionId
				GROUP BY act.actionId",
				new
				{
					//todo I need to set up some ITableProvider equivalent for table mocking
				},
				resultSchema: default(ActionStatistics));
		}
	}
}