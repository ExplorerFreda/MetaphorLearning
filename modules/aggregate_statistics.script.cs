using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

/// <summary>
///  
/// </summary>
public class SumReducer : Reducer
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

        foreach (var col in input.Columns)
        {
            if (string.Compare(col.Name, "Partition_ID") == 0) continue;

            output.Add(col);
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
    public override IEnumerable<Row> Reduce(RowSet input, Row output, string[] args)
    {
        Dictionary<string, int> aggregated_stat = new Dictionary<string, int>();
        List<string> stat_schema = new List<string>();
        int count = 0;

        foreach (Row row in input.Rows)
        {
            if (++count == 1)
            {
                foreach (var col in row.Schema.Columns)
                {
                    if (string.Compare(col.Name, "Partition_ID") == 0)
                    {
                        continue;
                    }

                    stat_schema.Add(col.Name);
                }

                foreach (string col_name in stat_schema)
                {
                    aggregated_stat.Add(col_name, int.Parse(row[col_name].String));
                }
                continue;
            }

            foreach (string col_name in stat_schema)
            {
                aggregated_stat[col_name] += int.Parse(row[col_name].String);
            }
        }

        foreach (var kv in aggregated_stat)
        {
            output[kv.Key].Set(kv.Value);
        }
        yield return output;
    }
}
