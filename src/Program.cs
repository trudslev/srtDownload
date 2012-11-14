using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SubtitleDownloader.Core;
using SubtitleDownloader.Implementations.OpenSubtitles;
using SubtitleDownloader.Implementations.Bierdopje;
using SubtitleDownloader.Implementations.Subscene;
using SubtitleDownloader.Implementations.Podnapisi;
using System.Text.RegularExpressions;
using System.IO;
using SubtitleDownloader.Util;
using System.Xml.Serialization;

namespace srtDownload
{
	class Program
	{
		static List<ISubtitleDownloader> downloaders;
		static Dictionary<string, string> ignoreShows = new Dictionary<string, string>();
		static bool Verbose = false;
		static SubtitleState state;
		static string stateFileName = "srtDownload.xml";
		static string ignoreFileName = null;
		static int giveUpDays = 7;

		static void Main(string[] args)
		{
			downloaders = new List<ISubtitleDownloader>()
			{
				new SubsceneDownloader(),
				new BierdopjeDownloader(),
				new PodnapisiDownloader(),
				new OpenSubtitlesDownloader()
			};

			if (args.Length == 0)
			{
				ShowHelp();
				return;
			}
			var arguments = new List<string>();
			bool grabStateName = false;
			bool grabIgnoreName = false;
			bool grabGiveUpDays = false;
			foreach (string arg in args)
			{
				if (grabStateName)
				{
					stateFileName = arg;
					grabStateName = false;
				}
				else if (grabIgnoreName)
				{
					ignoreFileName = arg;
					grabIgnoreName = false;
				}
				else if (grabGiveUpDays)
				{
					if (!int.TryParse(arg, out giveUpDays))
					{
						Console.WriteLine("Give up days is not an integer. Using default.");
						giveUpDays = 7;
					}
				}
				else if (arg.StartsWith("-"))
				{
					if (arg == "-v" || arg == "--verbose")
						Verbose = true;
					if (arg == "-s" || arg == "--state")
						grabStateName = true;
					if (arg == "-i" || arg == "--ignore")
						grabIgnoreName = true;
					if (arg == "-g" || arg == "--giveupdays")
						grabGiveUpDays = true;
					if (arg == "-h" || arg == "--help")
					{
						ShowHelp();
						return;
					}
				}
				else
					arguments.Add(arg);
			}

			if (ignoreFileName != null)
			{
				TextReader tr = new StreamReader(ignoreFileName);
				try
				{
					string s = null;
					while ((s = tr.ReadLine()) != null)
						ignoreShows.Add(s.ToLowerInvariant(), s);
				}
				finally
				{
					tr.Close();
				}
				Log("Ignore shows file loaded. {0} shows ignored.", ignoreShows.Keys.Count);
			}

			/*string curDir = Directory.GetCurrentDirectory();
			Directory.SetCurrentDirectory("//172.16.22.2/series/Series/");
			arguments.Add("Jimmy.Fallon.2012.10.29.Seth.Meyers.HDTV.XviD-AFG.avi");
			arguments.Add("Boston.Legal.S03.AC3.DVDRip.XviD-WAT");
			arguments.Add("Who.Do.You.Think.You.Are.US.S01E02.Emmitt.Smith.HDTV.XviD-FQM.avi");*/

			LoadState();

			if (arguments.Count == 0)
				arguments.Add(".");

			foreach (string file in arguments)
				if (Directory.Exists(file))
				{
					Log("Processing directory {0}...", file);
					state.AddEntries(Directory.GetFiles(file), DateTime.Now);
				}
				else if (File.Exists(file))
					state.AddEntry(file, DateTime.Now);

			var dict = state.Dict;
			List<string> removeKeys = new List<string>();
			foreach (SubtitleStateEntry entry in dict.Values)
			{
				if (ProcessFile(entry.File))
					removeKeys.Add(entry.File);
			}
			foreach (string key in removeKeys)
				dict.Remove(key);

			//Directory.SetCurrentDirectory(curDir);
			SaveState();
		}

		static void ShowHelp()
		{
			Console.WriteLine("Usage: srtDownload.exe [options...] <directory>");
			Console.WriteLine("Options:");
			Console.WriteLine(" -v, --verbose    Shows more information, otherwise nothing is output (cron");
			Console.WriteLine("                  mode)");
			Console.WriteLine(" -s, --state      Path of state file (remembers when files were scanned)");
			Console.WriteLine(" -g, --giveupdays The number of days after which the program gives up getting");
			Console.WriteLine("                  the subtitle and writes a .nosrt file.");
			Console.WriteLine(" -i, --ignore     Path of file containing ignored shows.");
			Console.WriteLine("                  A text file with a show name on each line. The name is the");
			Console.WriteLine("                  part of the the filename up to the season/episode id.");
			Console.WriteLine("                  E.g. \"Criminal.Minds.S08E07.HDTV.x264-LOL.mp4\" will be ");
			Console.WriteLine("                  ignored with a line of \"Criminal Minds\" in the file.");
		}

