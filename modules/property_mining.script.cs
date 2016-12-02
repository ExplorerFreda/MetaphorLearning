using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using Newtonsoft.Json;

/// <summary>
/// 
/// </summary>
public class PropertyExtractor : Processor
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

        output.Add(new ColumnInfo("Property", typeof(string)));

        return output;
    }
    // <summary>
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
            string s = row["Sentence"].String;
            int[] tkss = JsonConvert.DeserializeObject<int[]>(row["TokenStarts"].String);
            int[] tkes = JsonConvert.DeserializeObject<int[]>(row["TokenEnds"].String);
            string[] tokens = new string[tkss.Length];

            for (int i = 0; i < tkss.Length; i++)
            {
                tokens[i] = s.Substring(tkss[i], tkes[i] - tkss[i]);
            }

            int pstart = int.Parse(row["PatternStart"].String);
            int pend = int.Parse(row["PatternEnd"].String);

            for (int j = pstart; j < pend; j++)
            {
                if (string.Compare(tokens[j], "as") == 0)
                {
                    row.CopyTo(output);
                    output["Property"].Set(tokens[j + 1]);
                    yield return output;
                    break;
                }
            }
        }
    }
}

/// <summary>
///  
/// </summary>
public class PropertyConcatenator : Reducer
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

        ColumnInfo ci = input[input["LeftHead"]].Clone();
        ci.Source = input[input["LeftHead"]];
        output.Add(ci);

        ci = input[input["RightHead"]].Clone();
        ci.Source = input[input["RightHead"]];
        output.Add(ci);

        output.Add(new ColumnInfo("PropertyRankingList", typeof(string)));

        return output; 
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public override IEnumerable<Row> Reduce(RowSet input, Row output, string[] args)
    {
        
        int count = 0;
        StringBuilder str = new StringBuilder();
        foreach (Row row in input.Rows)
        {
            str.Append(row["Property"].String);
            str.Append(":");
            str.Append(row["Frequency"].String);
            str.Append(";");

            if (++count == 1)
            {
                row["LeftHead"].CopyTo(output["LeftHead"]);
                row["RightHead"].CopyTo(output["RightHead"]);
            }
        }

        output["PropertyRankingList"].Set(str.ToString());
        yield return output;
    }
}
