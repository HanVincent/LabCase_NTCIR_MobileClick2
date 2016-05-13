using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTMLtoContent
{
    class Setting
    {
        public const int numOfSentencesEachQ = 1000; //讀幾句後就停止 (後面的html就不會跑)
        public const int OutputSentencesEachQ = 1000;//最後要輸出幾句 (< numOfSentencesEachQ)

        //LDA related
        public const int topicCount = 5;

        //lexRank related
        public const double d = 0.85;
        public const double linkThreshold = 0.2;
        public const double convergenceThreshold = 0.0000000001;

        //main body detector related
        public const double thresholdT = 0.4;

        //tag related
        static public readonly string[] changeLineTags = { "p", "div", "marquee", "hr", "br", "img", "table", "frameset", "address", "body", "code", "ol", "option", "pre", "span", "ul", "li" };
        static public readonly string[] garnishTags = { "a", "b", "i", "u", "ins", "strike", "s", "del", "kbd", "tt", "font", "var" };
        static public readonly string[] ignoreTags = { "script", "noscript", "style", "#comment" };

        //path related
        public const string queryListFile = "1C2-E-queries.tsv";
        public const string HTML_DirectoryPath = @".\1C2-E.HTML";
        public const string outputDirectoryPath = @".\Converted";
    }
}
