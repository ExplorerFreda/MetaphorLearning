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
            row["Instance"].CopyTo(output["Instance"]);
            string raw_blc = row["BLC"].String;
            string[] raw_cs = raw_blc.Split(new string[] { ";sp;" }, StringSplitOptions.RemoveEmptyEntries);
            List<Tuple<string, float>> cs = new List<Tuple<string, float>>();
            foreach (var raw_tp in raw_cs)
            {
                string[] tp = raw_tp.Split(new string[] { ":sp:" }, StringSplitOptions.RemoveEmptyEntries);

                if (tp.Length != 2) continue;

                cs.Add(new Tuple<string, float>(tp[0], (float)double.Parse(tp[1])));
            }
            cs.Sort((x, y) => { if (x.Item2 < y.Item2)return 1; else if (x.Item2 > y.Item2) return -1; else return 0; });
            StringBuilder blc = new StringBuilder();
            foreach (var tp in cs)
            {
                blc.Append(tp.Item1);
                blc.Append(":sp:");
                blc.Append(tp.Item2.ToString());
                blc.Append(";sp;");
            }
            output["BLC"].Set(blc.ToString());

            yield return output;
        }
    }
}
