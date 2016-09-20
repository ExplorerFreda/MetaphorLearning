using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace local_experiments
{
    class Loader
    {
        public static List<Tuple<string, string>> LoadProbasePairs(string file_path, int discard_threshold = 5)
        {
            List<Tuple<string, string>> pairs = new List<Tuple<string, string>>();
            TextReader ips = new StreamReader(file_path);

            while (ips.Peek() != -1)
            {
                string line = ips.ReadLine();
                string[] cols = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length != 3) continue;
                int count = int.Parse(cols[2]);
                if (count < discard_threshold) continue;
                pairs.Add(new Tuple<string, string>(cols[1], cols[0]));
            }

            ips.Close();

            pairs.Sort();

            return pairs;
        }
    }
}