		static bool ProcessFile(string fileName)
		{
			var path = Path.GetDirectoryName(fileName);
			var ext = Path.GetExtension(fileName);
			if (ext != ".avi" && ext != ".mkv" && ext != ".mp4")
				return true;
			var file = Path.GetFileNameWithoutExtension(fileName);
			string targetSubtitleFile = Path.Combine(path, file) + ".srt";
			string targetNoSubtitleFile = Path.Combine(path, file) + ".nosrt";
			if (File.Exists(targetSubtitleFile) || File.Exists(targetNoSubtitleFile))
				return true;

			string name,group;
			int season,episode;
			if (ParseEpisodeInfo(Path.GetFileNameWithoutExtension(fileName), out name, out season, out episode, out group))
			{
				if (ignoreShows.ContainsKey(name.ToLowerInvariant()))
				{
					Log("Ignoring {0}", fileName);
					return true;
				}

				Log("Processing file {0}...", fileName);

				EpisodeSearchQuery query = new EpisodeSearchQuery(name, season, episode, null);
				query.LanguageCodes = new string[] { "eng" };

				foreach (ISubtitleDownloader downloader in downloaders)
					try
					{
						foreach (Subtitle subtitle in downloader.SearchSubtitles(query))
						{
							string compareName, compareGroup;
							int compareSeason, compareEpisode;
							ParseEpisodeInfo(subtitle.FileName, out compareName, out compareSeason, out compareEpisode, out compareGroup);
							if (name.ToLowerInvariant() == compareName.ToLowerInvariant() &&
								season == compareSeason &&
								episode == compareEpisode &&
								group.ToLowerInvariant() == compareGroup.ToLowerInvariant())
							{
								Log("Downloading subtitles from {0}...", downloader.GetName());
								try
								{
									List<FileInfo> subtitleFiles = downloader.SaveSubtitle(subtitle);
									FileInfo subtitleFile = subtitleFiles[0];
									Log("Renaming from {0} to {1}...", subtitleFile.FullName, targetSubtitleFile);
									File.Delete(targetSubtitleFile);
									File.Move(subtitleFile.FullName, targetSubtitleFile);
								}
								catch (Exception)
								{
									continue;
								}
								return true;
							}
						}
					}
					catch (Exception e)
					{
						Log("Downloader {0} failed: {1}", downloader.GetName(), e.Message);
					}
			}
			return false;
		}

		static bool ParseEpisodeInfo(string fileName, out string SeriesName, out int Season, out int Episode, out string Group)
		{
			SeriesName = String.Empty;
			Season = 0;
			Episode = 0;
			Group = String.Empty;
			Match match = Regex.Match(fileName, @"([a-z0-9\.]+?)S([0-9][0-9])E([0-9][0-9])[a-z0-9\.]+?-([a-z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			if (match.Success)
			{
				SeriesName = match.Groups[1].Value.Replace(".", " ").Trim();
				Season = Int32.Parse(match.Groups[2].Value);
				Episode = Int32.Parse(match.Groups[3].Value);
				Group = match.Groups[4].Value;
			}
			return match.Success;
		}

		static void Log(string format, params object[] parms)
		{
			if (Verbose)
				Console.WriteLine(String.Format(format, parms));
		}

		static void LoadState()
		{
			XmlSerializer xs = new XmlSerializer(typeof(SubtitleState));
			try
			{
				Log("Loading state from {0}...", stateFileName);
				TextReader reader = new StreamReader(stateFileName);
				state = null;
				try
				{
					state = (SubtitleState)xs.Deserialize(reader);
					if (state != null)
						state.PostDeserialize();
				}
				finally
				{
					reader.Close();
					Log("State loaded. {0} entries...", state.Dict.Count);
				}
			}
			catch (Exception e)
			{
				Log("Could not load state. Exception: {0}.", e.Message);
			}
			if (state == null)
				state = new SubtitleState();
			state.Cleanup(giveUpDays);
		}

		static void SaveState()
		{
			XmlSerializer xs = new XmlSerializer(typeof(SubtitleState));
			TextWriter writer = new StreamWriter(stateFileName);
			try
			{
				try
				{
					Log("Saving state to {0}...", stateFileName);
					state.PreSerialize();
					xs.Serialize(writer, state);
				}
				finally
				{
					writer.Close();
					Log("State saved.");
				}
			}
			catch (Exception e)
			{
				Log("Could not save state. Exception: {0}.", e.Message);
			}
		}
	}
}
