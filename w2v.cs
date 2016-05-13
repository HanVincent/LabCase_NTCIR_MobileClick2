using NLP_Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace w2v
{
    class Program
    {
        static public NLP nlp = new NLP();
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

            #region Total all query 100d scores
            Console.WriteLine("處理query 100d scores");
            string[] queries = File.ReadAllLines("1C2-E-queries.tsv");
            Dictionary<string, double[]> query_score = new Dictionary<string, double[]>();
            for(int i = 0; i < 100; i++)
            {
                double[] temp = new double[100];
                string qid = queries[i].Split('\t')[0];
                string query = queries[i].Split('\t')[1];

                string[] tokens = query.ToLower().Split(' ', '/', ',', '"');
                #region NLP
                //string[] tokens = query.Split(new char[] { ' ', '-', '\'', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                //string[] tokens = nlp.Tokenize(query);
                //Filter out stopwords
                //tokens = nlp.FilterOutStopWords(tokens);
                #endregion

                //string[] tokens = queries[i].Split(' ','\t').Skip(1).ToArray();
                foreach(string token in tokens)
                {
                    for(int j = 0; j < 100; j++)
                    {
                        if (w2v_dic.ContainsKey(token))
                            temp[j] += w2v_dic[token][j];
                    }
                }
                query_score.Add(qid, temp);
            }
            #endregion

            #region Count iunit score, minus and Writedown.
            Console.WriteLine("Calculate each 100d");
            StreamReader sr_iunit = new StreamReader("1C2-E-iunits.tsv");
            StreamReader oddsRatio = new StreamReader("no_smoothing_infreq.tsv");
            StreamWriter sw = new StreamWriter("NLP_svm_w2v.tsv");
            while (!sr_iunit.EndOfStream)
            {
                string[] iunit = sr_iunit.ReadLine().Split('\t');
                string[] itokens = iunit[2].ToLower().Split(' ', '/', ',', '"');

                
                #region NLP
                //string[] tokens = query.Split(new char[] { ' ', '-', '\'', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                //string[] itokens = nlp.Tokenize(iunit[2]);
                //Filter out stopwords
                //itokens = nlp.FilterOutStopWords(itokens);
                #endregion


                double[] temp = new double[100];
                sw.Write(iunit[0] + '\t' + String.Format("{0:D4}", iunit[1]) + '\t');
                for (int i = 0; i < 100; i++)
                {
                    foreach (string token in itokens)
                    {
                        if (w2v_dic.ContainsKey(token))
                            temp[i] += w2v_dic[token][i];
                    }
                    temp[i] -= query_score[iunit[0]][i];
                    sw.Write((i + 1) + ":" + temp[i] + " ");
                }
                sw.WriteLine("101:" + oddsRatio.ReadLine().Split('\t')[2]);
                sw.Flush();
            }
            sr_iunit.Close();
            oddsRatio.Close();
            sw.Close();
            #endregion
        }
    }
}
