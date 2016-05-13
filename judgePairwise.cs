using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace judgePairwise
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader sr_weight = new StreamReader("MC2-E-iunits.tsv");//只用來拿到 qID and uID
            Dictionary<string, string> qu = new Dictionary<string, string>();
            while (!sr_weight.EndOfStream)
            {
                string[] ID = sr_weight.ReadLine().Split('\t');
                qu.Add(ID[1], ID[0]);
            }
            sr_weight.Close();
            
            //分類，變成key是query，value是uid
            IEnumerable<IGrouping<string, string>> igroups = from a in qu group a.Key by a.Value;
            Dictionary<String, int> rank = new Dictionary<string, int>();
            StreamReader sr_predict = new StreamReader("20160203predict.tsv");
            foreach (var query in igroups)
            {
                for(int i = 0; i < query.Count(); i++)
                {
                    for(int j = i+1; j < query.Count(); j++)
                    {
                        string label = sr_predict.ReadLine();

                        if (String.IsNullOrEmpty(label)) break;

                        if (label.Equals("2"))//>
                        {

                            if (rank.ContainsKey(query.ElementAt(i)))
                                rank[query.ElementAt(i)]++;
                            else
                                rank.Add(query.ElementAt(i), 1);

                            if (!rank.ContainsKey(query.ElementAt(j)))
                                rank.Add(query.ElementAt(j), 0);
                        }
                        else if (label.Equals("0"))//<
                        {
                            if (query.ElementAt(j).Equals("MC2-E-0004-0027"))
                                Console.WriteLine("0027 0");

                            if (rank.ContainsKey(query.ElementAt(j)))
                                rank[query.ElementAt(j)]++;
                            else
                                rank.Add(query.ElementAt(j), 1);

                            if (!rank.ContainsKey(query.ElementAt(i)))
                                rank.Add(query.ElementAt(i), 0);
                        }
                        else if (label.Equals("1"))
                        {
                            if (!rank.ContainsKey(query.ElementAt(j)))
                                rank.Add(query.ElementAt(j), 0);

                            if (!rank.ContainsKey(query.ElementAt(i)))
                                rank.Add(query.ElementAt(i), 0);
                        }
                    }
                }
            }

            StreamWriter sw = new StreamWriter("20160203result.tsv");
            foreach (KeyValuePair<String, int> kvp in rank)
                sw.WriteLine(kvp.Key.Substring(0, 10) + "\t" + kvp.Key + "\t" + kvp.Value);
            sw.Close();
        }
    }
}
