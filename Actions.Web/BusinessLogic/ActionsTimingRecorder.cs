using System;
using System.Threading.Tasks;
using Actions.Client.Data;
using Nyk.Utilities.TableInfrastructure;

namespace Actions.Web.BusinessLogic
{
	public class ActionsTimingRecorder
	{
		private readonly ISqlQuery _sqlQuery;
		private readonly ITableProvider _tableProvider;

		public ActionsTimingRecorder(
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

		public async Task RecordAsync(ActionTiming actionTiming)
		{
			var actionTimes = this._tableProvider.Table("actionTimes");
			var actions = this._tableProvider.Table("actions");

			//this utility method prevents sql injection by parameterizing inputs into our template
			await this._sqlQuery.WriteAsync($@"
			SET XACT_ABORT ON
			BEGIN TRAN
				DECLARE @actionId INT

				SELECT
					@actionId = actionId
				FROM {actions} a WITH (UPDLOCK, HOLDLOCK)
				WHERE actionName = @actionName

				IF @actionId IS NULL
				BEGIN
					INSERT INTO {actions} (actionName)
					SELECT @actionName

					SET @actionId = SCOPE_IDENTITY()
				END
			-- exit our transaction to a) release locks on actions ASAP and to b) avoid taking an overly restrictive lock on actionTimes (which could also put us at risk of deadlock)
			COMMIT TRAN

			INSERT INTO {actionTimes} (actionId, actionTime)
			SELECT @actionId, @actionTime",
			new
			{
				actionName = actionTiming.Action,
				actionTime = actionTiming.Time,
			});
		}
	}
}