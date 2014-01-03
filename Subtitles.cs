using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubtitlesParser
{
    public class Subtitles : List<Subtitle>
    {
        #region Subrip

        public static async Task<Subtitles> ReadFromSubripAsync(Stream stream)
        {
            var streamResult = await ReadStreamAsync(stream);

            return await ReadFromSubripAsync(streamResult);
        }

        public static async Task<Subtitles> ReadFromSubripAsync(string source)
        {
            const string timeCodeRegex = @"^([\d\,:]+)\s+-->\s+([\d\,:]+)(?:\s,*)?$";

            var subtitles = new Subtitles();
            subtitles.AddRange(from subtitlePart in await ReadSubripSubtitleParts(source)
                select subtitlePart.Split(new[] {Environment.NewLine}, StringSplitOptions.None).ToList()
                into lines
                where lines.Count > 3
                let timeCodes = Regex.Match(lines[1], timeCodeRegex)
                where timeCodes.Success
                let start = ParseSubripTimecode(timeCodes.Groups[1].Value)
                let end = ParseSubripTimecode(timeCodes.Groups[2].Value)
                where !start.Equals(-1) || !end.Equals(-1)
                let text = string.Join<string>(Environment.NewLine, lines.Skip(2).Where(line => !string.IsNullOrEmpty(line)).ToList())
                select new Subtitle
                {
                    Text = text, Start = TimeSpan.FromMilliseconds(start), End = TimeSpan.FromMilliseconds(end)
                });

            return subtitles;
        }

        protected internal static async Task<List<string>> ReadSubripSubtitleParts(string subtitles)
        {
            var subtitleParts = new List<string>();
            var subtitleBuilder = new StringBuilder();

            await Task.Run(() =>
            {
                foreach (var line in subtitles.Split(new[] {Environment.NewLine}, StringSplitOptions.None))
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        subtitleParts.Add(subtitleBuilder.ToString());
                        subtitleBuilder = new StringBuilder();
                    }
                    else
                    {
                        subtitleBuilder.AppendLine(line);
                    }
                }
            });

            return subtitleParts;
        }

        protected internal static double ParseSubripTimecode(string timecode)
        {
            if (!Regex.IsMatch(timecode, "[0-9]+:[0-9]+:[0-9]+,[0-9]+")) return -1;
            TimeSpan timeSpan;
            return TimeSpan.TryParse(timecode.Replace(",", "."), out timeSpan) ? timeSpan.TotalMilliseconds : -1;
        }

        #endregion

        #region WebVtt

        public static async Task<Subtitles> ReadFromWebVttAsync(Stream stream)
        {
            var streamResult = await ReadStreamAsync(stream);

            return await ReadFromWebVttAsync(streamResult);
        }

        public static async Task<Subtitles> ReadFromWebVttAsync(string source)
        {
            const string timeCodeRegex = @"^([\d\.:]+)\s+-->\s+([\d\.:]+)(?:\s.*)?$";

            var subtitles = new Subtitles();
            subtitles.AddRange(from subtitlePart in await ReadWebVttSubtitleParts(source)
                where !subtitlePart.Contains("WEBVTT", StringComparison.OrdinalIgnoreCase)
                select subtitlePart.Split(new[] {Environment.NewLine}, StringSplitOptions.None).ToList()
                into lines
                let firstIndex = lines.FindIndex(line => Regex.IsMatch(line, timeCodeRegex))
                where firstIndex != -1
                let timeCodes = Regex.Match(lines[firstIndex], timeCodeRegex)
                where timeCodes.Success
                let start = ParseWebVttTimecode(timeCodes.Groups[1].Value)
                let end = ParseWebVttTimecode(timeCodes.Groups[2].Value)
                where !start.Equals(-1) || !end.Equals(-1)
                let text = string.Join<string>(Environment.NewLine, lines.Skip(firstIndex + 1).Where(line => !string.IsNullOrEmpty(line)).ToList())
                select new Subtitle
                {
                    Text = text, Start = TimeSpan.FromMilliseconds(start), End = TimeSpan.FromMilliseconds(end)
                });

            return subtitles;
        }

        protected internal static double ParseWebVttTimecode(string timeCode)
        {
            TimeSpan timeSpan;

            if (Regex.IsMatch(timeCode, @"^\s*(\d+)?:?(\d+):([\d\.]+)\s*$"))
                timeCode = string.Format("00:{0}", timeCode);

            return TimeSpan.TryParse(timeCode, out timeSpan) ? timeSpan.TotalMilliseconds : -1;
        }

        protected internal static async Task<List<string>> ReadWebVttSubtitleParts(string subtitles)
        {
            var subtitleParts = new List<string>();
            var subtitleBuilder = new StringBuilder();
            
            await Task.Run((() =>
            {
                foreach (var line in subtitles.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    if (string.IsNullOrEmpty(line.Trim()))
                    {
                        subtitleParts.Add(subtitleBuilder.ToString());
                        subtitleBuilder = new StringBuilder();
                    }
                    else
                    {
                        subtitleBuilder.AppendLine(line);
                    }
                }

                if (subtitleBuilder.Length > 0)
                    subtitleParts.Add(subtitleBuilder.ToString());

                subtitleParts.RemoveAll(string.IsNullOrEmpty);

            }));

            return subtitleParts;
        }

        #endregion

        protected internal static async Task<string> ReadStreamAsync(Stream stream)
        {
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                return await streamReader.ReadToEndAsync();
            }
        }
    }
}
