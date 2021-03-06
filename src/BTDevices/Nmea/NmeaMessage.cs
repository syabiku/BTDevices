﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BTDevices.Nmea
{
	public class NmeaMessageType : Attribute { public string Type { get; set; } }
	
	public abstract class NmeaMessage
	{
		public static NmeaMessage Parse(string message)
		{
			int checksum = -1;
			if (message[0] != '$')
				throw new ArgumentException("Invalid nmea message: Missing starting character '$'");
			var idx = message.IndexOf('*');
			if (idx >= 0)
			{
				checksum = Convert.ToInt32(message.Substring(idx + 1), 16);
				message = message.Substring(0, message.IndexOf('*'));
			}
			if (checksum > -1)
			{
				int checksumTest = 0;
				for (int i = 1; i < message.Length; i++)
				{
					if (i == 0) continue;
					checksumTest ^= Convert.ToByte(message[i]);
				}
				if (checksum != checksumTest)
					throw new ArgumentException("Invalid nmea message: Checksum failure");
			}

			string[] parts = message.Split(new char[] { ',' });
			string MessageType = parts[0].Substring(1);
			string[] MessageParts = parts.Skip(1).ToArray();
			if(messageTypes == null)
			{
				LoadResponseTypes();
			}
			NmeaMessage msg = null;
			if (messageTypes.ContainsKey(MessageType))
			{
				msg = (NmeaMessage)messageTypes[MessageType].Invoke(new object[] { });
			}
			else
			{
				msg = new UnknownMessage();
			}
			msg.MessageType = MessageType;
			msg.MessageParts = MessageParts;
			msg.LoadMessage(MessageParts);
			return msg;
		}

		private static void LoadResponseTypes()
		{
			messageTypes = new Dictionary<string, ConstructorInfo>();
			var typeinfo = typeof(NmeaMessage).GetTypeInfo();
			foreach (var subclass in typeinfo.Assembly.DefinedTypes.Where(t => t.IsSubclassOf(typeof(NmeaMessage))))
			{
				var attr = subclass.GetCustomAttribute<NmeaMessageType>(false);
				if (attr != null)
				{
					if (!subclass.IsAbstract)
					{
						foreach (var c in subclass.DeclaredConstructors)
						{
							var pinfo = c.GetParameters();
							if (pinfo.Length == 0)
							{
								messageTypes.Add(attr.Type, c);
								break;
							}
						}
					}
				}
			}
		}

		private static Dictionary<string, ConstructorInfo> messageTypes;

		protected string[] MessageParts { get; private set; }

		public string MessageType { get; private set; }

		protected virtual void LoadMessage(string[] message) { MessageParts = message; }

		public override string ToString()
		{
			return string.Format("${0},{1}", MessageType, string.Join(",", MessageParts));
		}
	}
}
