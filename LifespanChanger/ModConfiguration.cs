using System;
using System.IO;
using System.Xml.Serialization;

namespace LifespanChanger
{
	public class ModConfiguration
	{
		public string LifespanValue;

		public ModConfiguration()
		{
			this.LifespanValue = ModMain.LifespanValues[0];
		}

		public static bool Serialize(string filename, ModConfiguration config)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModConfiguration));
			try
			{
				using (StreamWriter streamWriter = new StreamWriter(filename))
				{
					xmlSerializer.Serialize(streamWriter, config);
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		public static ModConfiguration Deserialize(string filename)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModConfiguration));
			try
			{
				using (StreamReader streamReader = new StreamReader(filename))
				{
					ModConfiguration modConfiguration = (ModConfiguration)xmlSerializer.Deserialize(streamReader);
					if (Array.IndexOf<string>(ModMain.LifespanValues, modConfiguration.LifespanValue) < 0)
					{
						modConfiguration.LifespanValue = ModMain.LifespanValues[0];
					}
					return modConfiguration;
				}
			}
			catch
			{
			}
			return null;
		}
	}
}
