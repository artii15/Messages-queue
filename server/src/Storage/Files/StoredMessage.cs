﻿using System;
using Server.Entities;

namespace Server.Storage.Files
{
	[Serializable]
	public class StoredMessage
	{
		public Message Message { get; set; }
		public string Next { get; set; }
	}
}
