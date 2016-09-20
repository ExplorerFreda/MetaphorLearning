using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;


/// <summary>
/// 
/// </summary>
public class SortProcessor : Processor
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
        return input.CloneWithSource();
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
            row["Left"].CopyTo(output["Left"]);

            string[] raw_rs = row["Rights"].String.Split(new string[] { ";sp;" }, StringSplitOptions.RemoveEmptyEntries);
            List<Tuple<string, int>> rs = new List<Tuple<string, int>>();
            foreach (string raw_tp in raw_rs)
            {
                string[] tp = raw_tp.Split(new string[] { ":sp:" }, StringSplitOptions.RemoveEmptyEntries);

                if (tp.Length != 2) continue;

                rs.Add(new Tuple<string, int>(tp[0], int.Parse(tp[1])));
            }
            rs.Sort((x, y) => { if (x.Item2 < y.Item2)return 1; else if (x.Item2 > y.Item2)return -1; else return 0; });
            StringBuilder rs_str = new StringBuilder();
            foreach (var tp in rs)
            {
                rs_str.Append(tp.Item1);
                rs_str.Append(":");
                rs_str.Append(tp.Item2.ToString());
                rs_str.Append(";");
            }
            output["Rights"].Set(rs_str.ToString());

            yield return output;
        }
    }
}
