using System;
using System.Threading.Tasks;
using Actions.Client.Data;

namespace Actions.Web.BusinessLogic
{
	public class ActionsTimingRecorder
	{
		private readonly ISqlQuery _sqlQuery;

		public ActionsTimingRecorder(ISqlQuery sqlQuery)
		{
			if (sqlQuery is null)
			{
				throw new ArgumentNullException(nameof(sqlQuery));
			}

			this._sqlQuery = sqlQuery;
		}

		public async Task RecordAsync(ActionTiming actionTiming)
		{
			//this utility method prevents sql injection by parameterizing inputs into our template
			await this._sqlQuery.WriteAsync(@"
			SET XACT_ABORT ON
			BEGIN TRAN
				DECLARE @actionId INT

				SELECT
					@actionId = actionId
				FROM dbo.actions a WITH (UPDLOCK, HOLDLOCK)
				WHERE actionName = @actionName

				IF @actionId IS NULL
				BEGIN
					INSERT INTO dbo.actions (actionName)
					SELECT @actionName

					SET @actionId = SCOPE_IDENTITY()
				END
			-- exit our transaction to a) release locks on actions ASAP and to b) avoid taking an overly restrictive lock on actionTiming (which could also put us at risk of deadlock)
			COMMIT TRAN

			INSERT INTO dbo.actionTimes (actionId, actionTime)
			SELECT @actionId, @actionTime",
			new
			{
				actionName = actionTiming.Action,
				actionTime = actionTiming.Time
			});
		}
	}
}