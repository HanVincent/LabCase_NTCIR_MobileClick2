using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;


namespace HTMLtoContent
{
    class Program
    {
        static public NLP NLPmethods = new NLP();
        static void Main(string[] args)
        {
            //read query list file
            string[][] queryTokenList = new string[204][];   //有斷詞、過濾StopWord
            string[] queryList = new string[204];                 //原始query
            StreamReader sr = new StreamReader(Setting.queryListFile);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string query = line.Substring(10);
                int number = Convert.ToInt32(line.Substring(6,4));
                string[] queryTokens = NLPmethods.Stemming(NLPmethods.FilterOutStopWords(NLPmethods.Tokenization(query)));

                queryTokenList[number] = queryTokens;  //將(1-203)query都放array，不排除空白query
                queryList[number] = query;
            }
            sr.Close();

            //foreach html
            if (Directory.Exists(Setting.HTML_DirectoryPath))
            {
                for (int qId = 1; qId <= 203; qId++)
                {
                    int HTMLcountInThisQuery = Directory.GetFiles(Setting.HTML_DirectoryPath, "1C2-E-" + String.Format("{0:D4}", qId) + "-*.html").Length; //這個query有幾個網頁

                    List<Sentence> Q_Sens = new List<Sentence>();
                    List<string[]> All_Sens = new List<string[]>(); 

                    int alreadyGetSentencesFromQid = 0, fileCount = 0;


                   // Dictionary<string, int> dict = new Dictionary<string, int>();
                    if ( HTMLcountInThisQuery > 0) {
                        for (int i = 1; /*alreadyGetSentencesFromQid < Setting.numOfSentencesEachQ &&*/ fileCount < HTMLcountInThisQuery; i++)
                        {//依照搜尋引擎順序，foreach每個網頁
                            string file = Setting.HTML_DirectoryPath + "\\1C2-E-" + String.Format("{0:D4}", qId) + "-" + i + ".html";

                            DirectoryInfo di = new DirectoryInfo(file);

                            if (!File.Exists(file))
                                continue;
                            else
                                fileCount++;

                            Console.WriteLine("正在處理檔案：" + di.Name);

                            //把html讀進來
                            sr = new StreamReader(file);
                            string html = sr.ReadToEnd();
                            sr.Close();


                            HtmlDocument doc = new HtmlDocument();
                            HtmlNode.ElementsFlags.Remove("form");//沒什麼特別的，只是不要讓HtmlAgilityPack刪掉form node，http://stackoverflow.com/questions/2385840/how-to-get-all-input-elements-in-a-form-with-htmlagilitypack
                            doc.LoadHtml(html);

                            HtmlNode bodyNode = doc.DocumentNode.SelectSingleNode("//body");
                            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//html//head//title");

                            string[] titleTokens = null;
                            if (titleNode != null)
                                titleTokens = NLPmethods.Stemming(NLPmethods.FilterOutStopWords(NLPmethods.Tokenization(WebUtility.HtmlDecode(titleNode.InnerText))));


                            /*QA網站分析*/
                            Pair<string, double>[] parseResult = null; //question-weight pair
                            parseResult = QA.ExtractBlocks(doc);
                            if (parseResult == null)
                            {//若不是QA網站

                                MainBodyDetector mbd = new MainBodyDetector(bodyNode, Setting.thresholdT);//偵測哪些node是main body, 哪些要過濾
                                TopicBlocks tbs = ExtractBlocks(bodyNode, mbd, titleTokens);
                                parseResult = (tbs == null ? null : tbs.getBlocksWithWeight(queryTokenList[qId].ToArray()));
                            }


                            Sentence[] sentences = SplitToSentences(parseResult, queryTokenList[qId].ToArray(), i);

                            foreach (Sentence s in sentences)
                            {
                                if (alreadyGetSentencesFromQid < Setting.OutputSentencesEachQ)
                                    s.isTop = true;
                                else
                                    s.isTop = false;

                                Q_Sens.Add(s);
                                alreadyGetSentencesFromQid++;
                            }
                        }
                    } else {
                        continue;
                    }


                    //LexRank
                    //LexRank.getScore(Q_Sens.ToArray());

                    //Lucene
                    //Lucene.indexing(Q_Sens.ToArray());
                    //Lucene.query(Q_Sens.ToArray(), queryList[qId - 1]);

                    Q_Sens.Sort(delegate(Sentence x, Sentence y)
                    {
                        //double a = x.lexRank * x.logRank * x.tf * x.topicWeight;
                        //double b = y.lexRank * y.logRank * y.tf * y.topicWeight;

                        //設定你想要的排序!!!
                        double a = x.searchRank;
                        double b = y.searchRank;

                        return a.CompareTo(b) * (1);
                    });

                    //output
                    if (!Directory.Exists(Setting.outputDirectoryPath))
                        Directory.CreateDirectory(Setting.outputDirectoryPath);

                    StreamWriter sw = new StreamWriter(Setting.outputDirectoryPath + @"\" + qId + ".txt");
            //        StreamWriter sw = new StreamWriter(Setting.outputDirectoryPath + "\\1C2-E-" + String.Format("{0:D4}", qId) + "-" + qId + ".html");
                    HashSet<string> alreadyOutput = new HashSet<string>();
                    foreach (Sentence s in Q_Sens)
                    {
                        if (!alreadyOutput.Contains(s.sentnece) && s.isTop)
                        {
                            sw.WriteLine("sentence:\t\t\t" + s.sentnece);
                            sw.WriteLine("with chunker:\t\t" + s.senWithChunk);
                            //sw.WriteLine("parser:\n" + NLPmethods.Parser(s.sentnece));
                            sw.WriteLine("term freq:\t\t\t" + s.tf);
                            sw.WriteLine("search rank:\t\t" + s.searchRank);
                            sw.WriteLine("logRank:\t\t\t" + s.logRank);
                            //sw.WriteLine("lexRank:\t\t\t" + s.lexRank);
                            //sw.WriteLine("subtopic weight:\t" + s.topicWeight);
                            //sw.WriteLine("lucene:\t\t\t\t" + s.lucene);
                            sw.WriteLine("not stopword count:\t" + s.stemTokens.Length);
                            sw.Write("stemed:\t");
                            foreach (string w in s.stemTokens)
                                sw.Write(w + " ");
                            sw.WriteLine();

                            sw.WriteLine("total token count:\t" + s.tokens.Length);
                            sw.WriteLine("----------------------------------------------------------------");
                            sw.Flush();

                            alreadyOutput.Add(s.sentnece);
                        }
                    }
                    sw.Close();
                    
                }
            }

            Console.WriteLine("[Finish!]");
            Console.ReadKey();

        }
        static private string ExtractText(HtmlNode node)
        {
            string result = "";
            if (Setting.ignoreTags.Contains(node.Name))
                return " ";
            else if (node.ChildNodes.Count == 0)
                return WebUtility.HtmlDecode(node.InnerText.Replace("\n", " ").Replace("\r", " "))  + " ";
            else if (node.ChildNodes.Count > 0)
            {
                foreach (HtmlNode n in node.ChildNodes)
                    result += ExtractText(n);
            }

            return result;
        }

