using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using Newtonsoft.Json;

using StringNormalization;

/// <summary>
/// 
/// </summary>
public class CopyProcessor : Processor
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

        output.Add(new ColumnInfo("Partition_ID", typeof(int)));

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

        foreach (Row row in input.Rows)
        {
            row.CopyTo(output);

            for (int i = 0; i < 2000; i++)
            {
                output["Partition_ID"].Set(i);

                yield return output;
            }
        }
    }
}

/// <summary>
/// 
/// </summary>
public class Alignment : Combiner
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="columns"></param>
    /// <param name="args"></param>
    /// <param name="leftSchema"></param>
    /// <param name="leftTable"></param>
    /// <param name="rightSchema"></param>
    /// <param name="rightTable"></param>
    /// <returns></returns>
    public override Schema Produces(string[] columns, string[] args, Schema leftSchema, string leftTable, Schema rightSchema, string rightTable)
    {
        Schema output = new ScopeRuntime.Schema();


        output.Add(new ColumnInfo("LeftHead", typeof(string)));
        output.Add(new ColumnInfo("RightHead", typeof(string)));

        foreach (var ci in leftSchema.Columns)
        {
            ColumnInfo oci = ci.Clone();
            oci.Source = leftSchema[leftSchema[ci.Name]];
            output.Add(oci);
        }

        return output;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="output"></param>
    /// <param name="args"></param>
    /// <returns></returns>   
    public override IEnumerable<Row> Combine(RowSet left, RowSet right, Row output, string[] args)
    {
        HashSet<string> vocabulary = new HashSet<string>();
        Dictionary<string, List<string>> pairs = new Dictionary<string, List<string>>();

        foreach (Row row in right.Rows)
        {
            string l = row["LeftHead"].String;
            string r = row["RightHead"].String;

            vocabulary.Add(l);
            vocabulary.Add(r);

            if (pairs.ContainsKey(l) == false)
            {
                pairs.Add(l, new List<string>());
            }

            pairs[l].Add(r);
        }

        int n = int.Parse(args[0]);

        foreach (Row row in left.Rows)
        {
            string s = row["Sentence"].String;
            int[] tkss = JsonConvert.DeserializeObject<int[]>(row["TokenStarts"].String);
            int[] tkes = JsonConvert.DeserializeObject<int[]>(row["TokenEnds"].String);

            string[] tokens = new string[tkss.Length];

            for (int i = 0; i < tkss.Length; i++)
            {
                tokens[i] = s.Substring(tkss[i], tkes[i] - tkss[i]);
            }

            HashSet<string> gs = new HashSet<string>();

            for (int i = 0; i < tokens.Length; i++)
            {
                StringBuilder phr = new StringBuilder(tokens[i]);

                gs.Add(StringNormalization.StringNormalization.MediateNormalization(phr.ToString()));

                for (int j = 1; j < n && i + j < tokens.Length; j++)
                {
                    phr.Append(" ");
                    phr.Append(tokens[i + j]);

                    gs.Add(StringNormalization.StringNormalization.MediateNormalization(phr.ToString()));
                }
            }

            List<Tuple<string, string>> ms = new List<Tuple<string, string>>();

            foreach (string p in gs)
            {
                if (pairs.ContainsKey(p) == true)
                {
                    foreach (string ap in pairs[p])
                    {
                        if (gs.Contains(ap) == true)
                        {
                            ms.Add(new Tuple<string, string>(p, ap));
                        }
                    }
                }
            }

            /* if (ms.Count > 0)
            {
                row.CopyTo(output);
            } */

            foreach (var p in ms)
            {
                int lidx = s.IndexOf(p.Item1);
                int ridx = s.IndexOf(p.Item2);

                if (ridx >= lidx + p.Item1.Length || lidx >= ridx + p.Item2.Length)
                {
                    output["LeftHead"].Set(p.Item1);
                    output["RightHead"].Set(p.Item2);

                    foreach (var ci in row.Schema.Columns)
                    {
                        row[ci.Name].CopyTo(output[ci.Name]);
                    }

                    yield return output;
                }
            }
        }
    }
}
