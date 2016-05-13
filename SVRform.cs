using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVRform
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] weight_line = File.ReadAllLines("1C2-E-weights.tsv");
            Dictionary<Tuple<int, string>, string> weight_dic = new Dictionary<Tuple<int, string>, string>();
            for (int i = 0; i < weight_line.Length; i++)
            {
                string id = weight_line[i].Split('\t')[1];
                int qid = Convert.ToInt16(weight_line[i].Split('\t')[0].Substring(6, 4));
                string weight = weight_line[i].Split('\t')[2];

                weight_dic.Add(new Tuple<int, string>(qid, id), weight);
            }

            string[] ratio_line = File.ReadAllLines("NLP_svm_w2v.tsv");
            Dictionary<Tuple<int, string>, string> odds_dict = new Dictionary<Tuple<int, string>, string>();
            for (int i = 0; i < ratio_line.Length; i++)
            {
                int qid = Convert.ToInt16(ratio_line[i].Split('\t')[0].Substring(6, 4));
                string id = ratio_line[i].Split('\t')[1];
                string oddsratio = ratio_line[i].Split('\t')[2];
                odds_dict.Add(new Tuple<int, string>(qid, id), oddsratio);
            }
                  
            int fold = 5;
            StreamWriter[] sw_training = new StreamWriter[fold];
            StreamWriter[] sw_test = new StreamWriter[fold];
            for(int i = 0; i < fold; i++)
            {
                sw_training[i] = new StreamWriter(i + "_training.txt");
                sw_test[i] = new StreamWriter(i + "_test.txt");
            }

            IEnumerable<IGrouping<int, Tuple<string, string>>> query = from a in weight_dic group new Tuple<string, string>(a.Key.Item2, a.Value) by a.Key.Item1;

            int index = 0;//寫入第幾檔案
            int q_num = 0;//第幾筆query
            foreach(IGrouping<int, Tuple<string, string>> group in query)
            {
                int qid = group.Key;
                List<Tuple<string, string>> weights = group.ToList();
                //ps item1 = uid, item2 = weight

                foreach(Tuple<string, string> t in weights)
                    sw_test[index].WriteLine(t.Item2 + "\t" + odds_dict[new Tuple<int, string>(qid, t.Item1)]);

                for (int k = 0; k < fold; k++)
                {
                    if (k != index % fold)
                    {
                        foreach (Tuple<string, string> t in weights)
                            sw_training[k].WriteLine(t.Item2 + "\t" + odds_dict[new Tuple<int, string>(qid, t.Item1)]);
                    }
                }
                
                //100筆query切成5等分，連續20筆query為test/train
                q_num++;
                if (q_num % 20 == 0)
                    index++;
            }

            for (int i = 0; i < fold; i++)
            {
                sw_training[i].Close();
                sw_test[i].Close();
            }
        }
    }
}
