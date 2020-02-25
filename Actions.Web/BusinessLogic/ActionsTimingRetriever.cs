using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Actions.Web.Data;
using Nyk.Utilities.TableInfrastructure;

namespace Actions.Web.BusinessLogic
{
	public class ActionsTimingRetriever
	{
		private readonly ISqlQuery _sqlQuery;
		private readonly ITableProvider _tableProvider;

		public ActionsTimingRetriever(
			ISqlQuery sqlQuery,
			ITableProvider tableProvider)
		{
			if (sqlQuery is null)
			{
				throw new ArgumentNullException(nameof(sqlQuery));
			}

			if (tableProvider is null)
			{
				throw new ArgumentNullException(nameof(tableProvider));
			}

			this._sqlQuery = sqlQuery;
			this._tableProvider = tableProvider;
		}

		public async Task<IEnumerable<ActionStatistics>> RetrieveAsync()
		{
			return await this._sqlQuery.ReadAsync($@"
				SELECT
					MAX(a.actionName) AS [name]
					, AVG(act.actionTime) AS [avg]
				FROM {this._tableProvider.Table("actionTimes")} act
				JOIN {this._tableProvider.Table("actions")} a
					ON a.actionId = act.actionId
				GROUP BY act.actionId",
				new
				{
					// as my utility infrastructure currently stands, it is not a true templating engine, so we interpolate table names above
				},
				resultSchema: default(ActionStatistics));
		}
	}
}