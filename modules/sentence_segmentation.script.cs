using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;

using opennlpinterface;

/// <summary>
/// input a paragraph/doc
/// output sentences
/// </summary>
public class SentenceSegmentationProcessor : Processor
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
        Schema output = new Schema();
        ColumnInfo c0 = new ColumnInfo(columns[0], ColumnDataType.String);
        output.Add(c0);
        return output;
    }

    NLPToolBox nlp_models = null;
    Thread worker = null;
    opennlp.tools.util.Span[] ss = null;

    public void TaskExecutor(object para)
    {
        ss = nlp_models.sentenceDetector.sentPosDetect((string)para);
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
        nlp_models = new NLPToolBox(Path.GetFileName(args[0]), Path.GetFileName(args[1]), Path.GetFileName(args[2]), Path.GetFileName(args[3]), Path.GetFileName(args[4]), Path.GetFileName(args[5]), Path.GetFileName(args[6]), Path.GetFileName(args[7]), Path.GetFileName(args[8]), Path.GetFileName(args[9]), Path.GetFileName(args[10]), Path.GetFileName(args[11]), maltparsemodel_name);

        foreach (Row row in input.Rows)
        {
            string original_text = row["Text"].String.Trim();
            original_text = original_text.Replace("--.", ". ");
            original_text = original_text.Replace("#R#", " ");
            original_text = original_text.Replace("#TAB#", " ");
            original_text = Regex.Replace(original_text, @"( )+", " ");

            worker = new Thread(new ParameterizedThreadStart(TaskExecutor));
            worker.Start(original_text);
            if (!worker.Join(new TimeSpan(0, 0, 8)))
            {
                worker.Abort();
                continue;
            }

            for (int i = 0; i < ss.Length; i++)
            {
                output["Sentence"].Set(original_text.Substring(ss[i].getStart(), ss[i].getEnd() - ss[i].getStart()).Trim());
                yield return output;
            }
        }
    }
}

/// <summary>
/// 
/// </summary>
public class Filterer : Processor
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
        Schema output = new Schema();
        ColumnInfo c0 = new ColumnInfo(columns[0], ColumnDataType.String);
        c0.Source = input[0];
        output.Add(c0);
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
        float lcl_ratio = float.Parse(args[0]);

        foreach (Row row in input.Rows)
        {
            string s = row[0].String;

            //strip if the sentence contains too many non-lowercase_letter chars
            int num_lcl = 0;
            int num_tk_seperator = 0;
            int num_cptk = 0;
            for (int i = 0; i < s.Length; ++i)
            {
                if (Char.IsLower(s, i) == true)
                {
                    num_lcl++;
                }

                if (s[i] == ' ')
                {
                    num_tk_seperator++;

                    if (i + 1 < s.Length && Char.IsUpper(s, i + 1) == true)
                    {
                        num_cptk++;
                    }
                }
            }
            if ((float)num_lcl / (float)s.Length < lcl_ratio) continue;
            if ((float)num_cptk / (float)num_tk_seperator >= lcl_ratio) continue;

            row[0].CopyTo(output[0]);
            yield return output;
        }
    }
}