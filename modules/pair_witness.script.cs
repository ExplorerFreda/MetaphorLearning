using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using System.Text.RegularExpressions;

public class PatternFilter
{
    public static Regex islikea = new Regex(@"\b(is|was|has been|be|being) like( (a|an))?\b");
    public static Regex isa = new Regex(@"\b(is|was|has been|be|being)( (a|an))?\b");
    public static Regex isasadjasa = new Regex(@"\b(is|was|has been|be|being) as (\w)+ as( (a|an))?\b");
}

/// <summary>
///  
/// </summary>
public class NGramGenerationReducer : Reducer
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
        return new Schema("Ngram:string, Partition_ID:int");
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
        int order = int.Parse(args[0]);

        HashSet<string> dict = new HashSet<string>();
        int count = 0;

        foreach (Row row in input.Rows)
        {
            if (++count == 1)
            {
                row["Partition_ID"].CopyTo(output["Partition_ID"]);
            }

            string[] tks = row["Sentence"].String.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (tks.Length >= 64) continue;

            for (int i = 0; i < tks.Length; ++i)
            {
                StringBuilder gram = new StringBuilder(tks[i]);
                dict.Add(gram.ToString());

                for (int j = 1; j < order && i + j < tks.Length; ++j)
                {
                    gram.Append(" ");
                    gram.Append(tks[i + j]);
                    dict.Add(gram.ToString());
                }
            }
        }

        foreach (string gram in dict)
        {
            output["Ngram"].Set(gram);

            yield return output;
        }
    }
}

/// <summary>
/// 
/// </summary>
public class PairWitnessCombiner : Combiner
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
        return new Schema("Left:string, Right:string, Mention:string");
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
        int order = int.Parse(args[0]);

        Dictionary<string, List<string>> lookup = new Dictionary<string, List<string>>();

        foreach (Row row in right.Rows)
        {
            if (lookup.ContainsKey(row["Left"].String) == false)
            {
                lookup[row["Left"].String] = new List<string>();
            }

            lookup[row["Left"].String].Add(row["Right"].String);
        }

        foreach (Row row in left.Rows)
        {
            string[] tks = row["Sentence"].String.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (tks.Length >= 64) continue;

            HashSet<string> ngrams = new HashSet<string>();

            for (int i = 0; i < tks.Length; ++i)
            {
                StringBuilder gram = new StringBuilder(tks[i]);
                ngrams.Add(gram.ToString());

                for (int j = 1; j < order && i + j < tks.Length; ++j)
                {
                    gram.Append(" ");
                    gram.Append(tks[i + j]);
                    ngrams.Add(gram.ToString());
                }
            }

            foreach (string g in ngrams)
            {
                if (lookup.ContainsKey(g) == true)
                {
                    foreach (string s in lookup[g])
                    {
                        if (ngrams.Contains(s) == true)
                        {
                            output["Left"].Set(g);
                            output["Right"].Set(s);
                            output["Mention"].Set(row["Sentence"].String);

                            yield return output;
                        }
                    }
                }
            }
        }
    }
}
