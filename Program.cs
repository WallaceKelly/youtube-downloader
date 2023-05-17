using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YoutubeExtractor;
using Args;
using Args.Help;
using Args.Help.Formatters;

namespace YouTubeDownloader
{
    class Program
    {
        private static object showProgressLock = new object();
        private static void ShowUsage()
        {
            Console.WriteLine("YouTube Downloader");
            var helpProvider = new HelpProvider();
            var modelHelp = helpProvider.GenerateModelHelp<CommandLineArgs>(Configuration.Configure<CommandLineArgs>());
            var consoleHelpFormatter = new ConsoleHelpFormatter();
            consoleHelpFormatter.WriteHelp(modelHelp, Console.Out);
        }
        
        private static void ShowProgress(string msg, double progress)
        {
            lock (Program.showProgressLock)
            {
                var count = (int)Math.Round(progress / 2.0);
                var arg = new string('=', count);
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("[{0}{1:F0}%", arg, progress);
                var count2 = 50 + "[100%".Length - stringBuilder.Length;
                var arg2 = new string(' ', count2);
                stringBuilder.AppendFormat("{0}]", arg2);
                Console.SetCursorPosition(0, 0);
                Console.WriteLine(msg);
                Console.Write(stringBuilder);
            }
        }

        private static void Main(string[] args)
        {
            try
            {
                var commandLineArgs = GetCommandLineArgs(args);

                if (!Directory.Exists(commandLineArgs.Folder))
                {
                    Console.WriteLine("Creating {0}.", commandLineArgs.Folder);
                    Directory.CreateDirectory(commandLineArgs.Folder);
                }

                var isLinkSpecified = !String.IsNullOrWhiteSpace(commandLineArgs.Link);

                var isInputFileSpecified = !String.IsNullOrWhiteSpace(commandLineArgs.InputFile);
                var fileLinks = isInputFileSpecified ?
                    File.ReadLines(commandLineArgs.InputFile).ToArray() :
                    new[] { commandLineArgs.Link };

                foreach (var file in fileLinks)
                {
                    if (commandLineArgs.AudioOnly)
                    {
                        Program.DownloadAudio(file, commandLineArgs.Folder);
                    }
                    else
                    {
                        Program.DownloadVideo(file, commandLineArgs.Folder);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.GetType().Name);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static CommandLineArgs GetCommandLineArgs(string[] args)
        {
            var commandLineArgs = Configuration.Configure<CommandLineArgs>().CreateAndBind(args);
            try
            {
                commandLineArgs.CheckArgs();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Program.ShowUsage();
                Environment.Exit(1);
            }
            return commandLineArgs;
        }

        private static string CleanTitle(string title)
        {
            return title.Replace(':', '_').Replace('|', ' ');
        }

        private static void DownloadAudio(string link, string folder)
        {
            var downloadUrls = DownloadUrlResolver.GetDownloadUrls(link);
            VideoInfo videoInfo =
                downloadUrls.Where(i => i.VideoType == VideoType.Mp4 && i.Resolution == 0 && i.CanExtractAudio)
                            .OrderByDescending(q => q.AudioBitrate).FirstOrDefault();
            if(videoInfo == null)
            {
                throw new ArgumentException("The audio track cannot be extracted from this video.");
            }
            if (videoInfo.RequiresDecryption)
            {
                DownloadUrlResolver.DecryptDownloadUrl(videoInfo);
            }

            var title = CleanTitle(videoInfo.Title + videoInfo.AudioExtension);
            var audioDownloader = new AudioDownloader(videoInfo, Path.Combine(folder, title));
            audioDownloader.DownloadProgressChanged += (sender, a) =>
                Program.ShowProgress("Downloading " + link, a.ProgressPercentage * 0.85);
            audioDownloader.AudioExtractionProgressChanged += (sender, a) =>
                Program.ShowProgress(" Extracting " + link, 85.0 + a.ProgressPercentage * 0.15);
            audioDownloader.Execute();
        }

        private static void DownloadVideo(string link, string folder)
        {
            var downloadUrls = DownloadUrlResolver.GetDownloadUrls(link);
            var videoInfo = downloadUrls.First((VideoInfo info) => info.VideoType == VideoType.Mp4 && info.Resolution == 360);
            var title = CleanTitle(videoInfo.Title + videoInfo.VideoExtension);
            var videoDownloader = new VideoDownloader(videoInfo, Path.Combine(folder, title));
            videoDownloader.DownloadProgressChanged += (sender, a) =>
                Program.ShowProgress("Downloading " + link, a.ProgressPercentage);
            videoDownloader.Execute();
        }
    }
}

