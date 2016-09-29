using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

/// <summary>
///  
/// </summary>
public class NPDeduplicator : Reducer
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

        ColumnInfo ci = input[input["PartitionID"]].Clone();
        ci.Source = input[input["PartitionID"]];
        output.Add(ci);

        output.Add(new ColumnInfo("NP", typeof(string)));

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
        HashSet<string> nps = new HashSet<string>();
        int count = 0;

        foreach (Row row in input.Rows)
        {
            if (++count == 1)
            {
                output["PartitionID"].Set(int.Parse(row["PartitionID"].String));
            }

            if (string.IsNullOrEmpty(row["Left"].String) == false)
            {
                nps.Add(row["Left"].String);
            }
            if (string.IsNullOrEmpty(row["Right"].String) == false)
            {
                nps.Add(row["Right"].String);
            }
            if (string.IsNullOrEmpty(row["LeftHead"].String) == false)
            {
                nps.Add(row["LeftHead"].String);
            }
            if (string.IsNullOrEmpty(row["RightHead"].String) == false)
            {
                nps.Add(row["RightHead"].String);
            }
        }

        foreach (var e in nps)
        {
            output["NP"].Set(e);
            yield return output;
        }
    }
}

/// <summary>
/// 
/// </summary>
public class StringMatchingLinker : Combiner
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
        Schema output = leftSchema.CloneWithSource();

        output.Add(new ColumnInfo("LeftNode", typeof(string)));
        output.Add(new ColumnInfo("RightNode", typeof(string)));

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
        Dictionary<string, List<string>> n2es = new Dictionary<string, List<string>>();

        foreach (Row row in right.Rows)
        {
            string name = row["Name"].String;

            if (n2es.ContainsKey(name) == false)
            {
                n2es.Add(name, new List<string>());
            }

            n2es[name].Add(row["_E"].String);
        }

        foreach (Row row in left.Rows)
        {
            string left_node = null, right_node = null;

            if (n2es.ContainsKey(row["Left"].String) == true)
            {
                left_node = row["Left"].String;
            }
            else
            {
                string simplified_left = StringNormalization.StringNormalization.MediateNormalization(row["Left"].String);

                if (n2es.ContainsKey(simplified_left) == true)
                {
                    left_node = simplified_left;
                }
                else
                {
                    if (n2es.ContainsKey(row["LeftHead"].String) == true)
                    {
                        left_node = row["LeftHead"].String;
                    }
                }
            }

            if (n2es.ContainsKey(row["Right"].String) == true)
            {
                right_node = row["Right"].String;
            }
            else
            {
                string simplified_right = StringNormalization.StringNormalization.MediateNormalization(row["Right"].String);

                if (n2es.ContainsKey(simplified_right) == true)
                {
                    right_node = simplified_right;
                }
                else
                {
                    if (n2es.ContainsKey(row["RightHead"].String) == true)
                    {
                        right_node = row["RightHead"].String;
                    }
                }
            }

            if (string.IsNullOrEmpty(left_node) == false &&
                string.IsNullOrEmpty(right_node) == false &&
                string.Compare(left_node, right_node) != 0)
            {
                row.CopyTo(output);

                foreach (var ln in n2es[left_node])
                {
                    output["LeftNode"].Set(ln);

                    foreach (var rn in n2es[right_node])
                    {
                        output["RightNode"].Set(rn);

                        yield return output;
                    }
                }
            }
        }
    }
}