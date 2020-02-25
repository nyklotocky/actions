using System;
using System.Threading.Tasks;
using Actions.Client.Data;
using Actions.Web.BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Actions.Web.Controllers
{
	[AllowAnonymous] //not ideal, but since we don't have any auth requirements for this project, let's defer this decision
	[ApiController]
	[Route("[controller]")]
	public class ActionsTimingController : ControllerBase
	{
		private readonly ActionsTimingRecorder _actionsTimingRecorder;
		private readonly ActionsTimingRetriever _actionsTimingRetriever;

		public ActionsTimingController(
			ActionsTimingRecorder actionsTimingRecorder,
			ActionsTimingRetriever actionsTimingRetriever)
		{
			if (actionsTimingRecorder is null)
			{
				throw new ArgumentNullException(nameof(actionsTimingRecorder));
			}

			if (actionsTimingRetriever is null)
			{
				throw new ArgumentNullException(nameof(actionsTimingRetriever));
			}

			this._actionsTimingRecorder = actionsTimingRecorder;
			this._actionsTimingRetriever = actionsTimingRetriever;
		}

		[HttpPost]
		[Route("v1/AddAction")]
		public async Task<ActionResult> AddAction([FromBody] ActionTime actionTiming)
		{
			await this._actionsTimingRecorder.RecordAsync(actionTiming);

			return this.Ok();
		}

		[HttpGet]
		[Route("v1/GetStats")]
		public async Task<ActionResult> GetStats()
		{
			var stats = await this._actionsTimingRetriever.RetrieveAsync();
			var serializedStats = JsonConvert.SerializeObject(stats);

			return Content(serializedStats, "application/json");
		}
	}
}
