using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;

using NLPTech;

namespace local_experiments
{
    class Program
    {
        static void Main(string[] args)
        {
            //RepresentAdjByWordEmbeddings(@"D:\v-zw\metaphor_learning\data\wikipedia\adj.tsv", @"D:\v-zw\corpora\word2vec\validation_cmp\vec.bin", @"D:\v-zw\metaphor_learning\data\wikipedia\adj_we.tsv");

            //ExtractAdjFromPairMentions(@"D:\v-zw\metaphor_learning\data\wikipedia\asadjas.tsv", @"D:\v-zw\metaphor_learning\data\wikipedia\adj.tsv");

            //TextNLPArsenal(@"D:\v-zw\opennlpmodels");

            TextReader ips = new StreamReader(@"D:\v-zw\metaphor_learning\data\coling2016\LabeledData.txt");
            TextWriter ops = new StreamWriter(@"D:\v-zw\metaphor_learning\data\coling2016\dd_test_type1.tsv");

            while (ips.Peek() != -1)
            {
                string[] cols = ips.ReadLine().Split('\t');

                ops.WriteLine(cols[4] + "\t" + cols[5] + "\t" + cols[13] + "\t" + (string.IsNullOrEmpty(cols[0]) ? "0" : "1"));
            }

            ips.Close();
            ops.Close();
        }

        static void RepresentAdjByWordEmbeddings(string adj_path, string we_path, string output_path)
        {
            BinaryReader bips = new BinaryReader(new FileStream(@"D:\v-zw\corpora\word2vec\validation_cmp\vec.bin", FileMode.Open, FileAccess.Read));
            int first = 0;
            int second = 0;
            int cur = 0;

            while (true)
            {
                char ch = (char)bips.ReadByte();

                if (ch == '\n')
                {
                    second = cur;
                    cur = 0;
                    break;
                }

                if (ch == ' ')
                {
                    first = cur;
                    cur = 0;
                    continue;
                }

                cur *= 10;
                cur += ch - '0';
            }



            Console.WriteLine(first.ToString() + "\t" + second.ToString());

            Dictionary<string, float[]> we = new Dictionary<string, float[]>(first);

            while (bips.PeekChar() != -1)
            {
                StringBuilder word = new StringBuilder();
                char ch = (char)bips.ReadByte();

                while (ch != ' ')
                {
                    word.Append(ch);
                    ch = (char)bips.ReadByte();
                }

                byte[] vec = bips.ReadBytes(sizeof(float) * second);
                float[] emb = new float[second];
                Buffer.BlockCopy(vec, 0, emb, 0, vec.Length);
                we.Add(word.ToString(), emb);
                bips.ReadByte();
            }

            bips.Close();

            TextReader ips = new StreamReader(adj_path);
            TextWriter ops = new StreamWriter(output_path);

            while (ips.Peek() != -1)
            {
                string[] cols = ips.ReadLine().Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (we.ContainsKey(cols[0]) == true)
                {
                    ops.WriteLine(cols[0] + "\t" + string.Join(",", we[cols[0]]));
                }
            }

            ips.Close();
            ops.Close();
        }

        static void ExtractAdjFromPairMentions(string input_path, string output_path)
        {
            TextReader ips = new StreamReader(input_path);

            Regex rg = new Regex(@"\b(is|was|has been|be|being) as (\w)+ as( (a|an))?\b");

            Dictionary<string, int> dict = new Dictionary<string, int>();

            while (ips.Peek() != -1)
            {
                string[] cols = ips.ReadLine().Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (cols.Length != 4)
                {
                    Console.WriteLine("Error row encountered.");
                    continue;
                }

                string mention = cols[2];
                var match_info = rg.Match(mention);

                if (match_info.Success == true)
                {
                    for (int i = match_info.Index; i < match_info.Index + match_info.Length; ++i)
                    {
                        if (mention[i] == ' ' && i + 1 < match_info.Index + match_info.Length &&
                            mention[i + 1] == 'a' && i + 2 < match_info.Index + match_info.Length &&
                            mention[i + 2] == 's')
                        {
                            int adj_index = i + 4;
                            int adj_length = 1;

                            while (mention[adj_index + adj_length] != ' ')
                            {
                                adj_length++;
                            }

                            string adj = mention.Substring(adj_index, adj_length).ToLower();

                            /* if (string.Compare(adj, "as") == 0)
                            {
                                Console.WriteLine(mention + "\t" + "\t" + i.ToString() + "\t" + adj_index.ToString() + "\t" + adj_length.ToString());
                                Console.ReadLine();
                            } */

                            if (dict.ContainsKey(adj) == false)
                            {
                                dict.Add(adj, 0);
                            }

                            dict[adj]++;

                            break;
                        }
                    }
                }
            }

            ips.Close();

            TextWriter ops = new StreamWriter(output_path);
            int total = 0;

            foreach (var kv in dict.OrderByDescending(x=>x.Value))
            {
                ops.WriteLine(kv.Key + "\t" + kv.Value.ToString());
                total += kv.Value;
            }

            ops.Close();

            Console.WriteLine(total.ToString());
        }

        static void TextNLPArsenal(string folder_name)
        {
            string[] model_names = new string[]{
                "en-sent.bin",
"en-token.bin",
"en-pos-maxent.bin",
"en-chunker.bin",
"en-ner-person.bin",
"en-ner-organization.bin",
"en-ner-location.bin",
"en-ner-date.bin",
"en-ner-money.bin",
"en-ner-percentage.bin",
"en-ner-time.bin",
"en-parser-chunking.bin",
"engmalt.linear-1.7"
            };

            for (int i = 0; i < model_names.Length; ++i)
            {
                model_names[i] = folder_name + "\\" + model_names[i];
            }

            NLPArsenal ob = new NLPArsenal(model_names);

            string text = "Born and raised in Liverpool, Lennon became involved in the skiffle craze as a teenager; his first band, the Quarrymen, evolved into the Beatles in 1960. When the group disbanded in 1970, Lennon embarked on a solo career that produced the critically acclaimed albums John Lennon/Plastic Ono Band and Imagine, and iconic songs such as \"Give Peace a Chance\", \"Working Class Hero\", and \"Imagine\". After his marriage to Yoko Ono in 1969, he changed his name to John Ono Lennon. Lennon disengaged himself from the music business in 1975 to raise his infant son Sean, but re-emerged with Ono in 1980 with the new album Double Fantasy. He was murdered three weeks after its release.";

            var ss = ob.SentenceSegmentation(text);

            foreach (var s in ss)
            {
                string sent = text.Substring(s.getStart(), s.getEnd() - s.getStart());
                Console.WriteLine(sent);

                var tkss = ob.Tokenization(sent);
                string[] tks = new string[tkss.Length];
                for (int i = 0; i < tks.Length; ++i)
                {
                    tks[i] = sent.Substring(tkss[i].getStart(), tkss[i].getEnd() - tkss[i].getStart());
                }

                var postags = ob.PartOfSpeechTagging(tks);
                for (int i = 0; i < tks.Length; ++i)
                {
                    Console.Write(tks[i] + "|" + postags[i] + "\t");
                }
                Console.WriteLine();
            }
        }
    }
}
