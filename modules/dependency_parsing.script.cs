using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using Newtonsoft.Json;

using opennlpinterface;



/// <summary>
/// 
/// </summary>
public class ParsingProcessor : Processor
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="columns"></param>
    /// <param name="args"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Schema Produces(string[] columns, string[] args, Schema input)
    {
        Schema output = input.CloneWithSource();

        output.Add(new ColumnInfo("Heads", typeof(string)));
        output.Add(new ColumnInfo("Relations", typeof(string)));

        return output;
    }
    /// <summary>
    ///
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="args"></param>
    /// <returns></returns>    
    public override IEnumerable<Row> Process(RowSet input, Row output, string[] args)
    {
        string maltparsemodel_name = Path.GetFileName(args[12]);
        maltparsemodel_name = maltparsemodel_name.Substring(0, maltparsemodel_name.Length - 4);
        NLPToolBox nlp_models = new NLPToolBox(Path.GetFileName(args[0]), Path.GetFileName(args[1]), Path.GetFileName(args[2]), Path.GetFileName(args[3]), Path.GetFileName(args[4]), Path.GetFileName(args[5]), Path.GetFileName(args[6]), Path.GetFileName(args[7]), Path.GetFileName(args[8]), Path.GetFileName(args[9]), Path.GetFileName(args[10]), Path.GetFileName(args[11]), maltparsemodel_name);

        foreach (Row row in input.Rows)
        {
            row.CopyTo(output);

            string s = row["Sentence"].String;
            int[] tkss = JsonConvert.DeserializeObject<int[]>(row["TokenStarts"].String);
            int[] tkes = JsonConvert.DeserializeObject<int[]>(row["TokenEnds"].String);
            string[] tks = new string[tkss.Length];

            for (int i = 0; i < tkss.Length; ++i)
            {
                tks[i] = s.Substring(tkss[i], tkes[i] - tkss[i]);
            }

            string[] postags = JsonConvert.DeserializeObject<string[]>(row["POSTags"].String);

            string[] conlls = new string[tks.Length];

            for (int i = 0; i < conlls.Length; i++)
            {
                conlls[i] = (i + 1).ToString() + "\t" + (tks[i] == "" ? "_" : tks[i]) + "\t_\t" + postags[i] + "\t" + postags[i] + "\t_";
            }

            string[] parsed = nlp_models.maltparser.parseTokens(conlls);
            int[] hs = new int[parsed.Length];
            string[] rels = new string[parsed.Length];

            for (int i = 0; i < parsed.Length; ++i)
            {
                string[] parts = parsed[i].Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                hs[i] = int.Parse(parts[parts.Length - 2]);
                rels[i] = parts[parts.Length - 1];
            }

            output["Heads"].Set(JsonConvert.SerializeObject(hs));
            output["Relations"].Set(JsonConvert.SerializeObject(rels));

            yield return output;
        }
    }
}
