using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace srtDownload
{
	[Serializable]
	public class SubtitleState
	{
		private Dictionary<string, SubtitleStateEntry> _dict;

		[XmlIgnore]
		public Dictionary<string, SubtitleStateEntry> Dict
		{
			get { return _dict; }
		}
		public List<SubtitleStateEntry> Entries { get; set; }

		public SubtitleState()
		{
			Entries = new List<SubtitleStateEntry>();
			_dict = new Dictionary<string, SubtitleStateEntry>();
		}

		public void AddEntry(string file, DateTime timestamp)
		{
			if (!_dict.ContainsKey(file))
				_dict.Add(file, new SubtitleStateEntry(file, timestamp));
		}

		public void AddEntries(string[] files, DateTime timestamp)
		{
			foreach (string file in files)
				if (!_dict.ContainsKey(file))
					_dict.Add(file, new SubtitleStateEntry(file, timestamp));
		}

		public void Cleanup(int days = 7)
		{
			List<string> removeKeys = new List<string>();
			foreach (SubtitleStateEntry entry in _dict.Values)
			{
				if (entry.Timestamp.AddDays(days) <= DateTime.Now)
				{
					string fileName = Path.GetFileNameWithoutExtension(entry.File) + ".nosrt";
					StreamWriter w = File.CreateText(fileName);
					w.Write("No subtitle available");
					w.Flush();
					w.Close();

					removeKeys.Add(entry.File);
				}
			}
			foreach (string key in removeKeys)
				_dict.Remove(key);
		}

		public void PostDeserialize()
		{
			foreach (var entry in Entries)
				_dict.Add(entry.File, entry);
		}

		public void PreSerialize()
		{
			Entries.Clear();
			Entries.AddRange(_dict.Values);
		}

	}

	[Serializable]
	public class SubtitleStateEntry
	{
		public string File { get; set; }
		public DateTime Timestamp { get; set; }

		public SubtitleStateEntry()
		{
			File = String.Empty;
			Timestamp = new DateTime();
		}

		public SubtitleStateEntry(string file, DateTime timestamp)
		{
			File = file;
			Timestamp = timestamp;
		}
	}
}
