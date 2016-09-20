using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using System.Threading;

using Newtonsoft.Json;

using opennlpinterface;

/// <summary>
/// input a paragraph/doc
/// output sentences
/// </summary>
public class POSTaggingProcessor : Processor
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

        output.Add(new ColumnInfo("POSTags", typeof(string)));

        return output;
    }

    /* public void TaskExecutor(object sent)
    {
        string s = (string)sent;
        tkpos = nlp_models.sentenceTokenizer.tokenizePos(s);
        if (tkpos != null && tkpos.Length >= min_tk_num && tkpos.Length < max_tk_num)
        {
            tks = new string[tkpos.Length];
            tkss = new int[tkpos.Length];
            tkes = new int[tkpos.Length];

            for (int i = 0; i < tkpos.Length; ++i)
            {
                tks[i] = s.Substring(tkpos[i].getStart(), tkpos[i].getEnd() - tkpos[i].getStart());
                tkss[i] = tkpos[i].getStart();
                tkes[i] = tkpos[i].getEnd();
            }

            if (oo >= 1)
            {
                pos = nlp_models.tagger.tag(tks);
            }

            valid = true;
        }
        else
        {
            valid = false;
        }
    } */

    /// <summary>
    ///
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="args"></param>
    /// <returns></returns>    
    public override IEnumerable<Row> Process(RowSet input, Row output, string[] args)
    {
        for (int i = 0; i < 13; i++)
        {
            args[i] = Path.GetFileName(args[i]);
        }

        NLPToolBox nlp_models = new NLPToolBox(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12].Substring(0, args[12].Length - 4));

        foreach (Row row in input.Rows)
        {
            string s = row["Sentence"].String;

            int[] tkss = JsonConvert.DeserializeObject<int[]>(row["TokenStarts"].String);
            int[] tkes = JsonConvert.DeserializeObject<int[]>(row["TokenEnds"].String);

            if (tkss.Length != tkes.Length) continue;

            string[] tks = new string[tkss.Length];

            for (int i = 0; i < tks.Length; ++i)
            {
                tks[i] = s.Substring(tkss[i], tkes[i] - tkss[i]);
            }

            row.CopyTo(output);

            string[] pos = nlp_models.tagger.tag(tks);

            output["POSTags"].Set(JsonConvert.SerializeObject(pos));

            yield return output;
        }
    }
}