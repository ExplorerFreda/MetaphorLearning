using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using System.Net;

using Newtonsoft.Json;
using MurmurHash;

public class Anchor
{
    public int Start
    {
        get;
        set;
    }
    public int Len
    {
        get;
        set;
    }
    public string Surface
    {
        get;
        set;
    }
    public string Tag
    {
        get;
        set;
    }
}

/// <summary>
/// 
/// </summary>
public class CleanWikipediaProcessor : Processor
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
            row["Url"].CopyTo(output["Url"]);
            row["Text"].CopyTo(output["Text"]);
            string[] categories = JsonConvert.DeserializeObject<string[]>(row["CategoriesJson"].String);
            for (int i = 0; i < categories.Length; ++i)
            {
                categories[i] = WebUtility.UrlDecode(categories[i]);
            }
            output["CategoriesJson"].Set(JsonConvert.SerializeObject(categories));
            List<Anchor> anchors = JsonConvert.DeserializeObject<List<Anchor>>(row["AnchorsJson"].String);
            for (int i = 0; i < anchors.Count; ++i)
            {
                anchors[i].Tag = WebUtility.UrlDecode(anchors[i].Tag);
            }
            output["AnchorsJson"].Set(JsonConvert.SerializeObject(anchors));
            yield return output;
        }
    }
}

/// <summary>
/// 
/// </summary>
public class CategoryExpansionProcessor : Processor
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

        ColumnInfo ci = input[input["Url"]].Clone();
        ci.Source = input[input["Url"]];
        output.Add(ci);
        output.Add(new ColumnInfo("Category", typeof(string)));

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
            row["Url"].CopyTo(output["Url"]);

            string[] categories = JsonConvert.DeserializeObject<string[]>(row["CategoriesJson"].String);

            foreach (string c in categories)
            {
                output["Category"].Set(c);

                yield return output;
            }
        }
    }
}


/// <summary>
/// 
/// </summary>
public class FormatTextProcessor : Processor
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
        return new Schema("Sentence:string, TokenStarts:string, TokenEnds:string, POSTags:string");
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
            string[] tks = JsonConvert.DeserializeObject<string[]>(row["Tokens"].String);
            StringBuilder sent = new StringBuilder();
            int[] tkss = new int[tks.Length];
            int[] tkes = new int[tks.Length];

            for (int i = 0; i < tks.Length; ++i)
            {
                if (i != 0)
                {
                    sent.Append(" ");
                }

                tkss[i] = sent.Length;
                sent.Append(tks[i]);
                tkes[i] = sent.Length;
            }

            output["Sentence"].Set(sent.ToString());
            output["TokenStarts"].Set(JsonConvert.SerializeObject(tkss));
            output["TokenEnds"].Set(JsonConvert.SerializeObject(tkes));
            output["POSTags"].CopyTo(output["POSTags"]);

            yield return output;
        }
    }
}


/// <summary>
/// 
/// </summary>
public class HashProcessor : Processor
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
        ColumnInfo ci = input[input["Sentence"]].Clone();
        ci.Source = input[input["Sentence"]];
        output.Add(ci);
        output.Add(new ColumnInfo("H0", typeof(int)));
        output.Add(new ColumnInfo("H1", typeof(int)));
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
        int ub = int.Parse(args[0]);

        foreach (Row row in input.Rows)
        {
            row[0].CopyTo(output[0]);
            output[1].Set(Math.Abs(Murmur3.UniformHash(row[0].String) % ub));
            output[2].Set(Math.Abs((Murmur3.UniformHash(row[0].String) + row[0].String.Length) % ub));
            yield return output;
        }
    }
}

