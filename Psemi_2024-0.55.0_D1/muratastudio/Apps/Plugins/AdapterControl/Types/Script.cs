using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AdapterControl
{
	public class ScriptManager 
	{
		#region Private Members

		private const int MAX_REPEAT = 1000000;
		private const ushort MAX_DELAY = 65535;

		private List<Script> _scripts = new List<Script>();

		#endregion

		#region Public Methods

		public List<Script> Scripts
		{
			get { return _scripts; }
		}

		public bool Load(string filename)
		{
			try
			{
				XDocument scriptDoc = XDocument.Load(filename);
				IEnumerable<XElement> scripts = scriptDoc.Descendants("Script");

				foreach (XElement script in scripts)
				{
					byte? addr = null;

					if ((script.Attribute("SlaveAddress") != null))
					{
						addr = Convert.ToByte(script.Attribute("SlaveAddress").Value, 16);
					}

					Script newScript = new Script
					{
						Name = script.Attribute("Name").Value,
						SlaveAddress = addr
					};

					XElement tag = null;
					foreach (var node in script.Nodes())
					{
						if (node is XElement)
						{
							tag = node as XElement;
						}
						else
						{
							continue;
						}

						switch (tag.Name.LocalName.ToLower())
						{
							case "comm":
							case "pmbus":
							case "i2c":
								CreateTransaction(newScript, tag);
								break;
							case "loop":
								CreateLoop(newScript, tag);
								break;
							case "delay":
								CreateDelay(newScript, tag);
								break;
						}
					}
					_scripts.Add(newScript);
				}
			}
			catch (Exception)
			{
				throw;
			}

			return true;
		}

		#endregion
		
		#region Private Methods

		private void CreateDelay(Script script, XElement item)
		{
			// Limit the delay to 65535 mS
			ulong waitMs = (item.Attribute("WaitMs") == null) ? (ulong)0 : Convert.ToUInt64(item.Attribute("WaitMs").Value);

			script.Actions.Add(new DelayAction
			{
				Operation = OperationType.Delay,
				WaitMs = (waitMs > MAX_DELAY) ? MAX_DELAY : (ushort)waitMs
			});
		}

		private void CreateLoop(Script script, XElement item)
		{
			// Limit the repeat count to 1,000,000
			int repeatCount = (item.Attribute("RepeatCount") == null) ? 1 : Convert.ToInt32(item.Attribute("RepeatCount").Value);
			repeatCount = repeatCount > MAX_REPEAT ? MAX_REPEAT : repeatCount;

			for (int i = 0; i < repeatCount; i++)
			{
				XElement action = null;
				foreach (var node in item.Nodes())
				{
					if (node is XElement)
					{
						action = node as XElement;
					}
					else
					{
						continue;
					}

					switch (action.Name.LocalName.ToLower())
					{
						case "comm":
						case "pmbus":
						case "i2c":
							CreateTransaction(script, action);
							break;
						case "loop":
							CreateLoop(script, action);
							break;
						case "delay":
							CreateDelay(script, action);
							break;
					}
				}
			}
		}

		private void CreateTransaction(Script script, XElement item)
		{
			// Get the data if available
			string[] sData = null;
			List<byte> bData = new List<byte>();
			if (!string.IsNullOrEmpty(item.Value))
			{
				sData = item.Value.Split(' ');

				for (int i = 0; i < sData.Length; i++)
				{
					bData.Add(Convert.ToByte(sData[i], 16));
				}
			}

			ulong repeatCount = (item.Attribute("RepeatCount") == null) ? (ulong)1 : Convert.ToUInt64(item.Attribute("RepeatCount").Value);
			repeatCount = repeatCount > MAX_REPEAT ? MAX_REPEAT : repeatCount;

			ulong innerDelayMs = (item.Attribute("InnerDelayMs") == null) ? (ulong)0 : Convert.ToUInt64(item.Attribute("InnerDelayMs").Value);
			innerDelayMs = innerDelayMs > MAX_DELAY ? MAX_DELAY : innerDelayMs;

			byte? length = (item.Attribute("Length") == null) ? null : (byte?)(Convert.ToInt32(item.Attribute("Length").Value) & 0xFF);

			bool isCommand = item.Attribute("Command") != null;

			Transaction tranaction = new Transaction
			{
				// Required
				Operation = (item.Attribute("Type").Value.ToUpper() == "W") ? OperationType.Write : OperationType.Read,
				Command = isCommand ? Convert.ToByte(item.Attribute("Command").Value, 16) : Convert.ToByte(item.Attribute("RegisterAddress").Value, 16),
				Data = bData.ToArray(),

				// Optional
				RepeatCount = (int)repeatCount,
				InnerDelayMs = (ushort)innerDelayMs,
				Length = (length == null) ? (byte)(sData.Length & 0xFF) : length.Value,
			};
			script.Actions.Add(tranaction);
		}

		#endregion
	}

	public class Script
	{
		public string Name { get; set; }
		public byte? SlaveAddress { get; set; }
		public List<ActionBase> Actions = new List<ActionBase>();
	}

	public abstract class ActionBase
	{
		public OperationType Operation { get; set; }
	}

	public class DelayAction : ActionBase
	{
		public ushort WaitMs { get; set; }
	}

	public class Transaction : ActionBase
	{
		public byte Command { get; set; }
		public byte Length { get; set; }
		public int RepeatCount { get; set; }
		public ushort InnerDelayMs { get; set; }
		public byte[] Data { get; set; }
	}

	public enum OperationType { Read, Write, Delay}
}
