using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pairwise
{
    class Program
    {
        static void Main(string[] args)
        {
           
            #region 處理w2v字典
            Console.WriteLine("處理w2v字典");
            Dictionary<string, double[]> w2v_dic = new Dictionary<string, double[]>();
            string[] w2v_txt = File.ReadAllLines("w2v.txt");
            foreach (string line in w2v_txt)
            {
                string word = line.Split(' ')[0];
                string[] dim = line.Split(' ').Skip(1).ToArray();
                double[] scores = new double[100];
                for (int i = 0; i < 100; i++)
                {
                    scores[i] = Convert.ToDouble(dim[i]);
                }
                w2v_dic.Add(word, scores);
            }
            #endregion

            #region 處理iunit - query
            Console.WriteLine("處理 iunit - query 的 100d scores");
            string[] queries = File.ReadAllLines("1C2-E-queries.tsv");
            //各q的分數
            Dictionary<string, double[]> query_score = new Dictionary<string, double[]>();
            for (int i = 0; i < 100; i++)   
            {
                double[] temp = new double[100];
                string qid = queries[i].Split('\t')[0];
                string q = queries[i].Split('\t')[1].ToLower();
                if (q.StartsWith("what ") || q.StartsWith("how to"))
                    continue;

                string[] tokens = q.Split(' ');
                bool abc = false;
                foreach(string token in tokens)
                    if (token.Equals("of") || token.Equals("to") || tokens.Equals("across"))
                        abc = true;

                if (abc) continue;

                foreach (string token in tokens)
                {
                    for (int j = 0; j < 100; j++)
                    {
                        if (w2v_dic.ContainsKey(token))
                            temp[j] += w2v_dic[token][j];
                    }
                }
                query_score.Add(qid, temp);
            }

            //u-q的分數
            Dictionary<string, double[]> u_q_score = new Dictionary<string, double[]>();
            StreamReader sr_iunit = new StreamReader("1C2-E-iunits.tsv");
            while (!sr_iunit.EndOfStream)
            {
                string[] iunit = sr_iunit.ReadLine().Split('\t');
                string[] itokens = iunit[2].ToLower().Split(' ', '/', ',', '"', '(', ')');
                double[] temp = new double[100];

                if (!query_score.Keys.Contains(iunit[0]))
                    continue;

                for (int i = 0; i < 100; i++)
                {
                    foreach (string token in itokens)
                    {
                        if (w2v_dic.ContainsKey(token))
                            temp[i] += w2v_dic[token][i];
                    }
                    temp[i] -= query_score[iunit[0]][i];
                }
                u_q_score.Add(iunit[1], temp);
            }
            sr_iunit.Close();
            #endregion

            #region weight_dic
            Dictionary<string, int> weight_dic = new Dictionary<string, int>();
            StreamReader sr = new StreamReader("1C2-E-weights.tsv");
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string uid = line.Split('\t')[1];
                int weight = Convert.ToInt16(line.Split('\t')[2]);
                weight_dic.Add(uid, weight);
            }
            #endregion


            #region 處理"Pair比較"
            Console.WriteLine("處理Pairwise");
            Dictionary<Tuple<int, string>, double> oddsratio_dic = new Dictionary<Tuple<int, string>, double>();
            string[] oddsratio_line = File.ReadAllLines("train_no_smoothing.tsv");
            for (int i = 0; i < oddsratio_line.Length; i++)
            {
                string[] info = oddsratio_line[i].Split('\t');
                int qid = Convert.ToInt16(info[0].Substring(6, 4));

                oddsratio_dic.Add(new Tuple<int, string>(qid, info[1]), Convert.ToDouble(info[2]));
            }

            StreamWriter sw1 = new StreamWriter("20160203pairwise.tsv");
            IEnumerable<IGrouping<int, Tuple<string, double>>> igroups = from a in oddsratio_dic group new Tuple<string, double>(a.Key.Item2, a.Value) by a.Key.Item1;

            foreach( var query in igroups )
            {
                for(int i = 0; i < query.Count(); i++)
                {
                    if (!u_q_score.Keys.Contains(query.ElementAt(i).Item1))
                        continue;

                    for(int j = i + 1; j < query.Count(); j++)
                    {
                        //training data
                        if (weight_dic[query.ElementAt(i).Item1] > weight_dic[query.ElementAt(j).Item1])
                            sw1.Write("2 ");
                        else if (weight_dic[query.ElementAt(i).Item1] == weight_dic[query.ElementAt(j).Item1])
                            sw1.Write("1 ");
                        else
                            sw1.Write("0 ");

                        //test data
                        //if (query.ElementAt(i).Item2 > query.ElementAt(j).Item2)
                        //    sw1.Write("2 ");
                        //else if (query.ElementAt(i).Item2 == query.ElementAt(j).Item2)
                        //    sw1.Write("1 ");
                        //else
                        //    sw1.Write("0 ");

                        for (int k = 0; k < 100; k++)
                            sw1.Write((k + 1) + ":" + u_q_score[query.ElementAt(i).Item1][k] + " ");
                        for (int k = 0; k < 100; k++)
                            sw1.Write((k + 101) + ":" + u_q_score[query.ElementAt(j).Item1][k] + " ");

                        /*
                        sw1.WriteLine("1:" + query.ElementAt(i).Item2
                                    + " 2:" + query.ElementAt(j).Item2 
                                    + " 3:" + (query.ElementAt(i).Item2 - query.ElementAt(j).Item2));*/
                        sw1.WriteLine("201:" + (query.ElementAt(i).Item2 - query.ElementAt(j).Item2));
                        sw1.Flush();
                    }
                }
            }
            sw1.Close();
            #endregion
        }
    }
}
