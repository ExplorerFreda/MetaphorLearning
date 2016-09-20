using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using System.Text.RegularExpressions;

using Newtonsoft.Json;

using StringNormalization;

/// <summary>
/// 
/// </summary>
public class HeadIdentifierProcessor : Processor
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

        string[] considered_columns = args[0].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < considered_columns.Length; ++i)
        {
            considered_columns[i] = considered_columns[i].Trim();

            output.Add(new ColumnInfo(considered_columns[i] + "Head", typeof(string)));
            output.Add(new ColumnInfo(considered_columns[i] + "HeadIdentification", typeof(string)));
        }

        foreach (var ci in input.Columns)
        {
            ColumnInfo oci = ci.Clone();
            oci.Source = input[input[ci.Name]];
            output.Add(oci);
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
        string[] considered_columns = args[0].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < considered_columns.Length; ++i)
        {
            considered_columns[i] = considered_columns[i].Trim();
        }

        string modifier = @"\b(DT ){0,1}((RB|JJ|OF|OF DT|JJR|JJS|RBR|RBS|CD|NN|NNS|NNP|NNPS|VBG|VBN|,|CC) ){0,}(?=((NN|NNS|NNP|NNPS)\b))";
        Regex modifier_rg = new Regex(modifier);

        foreach (Row row in input.Rows)
        {
            string s = row["Sentence"].String;
            int[] tkss = JsonConvert.DeserializeObject<int[]>(row["TokenStarts"].String);
            int[] tkes = JsonConvert.DeserializeObject<int[]>(row["TokenEnds"].String);
            string[] postags = JsonConvert.DeserializeObject<string[]>(row["POSTags"].String);
            int[] hs = JsonConvert.DeserializeObject<int[]>(row["Heads"].String);
            string[] rels = JsonConvert.DeserializeObject<string[]>(row["Relations"].String);

            foreach (string cn in considered_columns)
            {
                int start_idx = int.Parse(row[cn + "Start"].String);
                int end_idx = int.Parse(row[cn + "End"].String);

                int who_has_out_of_range_head = -1;
                int num_out_of_range_head = 0;

                for (int i = start_idx; i < end_idx; ++i)
                {
                    if (hs[i] - 1 < start_idx || hs[i] - 1 >= end_idx)
                    {
                        who_has_out_of_range_head = i;
                        num_out_of_range_head++;
                    }
                }

                if (num_out_of_range_head == 1)
                {
                    int first_head_token = who_has_out_of_range_head;

                    for (int i = who_has_out_of_range_head - 1; i >= start_idx; --i)
                    {
                        if ((string.Compare("nn", rels[i]) == 0 || string.Compare("compound", rels[i]) == 0) &&
                            hs[i] == who_has_out_of_range_head + 1)
                        {
                            first_head_token = i;
                        }
                        else
                        {
                            break;
                        }
                    }

                    StringBuilder hp = new StringBuilder(s.Substring(tkss[first_head_token], tkes[who_has_out_of_range_head] - tkss[first_head_token]));

                    for (int i = who_has_out_of_range_head + 1; i < end_idx; ++i)
                    {
                        if (hs[i] == who_has_out_of_range_head + 1 &&
                            (string.Compare(rels[i], "cc") == 0 || string.Compare(rels[i], "punct") == 0))
                        {    //i-->the next "and"/"or"

                            for (int j = i + 1; j < end_idx; ++j)
                            {
                                if (hs[j] == who_has_out_of_range_head + 1 &&
                                    string.Compare(rels[j], "conj") == 0)
                                {    //j-->the head of next np
                                    hp.Append(" ");
                                    hp.Append(s.Substring(tkss[i], tkes[i] - tkss[i]));
                                    first_head_token = j;
                                    for (int k = j - 1; k > i; --k)
                                    {
                                        if (hs[k] == j + 1 &&
                                            (string.Compare(rels[k], "nn") == 0 || string.Compare("compound", rels[k]) == 0))
                                        {
                                            first_head_token = k;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    hp.Append(" ");
                                    hp.Append(s.Substring(tkss[first_head_token], tkes[j] - tkss[first_head_token]));
                                    i = j;
                                    break;
                                }
                            }
                        }
                    }

                    output[cn + "Head"].Set(Regex.Replace(StringNormalization.StringNormalization.MediateNormalization(hp.ToString()), @"( )+", " "));
                    output[cn + "HeadIdentification"].Set("DP");
                }
                else
                {
                    StringBuilder np = new StringBuilder(postags[start_idx]);

                    for (int i = start_idx + 1; i < end_idx; ++i)
                    {
                        np.Append(" ");
                        np.Append(postags[i]);
                    }

                    string query = np.ToString();

                    var match = modifier_rg.Match(query);

                    int htk_start_idx = 0;

                    for (int i = 0; i < match.Index + match.Length; ++i)
                    {
                        if (query[i] == ' ')
                        {
                            htk_start_idx++;
                        }
                    }
                    htk_start_idx += start_idx;

                    StringBuilder hp = new StringBuilder(s.Substring(tkss[htk_start_idx], tkes[htk_start_idx] - tkss[htk_start_idx]));

                    for (int i = htk_start_idx + 1; i < end_idx; ++i)
                    {
                        if (postags[i].StartsWith("N") == false)
                        {
                            break;
                        }
                        else
                        {
                            hp.Append(" ");
                            hp.Append(s.Substring(tkss[i], tkes[i] - tkss[i]));
                        }
                    }

                    output[cn + "Head"].Set(Regex.Replace(StringNormalization.StringNormalization.MediateNormalization(hp.ToString()), @"( )+", " "));
                    output[cn + "HeadIdentification"].Set("CP");
                }
            }

            foreach (var ci in row.Schema.Columns)
            {
                row[ci.Name].CopyTo(output[ci.Name]);
            }

            yield return output;
        }
    }
}
