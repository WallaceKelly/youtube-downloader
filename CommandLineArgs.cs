using Args;
using System;
using System.ComponentModel;

namespace YouTubeDownloader
{
    class CommandLineArgs
    {
        [Description("Single YouTube link to download.")]
        [ArgsMemberSwitch(new[] { "l" })]
        public string Link { get; set; }

        [Description("Input file, containing one URL per line.")]
        [ArgsMemberSwitch(new[] { "i" })]
        public string InputFile { get; set; }

        [Description("Folder to write the result(s).")]
        [DefaultValue("c:\\temp\\")]
        [ArgsMemberSwitch(new[] { "f" })]
        public string Folder { get; set; }

        [Description("Only download the audio track(s).")]
        [DefaultValue(false)]
        [ArgsMemberSwitch(new[] { "a" })]
        public bool AudioOnly { get; set; }

        public void CheckArgs()
        {
            var linkIsSpecified = !String.IsNullOrWhiteSpace(Link);
            var inputFileIsSpecified = !String.IsNullOrWhiteSpace(InputFile);
            
            if (!linkIsSpecified && !inputFileIsSpecified)
            {
                var msg = "Either a link or input file must be specified.";
                throw new ArgumentException(msg);
            }

            if (linkIsSpecified && inputFileIsSpecified)
            {
                var msg = "Both a link and input file cannot be included.";
                throw new ArgumentException(msg);
            }
        }
    }
}