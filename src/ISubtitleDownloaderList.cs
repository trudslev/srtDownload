using System;
using System.Linq;
using System.Collections.Generic;
using SubtitleDownloader.Core;
using System.Reflection;

namespace srtDownload
{
	public class ISubtitleDownloaderList : List<ISubtitleDownloader>
	{
		public ISubtitleDownloader GetSubtitleDownloader(string name)
		{
			name = name.ToLowerInvariant();
			foreach (ISubtitleDownloader i in this)
				if (i.GetName().ToLowerInvariant().Equals(name))
					return i;
			return null;
		}

		public static ISubtitleDownloaderList GetSubtitleDownloaders()
		{
			var type = typeof(ISubtitleDownloader);
			var types = AppDomain.CurrentDomain.GetAssemblies().ToList()
				.SelectMany(s => s.GetTypes())
				.Where(p => type.IsAssignableFrom(p));
			var result = new ISubtitleDownloaderList();
			foreach (Type t in types)
			{
				if(t.Name.Equals("ISubtitleDownloader"))
					continue;
				ConstructorInfo ctor = t.GetConstructor(System.Type.EmptyTypes);
				if (ctor != null)
				{
					object instance = ctor.Invoke(null);
					result.Add(instance as ISubtitleDownloader);
				}
			}
			return result;
		}


	}
}
