using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Actions.Client.Data;
using Actions.Web.BusinessLogic;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Nyk.Utilities;
using Nyk.Utilities.TableInfrastructure;

namespace Actions.Tests.UnitTests
{
	public class ActionsTimingRecorderTest
	{
		[TestCaseSource(nameof(GenerateTestCases))]
		public async Task TestRetrieve(IEnumerable<ActionTime> actionTimes)
		{
			//not a big fan of this instantiation or much of this mocking
			// with the right amount of time i would build out an infrastructure to quickly leverage/register different components in the test's IoC container, including mocked components
			// however, for the sake of brevity in the project, I will do any such logic manually

			var connectionString = "Server=.\\SQLEXPRESS;Database=ActionsTiming;User Id=Actions_Login;Password=VerbsRFun;";
			var sqlQuery = new SqlQuery(connectionString);
			var tableProviderMock = new Mock<ITableProvider>();

			var actionsMock = await this.MockActionsTableAsync(sqlQuery);
			tableProviderMock.Setup(m => m.Table("actions", It.IsAny<string>())).Returns(actionsMock);

			var actionTimesMock = await this.MockActionTimesTableAsync(sqlQuery);
			tableProviderMock.Setup(m => m.Table("actionTimes", It.IsAny<string>())).Returns(actionTimesMock);

			var actionTimesRetriever = new ActionsTimingRecorder(sqlQuery, tableProviderMock.Object);
			await Task.WhenAll(actionTimes.Select(a => actionTimesRetriever.RecordAsync(a)));

			var uniqueActions = (await sqlQuery.ReadAsync($@"
				SELECT COUNT(DISTINCT actionId) AS distinctActions
				FROM {actionsMock}", new {}, new { distinctActions = default(int) }))
				.Single()
				.distinctActions;

			//ensure that we didn't accidentally create an extra action
			Assert.AreEqual(actionTimes.Select(a => a.Action).Distinct().Count(), uniqueActions);

			var actualActions = await sqlQuery.ReadAsync($@"
				SELECT a.actionName AS action
					, at.actionTime AS time
				FROM {actionTimesMock} at
				JOIN {actionsMock} a
					ON a.actionId = at.actionId", new {}, default(ActionTime));

			Func<IEnumerable<ActionTime>, string> convertToBaseline = vals => JsonConvert.SerializeObject(vals.OrderBy(a => a.Action).ThenBy(a => a.Time));

			var expectedBaseline = convertToBaseline(actionTimes);
			var actualBaseline = convertToBaseline(actualActions);
			Assert.AreEqual(expectedBaseline, actualBaseline);
		}

		#region Table mocking and data set up
		/* ok, honestly this ended up being way more code than I anticipated - I'm used to working with in-house capabilities that would have generated much of this in about 3 lines per table
		 * I believe the complexity of the test code is exceeding the complexity of the actual code at this point, which starts to make it somewhat suspect.
		 * I will still retain the unit tests, but the regression tests in this case will be somewhat more meaningful of coverage
		 */

		private async Task<string> MockActionsTableAsync(ISqlQuery sqlQuery)
		{
			var scratchTableName = $"actions_{Guid.NewGuid().ToString("N")}";

			//we can trivially clean this up with a maintenance process after a period of time
			await sqlQuery.WriteAsync($@"
				CREATE TABLE {scratchTableName}
				(
					actionId INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_{scratchTableName} PRIMARY KEY
						, actionName NVARCHAR(128) CONSTRAINT UQ_{scratchTableName} UNIQUE
				)",
			new {});

			return scratchTableName;
		}


		private async Task<string> MockActionTimesTableAsync(ISqlQuery sqlQuery)
		{
			var scratchTableName = $"actionTimes_{Guid.NewGuid().ToString("N")}";

			await sqlQuery.WriteAsync($@"
				CREATE TABLE {scratchTableName}
				(
					actionTimeId INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_{scratchTableName} PRIMARY KEY
						, actionId INT NOT NULL
						, actionTime FLOAT NOT NULL
				)",
			new {});

			return scratchTableName;
		}
		#endregion

		//these test cases are the same as those in the retriever test, although they do not compute statistics
		private static IEnumerable<TestCaseData> GenerateTestCases()
		{
			return new []
			{
				new TestCaseData(new [] { new ActionTime[0] })
				{
					TestName = "No entries in times table"
				},

				new TestCaseData(arg: new []
					{
						new ActionTime
						{
							Action = "Run",
							Time = 100
						}	
					})
				{
					TestName = "One action with one recorded time"
				},

				new TestCaseData(arg: new []
					{
						new ActionTime
						{
							Action = "Run",
							Time = 100
						},
						new ActionTime
						{
							Action = "Run",
							Time = 200
						}
					})
				{
					TestName = "One action with one multiple recorded times"
				},

				new TestCaseData(arg: new []
					{
						new ActionTime
						{
							Action = "Run",
							Time = 100
						},
						new ActionTime
						{
							Action = "Run",
							Time = 100
						}
					})
				{
					TestName = "One action with duplicate times"
				},

			new TestCaseData(arg: new []
				{
					new ActionTime
					{
						Action = "Run",
						Time = 99.9
					},
					new ActionTime
					{
						Action = "Run",
						Time = 107.5
					}
				})
			{
				TestName = "One action with fractional time"
			},

			new TestCaseData(arg: new []
					{
						new ActionTime
						{
							Action = "Run",
							Time = 100
						},
						new ActionTime
						{
							Action = "Walk",
							Time = 1000
						}
					})
				{
					TestName = "Multiple actions with single times"
				},

				new TestCaseData(arg: new []
					{
						new ActionTime
						{
							Action = "Run",
							Time = 100
						},
						new ActionTime
						{
							Action = "Run",
							Time = 200
						},
						new ActionTime
						{
							Action = "Walk",
							Time = 1000
						},
						new ActionTime
						{
							Action = "Walk",
							Time = 2000
						}
					})
				{
					TestName = "Multiple actions with multiple times"
				},

				new TestCaseData(arg: new []
					{
						new ActionTime
						{
							Action = "Robert'; DROP TABLE dbo.actionTimes; --",
							Time = 100
						}
					})
				{
					TestName = "SQL Injection"
				},
			};
		}
	}
}