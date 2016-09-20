using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using System.Text.RegularExpressions;
using System.Threading;

using Newtonsoft.Json;

using opennlpinterface;

/// <summary>
/// input a sentence
/// output the sentence as well as matching results, if it matches a given pattern
/// </summary>
public class LiteralPatternProcessor : Processor
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

        HashSet<string> existedColumns = new HashSet<string>();

        foreach (var ci in input.Columns)
        {
            existedColumns.Add(ci.Name);
        }

        if (existedColumns.Contains("PatternStart") == false)
        {
            output.Add(new ColumnInfo("PatternStart", typeof(int)));
        }

        if (existedColumns.Contains("PatternEnd") == false)
        {
            output.Add(new ColumnInfo("PatternEnd", typeof(int)));
        }

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
        Regex rg = new Regex(args[0]);

        foreach (Row row in input.Rows)
        {
            string s = row["Sentence"].String;
            var m = rg.Match(s);

            if (m.Success == true)
            {
                int[] tkss = JsonConvert.DeserializeObject<int[]>(row["TokenStarts"].String);
                int[] tkes = JsonConvert.DeserializeObject<int[]>(row["TokenEnds"].String);

                int keyword_start = -1;
                for (int i = 0; i < tkss.Length; ++i)
                {
                    if (tkss[i] == m.Index)
                    {
                        keyword_start = i;
                        break;
                    }
                }

                if (keyword_start == -1) continue;

                int keyword_end = -1;    //closed (i.e., "]")

                for (int i = keyword_start; i < tkes.Length; ++i)
                {
                    if (tkes[i] == m.Index + m.Length)
                    {
                        keyword_end = i;
                        break;
                    }
                }

                if (keyword_end == -1) continue;

                row.CopyTo(output);
                output["PatternStart"].Set(keyword_start);
                output["PatternEnd"].Set(keyword_end);

                yield return output;
            }
        }
    }
}

/// <summary>
/// input a sentence
/// output the sentence as well as matching results, if it matches a given pattern
/// </summary>
public class TagPatternProcessor : Processor
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

        HashSet<string> existedColumns = new HashSet<string>();

        foreach (var ci in input.Columns)
        {
            existedColumns.Add(ci.Name);
        }

        if (existedColumns.Contains("PatternStart") == false)
        {
            output.Add(new ColumnInfo("PatternStart", typeof(int)));
        }

        if (existedColumns.Contains("PatternEnd") == false)
        {
            output.Add(new ColumnInfo("PatternEnd", typeof(int)));
        }

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
        Regex rg = new Regex(args[0]);
        int[] element_starts = new int[200];
        int[] element_ends = new int[200];

        foreach (Row row in input.Rows)
        {
            string s = row["Sentence"].String;
            int[] tkss = JsonConvert.DeserializeObject<int[]>(row["TokenStarts"].String);
            int[] tkes = JsonConvert.DeserializeObject<int[]>(row["TokenEnds"].String);
            string[] postags = JsonConvert.DeserializeObject<string[]>(row["POSTags"].String);
            StringBuilder query = new StringBuilder();

            for (int i = 0; i < postags.Length; ++i)
            {
                if (i != 0)
                {
                    query.Append(" ");
                }

                element_starts[i] = query.Length;
                query.Append(s.Substring(tkss[i], tkes[i] - tkss[i]));
                query.Append("|");
                query.Append(postags[i]);
                element_ends[i] = query.Length;
            }

            string q = query.ToString();
            var m = rg.Match(q);

            if (m.Success == true)
            {
                int keyword_start = -1;
                for (int i = 0; i < postags.Length; ++i)
                {
                    if (element_starts[i] == m.Index)
                    {
                        keyword_start = i;
                        break;
                    }
                }

                int keyword_end = -1;    //closed (i.e., "]")
                for (int i = keyword_start + 1; i < postags.Length; ++i)
                {
                    if (element_ends[i] == m.Index + m.Length)
                    {
                        keyword_end = i;
                        break;
                    }
                }

                if (keyword_start == -1 || keyword_end == -1) continue;

                row.CopyTo(output);
                output["PatternStart"].Set(keyword_start);
                output["PatternEnd"].Set(keyword_end);

                yield return output;
            }
        }
    }
}
