srtDownload
===========

A simple command line program (for any platform using either Microsoft .NET or mono)

Usage
-----

srtDownload has a couple of options when you use it from the command line:

	Usage: srtDownload.exe [options...] <directory>
	Options:
	  -v, --verbose    Shows more information, otherwise nothing is output (cron
	                   mode)
	  -s, --state      Path of state file (remembers when files were scanned)
	  -g, --giveupdays The number of days after which the program gives up getting
	                   the subtitle and writes a .nosrt file.
	  -i, --ignore     Path of file containing ignored shows.
	                   A text file with a show name on each line. The name is the
	                   part of the the filename up to the season/episode id.
	                   E.g. "Criminal.Minds.S08E07.HDTV.x264-LOL.mp4" will be
	                   ignored with a line of "Criminal Minds" in the file.

At the moment it only downloads english subtitles. It would be pretty easy to add an option to select what language(s) to download. Let me know if you would like that.

SubtitleDownloader.dll
----------------------

Subtitle downloader uses the scraper library created by sekotin, which can be found at https://www.assembla.com/spaces/subtitledownloader/wiki. If you use this program, you should go make a donation to him, since this program would not be possible without it.