        //遞迴
        static private TopicBlocks ExtractBlocks(HtmlNode node, MainBodyDetector mbd, string[] titleTokens, TopicBlocks tbs = null, bool isRoot = true)
        {
            if (node == null)
                return null;

            if(tbs == null)
                tbs = new TopicBlocks(titleTokens);

            if (Setting.ignoreTags.Contains(node.Name) || !mbd.isMainBody(node))
                tbs.addExtractedText("\n");
            else
            {
                Regex HeadRegex = new Regex("h[1-6]");
                if (HeadRegex.IsMatch(node.Name) && node.Name.Length == 2)//是"h?"
                {
                    //save
                    tbs.SaveBlock();

                    //change header
                    int hx = Convert.ToInt16(node.Name.Substring(1));
                    string[] subtopicTokens = NLPmethods.Stemming(NLPmethods.FilterOutStopWords(NLPmethods.Tokenization(WebUtility.HtmlDecode(ExtractText(node)))));
                    tbs.addNewHeader(hx, subtopicTokens);

                    return tbs;
                }

                if (node.ChildNodes.Count == 0)
                {
                    tbs.addExtractedText(WebUtility.HtmlDecode(node.InnerText.Replace("\n", " ").Replace("\r", " ")));
                }
                else
                {
                    foreach (HtmlNode n in node.ChildNodes)
                    {
                        if (n.Name.Equals("a"))
                            tbs.addExtractedText("\n");

                        if (Setting.changeLineTags.Contains(n.Name))
                            tbs.addExtractedText("\n");

                        ExtractBlocks(n, mbd, null, tbs, false);

                        if (Setting.changeLineTags.Contains(n.Name))
                            tbs.addExtractedText("\n");

                    }
                }
            }
            if(isRoot)
                tbs.SaveBlock();

            return tbs;
        }
        static private Sentence[] SplitToSentences(Pair<string, double>[] blocksAndWeight, string[] query, int rank)
        {
            List<Sentence> result = new List<Sentence>();
            if (blocksAndWeight == null)
                return result.ToArray();

            foreach (Pair<string, double> block in blocksAndWeight)
            {
                string[] lines = block.first.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string[] sentences = NLPmethods.SentDetect(line);
                    foreach (string s in sentences)
                    {
                        string afterProcess = s.Trim(new char[] { '\t', ' ' , '-', '*'});

                        Regex whiteRegex = new Regex("[ \t]+");
                        afterProcess = whiteRegex.Replace(afterProcess, " ");

                        if (!NLPmethods.isQuestion(afterProcess))
                        {
                            string[] tokens = (NLPmethods.Tokenization(afterProcess));
                            string[] stemTokens = NLPmethods.Stemming(NLPmethods.FilterOutStopWords(tokens));

                            int tf = 0;
                            foreach (string token in stemTokens)
                                if (query.Contains(token))
                                    tf++;

                            string[] conjunction = {"to", "and", "in", "at", "is", "of", "for", "a", "an"};
                            bool likeTitle = true;
                            foreach (string t in tokens)
                            {
                                bool StartWithCapital = (t[0] >= 'A' && t[0] <= 'Z');
                                bool allNotEng = true;
                                foreach (char c in t)
                                {
                                    if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                                    {
                                        allNotEng = false;
                                        break;
                                    }
                                }


                                if (!StartWithCapital && !conjunction.Contains(t) && !allNotEng)
                                {
                                    likeTitle = false;
                                    break;
                                }
                            }
                            if (likeTitle && tokens.Length < 5)
                                likeTitle = false;


                            if (stemTokens.Length >= 3 && !likeTitle)
                            {
                                Sentence sen = new Sentence(afterProcess, tokens, stemTokens, tf, block.second, rank);
                                result.Add(sen);
                            }
                        }
                    }
                }
            }
            return result.ToArray();
        }

