using System;

namespace SubtitlesParser
{
    public class Subtitle
    {
        public string Text { get; set; }

        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }
    }
}
