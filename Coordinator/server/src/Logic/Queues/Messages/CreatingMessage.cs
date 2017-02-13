﻿using System.Data;
using RestSharp;
namespace Server
{
	public class CreatingMessage
	{
		readonly IDbConnection DBConnection;
		const int TIMEOUT = 30000;

		public CreatingMessage(IDbConnection dbConnection)
		{
			DBConnection = dbConnection;
		}

		public void Create(CreateMessage request)
		{ 
			if (!QueuesQueries.QueueExists(DBConnection, request.QueueName))
				throw new QueueNotExistsException();
			else
			{
				var queue = QueuesQueries.getQueueByName(DBConnection, request.QueueName);
				var worker = WorkerQueries.GetWorkerById(DBConnection, queue.Worker);
				var coworker = WorkerQueries.GetWorkerById(DBConnection, queue.Cooperator);

				if (WorkerQueries.IsWorkerAlive(DBConnection, worker.Id))
				{
					var response = PropagateRequest(request, worker, coworker);
					if (response.ResponseStatus == ResponseStatus.TimedOut ||
						response.ResponseStatus == ResponseStatus.Error)
					{
						PropagateRequestToCoworker(request, coworker);
						QueuesQueries.swapWorkers(DBConnection, queue);
					}
				}
				else
				{
					PropagateRequestToCoworker(request, coworker);
					QueuesQueries.swapWorkers(DBConnection, queue);
				}
			}
		}

		IRestResponse PropagateRequest(CreateMessage request, Worker worker, Worker coworker)
		{
			var client = new RestClient($"http://{worker.Address}");
			client.Timeout = TIMEOUT;
			var requestToSend = new RestRequest($"queues/{request.QueueName}/messages", Method.POST);
			requestToSend.AddParameter("Content", request.Content);
			requestToSend.AddParameter("Cooperator", coworker.Address);
			return client.Execute(requestToSend);
		}

		void PropagateRequestToCoworker(CreateMessage request, Worker coworker)
		{
			var coworkerClient = new RestClient($"http://{coworker.Address}");
			var coworkerRequestToSend = new RestRequest($"queues/{request.QueueName}/messages", Method.POST);
			coworkerRequestToSend.AddParameter("Content", request.Content);
			coworkerClient.Execute(coworkerRequestToSend);
		}
	}
}