        static private string[] GetAllSentences(Pair<string, double>[] blocksAndWeight, string[] query, int rank)
        {
            List<string> result = new List<string>();
            if (blocksAndWeight == null)
                return result.ToArray();

            foreach (Pair<string, double> block in blocksAndWeight)
            {
                string[] lines = block.first.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string[] sentences = NLPmethods.SentDetect(line);
                    foreach (string s in sentences)
                    {
                        string afterProcess = s.Trim(new char[] { '\t', ' ', '-', '*' });

                        Regex whiteRegex = new Regex("[ \t]+");
                        afterProcess = whiteRegex.Replace(afterProcess, " ");

                        if (!NLPmethods.isQuestion(afterProcess))
                        {
                            string[] tokens = (NLPmethods.Tokenization(afterProcess));
                            string[] stemTokens = NLPmethods.Stemming(NLPmethods.FilterOutStopWords(tokens));

                            foreach (string t in stemTokens)
                                result.Add(t);
                        }
                    }
                }
            }
            return result.ToArray();
        }

        static private string[] GetAllSentences2(Pair<string, double>[] blocksAndWeight, string[] query, int rank)
        {
            List<string> result = new List<string>();
            if (blocksAndWeight == null)
                return result.ToArray();

            foreach (Pair<string, double> block in blocksAndWeight)
            {
                string[] lines = block.first.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string[] sentences = NLPmethods.SentDetect(line);
                    foreach (string s in sentences)
                    {
                        string afterProcess = s.Trim(new char[] { '\t', ' ', '-', '*' });

                        Regex whiteRegex = new Regex("[ \t]+");
                        afterProcess = whiteRegex.Replace(afterProcess, " ");

                        if (!NLPmethods.isQuestion(afterProcess))
                        {
                            //string[] tokens = (NLPmethods.Tokenization(afterProcess));
                            //string[] stemTokens = NLPmethods.Stemming(NLPmethods.FilterOutStopWords(tokens));

                            //foreach (string t in stemTokens)
                            result.Add(afterProcess);
                        }
                    }
                }
            }
            return result.ToArray();
        }

    }
}
