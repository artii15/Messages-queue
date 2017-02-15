﻿using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Timers;
using RestSharp;

namespace Server
{
    class Program
    {
		static void Main (string[] args)
        {
			if (args.Length != 1) 
			{
				Console.Error.WriteLine ("Usage: server <listen_address>");
				Environment.Exit(1);
			}
			var addressPattern = new Regex(@"^(http|https)://.+(:d+)?/?$");
			var listenAddress = string.Concat(args[0].TrimEnd(new char[] {'/'}), "/");
			if (!addressPattern.IsMatch(listenAddress))
			{
				Console.Error.WriteLine ("Invalid address format");
				Environment.Exit(2);
			}

			var appHost = new AppHost(listenAddress);
            appHost.Init ();            
			appHost.Start (listenAddress);
			appHost.BeginRecovery();
			BeginHeartbeat(listenAddress);

			Console.WriteLine ("AppHost Created at {0}, listening on {1}", DateTime.Now, listenAddress);
			Console.WriteLine("Press <ENTER> key to exit...");
			Console.ReadLine();
			appHost.Stop();
        }

		static void BeginHeartbeat(string ownAddress)
		{
			var heartbeatClient = new RestClient(Environment.GetEnvironmentVariable("COORDINATOR_ADDRESS"));
			var heartbeatRequest = new RestRequest(ConfigurationManager.AppSettings["HeartbeatPath"], Method.POST);
			heartbeatRequest.RequestFormat = DataFormat.Json;
			heartbeatRequest.AddJsonBody(new { Address = ownAddress, Id = int.Parse(Environment.GetEnvironmentVariable("WORKER_ID")) });

			var heartbeatInterval = double.Parse(ConfigurationManager.AppSettings["HeartbeatInterval"]);
			var timer = new Timer(heartbeatInterval);
			timer.AutoReset = true;
			timer.Elapsed += (sender, e) => SendHeartbeat(heartbeatClient, heartbeatRequest);
			SendHeartbeat(heartbeatClient, heartbeatRequest);
			timer.Start();
		}

		static void SendHeartbeat(RestClient client, RestRequest request)
		{
			client.Execute(request);
		}
    }
}