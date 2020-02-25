using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Actions.Web.Data;
using Actions.Web.BusinessLogic;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Nyk.Utilities;
using Nyk.Utilities.TableInfrastructure;

namespace Actions.Tests.UnitTests
{
	public class ActionsTimingRetrieverTest
	{
		[TestCaseSource(nameof(GenerateTestCases))]
		public async Task TestRetrieve(IEnumerable<ActionTime> actionTimes, IEnumerable<ActionStatistics> expectedStatistics)
		{
			//not a big fan of this instantiation or much of this mocking
			// with the right amount of time i would build out an infrastructure to quickly leverage/register different components in the test's IoC container, including mocked components
			// however, for the sake of brevity in the project, I will do any such logic manually

			var connectionString = "Server=.\\SQLEXPRESS;Database=ActionsTiming;User Id=Actions_Login;Password=VerbsRFun;";
			var sqlQuery = new SqlQuery(connectionString);
			var tableProviderMock = new Mock<ITableProvider>();

			var actionsMock = await this.MockActionsTableAsync(sqlQuery, actionTimes);
			tableProviderMock.Setup(m => m.Table("actions", It.IsAny<string>())).Returns(actionsMock);

			var actionTimesMock = await this.MockActionTimesTableAsync(sqlQuery, actionTimes, actionsMock);
			tableProviderMock.Setup(m => m.Table("actionTimes", It.IsAny<string>())).Returns(actionTimesMock);

			var actionTimesRetriever = new ActionsTimingRetriever(sqlQuery, tableProviderMock.Object);
			var actualStatistics = await actionTimesRetriever.RetrieveAsync();


			var expectedBaseline = JsonConvert.SerializeObject(expectedStatistics.OrderBy(s => s.Name));
			var actualBaseline = JsonConvert.SerializeObject(actualStatistics.OrderBy(s => s.Name));
			Assert.AreEqual(expectedBaseline, actualBaseline);
		}

		#region Table mocking and data set up
		/* ok, honestly this ended up being way more code than I anticipated - I'm used to working with in-house capabilities that would have generated much of this in about 3 lines per table
		 * I believe the complexity of the test code is exceeding the complexity of the actual code at this point, which starts to make it somewhat suspect.
		 * I will still retain the unit tests, but the regression tests in this case will be somewhat more meaningful of coverage
		 */

		private async Task<string> MockActionsTableAsync(ISqlQuery sqlQuery, IEnumerable<ActionTime> actionTimes)
		{
			var scratchTableName = $"actions_{Guid.NewGuid().ToString("N")}";

			var insertStatement = string.Empty;
			if (actionTimes.Any())
			{
				var actionUnionAll = string.Join($"{Environment.NewLine}UNION ALL{Environment.NewLine}", actionTimes.Select(a => $"SELECT '{a.Action}' AS actionName").Distinct());

				insertStatement = $@"INSERT INTO {scratchTableName} (actionName)
				{actionUnionAll}";
			}

			//we can trivially clean this up with a maintenance process after a period of time
			await sqlQuery.WriteAsync($@"
				CREATE TABLE {scratchTableName}
				(
					actionId INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_{scratchTableName} PRIMARY KEY
						, actionName NVARCHAR(128) CONSTRAINT UQ_{scratchTableName} UNIQUE
				)
				{insertStatement}",
			new {});

			return scratchTableName;
		}


		private async Task<string> MockActionTimesTableAsync(ISqlQuery sqlQuery, IEnumerable<ActionTime> actionTimes, string actions)
		{
			var scratchTableName = $"actionTimes_{Guid.NewGuid().ToString("N")}";

			var insertStatement = string.Empty;
			if (actionTimes.Any())
			{
				var actionUnionAll = string.Join($"{Environment.NewLine}UNION ALL{Environment.NewLine}", actionTimes.Select(a => $"SELECT '{a.Action}' AS actionName, {a.Time} AS actionTime"));

				insertStatement = $@"INSERT INTO {scratchTableName} (actionId, actionTime)
				SELECT a.actionId
					, ua.actionTime
				FROM (
					{actionUnionAll}
				) ua
				JOIN {actions} a
					ON a.actionName = ua.actionName";
			}

			await sqlQuery.WriteAsync($@"
				CREATE TABLE {scratchTableName}
				(
					actionTimeId INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_{scratchTableName} PRIMARY KEY
						, actionId INT NOT NULL
						, actionTime FLOAT NOT NULL
				)
				{insertStatement}",
			new {});

			return scratchTableName;
		}
		#endregion

		private static IEnumerable<TestCaseData> GenerateTestCases()
		{
			return new []
			{
				new TestCaseData(new ActionTime[0], new ActionStatistics[0])
				{
					TestName = "No entries in times table"
				},

				new TestCaseData(new []
					{
						new ActionTime
						{
							Action = "Run",
							Time = 100
						}	
					},
					new []
					{
						new ActionStatistics
						{
							Name = "Run",
							Avg = 100
						}	
					})
				{
					TestName = "One action with one recorded time"
				},

				new TestCaseData(new []
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
					},
					new []
					{
						new ActionStatistics
						{
							Name = "Run",
							Avg = 150
						}	
					})
				{
					TestName = "One action with one multiple recorded times"
				},

				new TestCaseData(new []
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
					},
					new []
					{
						new ActionStatistics
						{
							Name = "Run",
							Avg = 100
						}	
					})
				{
					TestName = "One action with duplicate times"
				},

			new TestCaseData(new []
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
				},
				new []
				{
					new ActionStatistics
					{
						Name = "Run",
						Avg = 103.7
					}	
				})
			{
				TestName = "One action with fractional time"
			},

			new TestCaseData(new []
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
					},
					new []
					{
						new ActionStatistics
						{
							Name = "Run",
							Avg = 100
						},
						new ActionStatistics
						{
							Name = "Walk",
							Avg = 1000
						}	
					})
				{
					TestName = "Multiple actions with single times"
				},

				new TestCaseData(new []
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
					},
					new []
					{
						new ActionStatistics
						{
							Name = "Run",
							Avg = 150
						},
						new ActionStatistics
						{
							Name = "Walk",
							Avg = 1500
						}	
					})
				{
					TestName = "Multiple actions with multiple times"
				},
			};
		}
	}
}