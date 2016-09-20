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
public class TokenizationProcessor : Processor
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

        output.Add(new ColumnInfo("TokenStarts", typeof(string)));
        output.Add(new ColumnInfo("TokenEnds", typeof(string)));

        return output;
    }

    int min_tk_num = 0;
    int max_tk_num = 0;
    NLPToolBox nlp_models = null;
    opennlp.tools.util.Span[] tkpos = null;
    Thread worker = null;
    bool valid = false;
    int[] tkss = null;
    int[] tkes = null;

    public void TaskExecutor(object sent)
    {
        string s = (string)sent;

        tkpos = nlp_models.sentenceTokenizer.tokenizePos(s);

        if (tkpos != null && tkpos.Length >= min_tk_num && tkpos.Length < max_tk_num)
        {
            tkss = new int[tkpos.Length];
            tkes = new int[tkpos.Length];

            for (int i = 0; i < tkpos.Length; ++i)
            {
                tkss[i] = tkpos[i].getStart();
                tkes[i] = tkpos[i].getEnd();
            }

            valid = true;
        }
        else
        {
            valid = false;
        }
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
        for (int i = 0; i < 13; i++)
        {
            args[i] = Path.GetFileName(args[i]);
        }

        nlp_models = new NLPToolBox(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12].Substring(0, args[12].Length - 4));
        min_tk_num = int.Parse(args[13]);
        max_tk_num = int.Parse(args[14]);

        int batch_id = int.Parse(args[args.Length - 1]);
        TimeSpan ts = TimeSpan.FromSeconds(8);

        foreach (Row row in input.Rows)
        {
            string s = row["Sentence"].String;

            int hash_nb = Math.Abs((s.GetHashCode() + s.Length) % 64);

            if (hash_nb != batch_id) continue;

            worker = new Thread(new ParameterizedThreadStart(TaskExecutor));
            worker.Start(s);

            if (!worker.Join(ts))
            {
                worker.Abort();
                valid = false;
                continue;
            }

            if (valid == true)
            {
                row.CopyTo(output);

                output["TokenStarts"].Set(JsonConvert.SerializeObject(tkss));
                output["TokenEnds"].Set(JsonConvert.SerializeObject(tkes));

                yield return output;
            }

            /* opennlp.tools.util.Span[] tkpos = nlp_models.sentenceTokenizer.tokenizePos(s);

            if (tkpos != null && tkpos.Length >= min_tk_num && tkpos.Length < max_tk_num)
            {
                string[] tks = new string[tkpos.Length];
                int[] tkss = new int[tkpos.Length];
                int[] tkes = new int[tkpos.Length];

                for (int i = 0; i < tkpos.Length; ++i)
                {
                    tks[i] = s.Substring(tkpos[i].getStart(), tkpos[i].getEnd() - tkpos[i].getStart());
                    tkss[i] = tkpos[i].getStart();
                    tkes[i] = tkpos[i].getEnd();
                }

                if (oo >= 1)
                {
                    string[] pos = nlp_models.tagger.tag(tks);
                    output["POSTags"].Set(JsonConvert.SerializeObject(pos));
                }

                row[0].CopyTo(output[0]);
                output["TokenStarts"].Set(JsonConvert.SerializeObject(tkss));
                output["TokenEnds"].Set(JsonConvert.SerializeObject(tkes));

                yield return output;
            } */
        }
    }
}