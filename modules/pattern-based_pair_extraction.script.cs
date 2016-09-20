using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using System.Text.RegularExpressions;

using Newtonsoft.Json;

using opennlpinterface;

public class TargetSourceMappingSegmentationProcessor : Processor
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

        output.Add(new ColumnInfo("Left", typeof(string)));
        output.Add(new ColumnInfo("LeftStart", typeof(int)));
        output.Add(new ColumnInfo("LeftEnd", typeof(int)));
        output.Add(new ColumnInfo("Right", typeof(string)));
        output.Add(new ColumnInfo("RightStart", typeof(int)));
        output.Add(new ColumnInfo("RightEnd", typeof(int)));

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
        string np = @"(DT ){0,1}(((RB|JJ|OF|OF DT|JJR|JJS|RBR|RBS|CD|NN|NNS|NNP|NNPS|VBG|VBN|,|CC) ){0,}(NN|NNS|NNP|NNPS)( CD)?)";
        
        Regex left_np_rg = new Regex(@"(?<=(^|IN |, ))\b" + np + @"$");
        Regex right_np_rg = new Regex(@"^" + np + @"\b" + @"(( (, ){0,1}((RB |JJR ){0,}(IN |TO |TO VB )|(RB |JJR ){0,}(IN |TO VB ){1,2}|(RB |RBR ){0,}(VBG |VBN |IN |TO |TO VB )(IN |TO ){0,2})" + np + @"){0,}(?=( \.){0,2}$))?");

        //followed by noun phrase
        Regex np_rg = new Regex(@"^ " + np + @"\b");
        //followed by verb phrase
        Regex vb_rg = new Regex(@"^ (VBZ|VB|VBD|VBT)\b");
        //followed by clause:
        Regex clause_rg = new Regex(@"^ (, ){0,1}(:|WDT|WP|WRB|CC VBZ|, CC VBZ)");

        foreach (Row row in input.Rows)
        {
            int keyword_start = int.Parse(row["PatternStart"].String);
            int keyword_end = int.Parse(row["PatternEnd"].String);

            string[] pos = JsonConvert.DeserializeObject<string[]>(row["POSTags"].String);

            StringBuilder query = new StringBuilder();

            for (int i = 0; i < keyword_start; ++i)
            {
                if (i > 0)
                {
                    query.Append(" ");
                }

                query.Append(pos[i]);
            }

            string prefix = query.ToString();

            var prefix_match = left_np_rg.Match(prefix);

            if (prefix_match.Success == false)
            {
                continue;
            }

            query.Clear();

            for (int i = keyword_end + 1; i < pos.Length; ++i)
            {
                if (i != keyword_end + 1)
                {
                    query.Append(" ");
                }

                query.Append(pos[i]);
            }

            string suffix = query.ToString();
            var suffix_match = right_np_rg.Match(suffix);

            if (suffix_match.Success == false)
            {
                continue;
            }

            //Rule_IsA_NoPureNounPhraseAfterConcept
            if (np_rg.IsMatch(suffix.Substring(suffix_match.Index + suffix_match.Length, suffix.Length - (suffix_match.Index + suffix_match.Length))) == true)
            {
                continue;
            }

            //Rule_IsA_NoVBAfterConcept
            if (vb_rg.IsMatch(suffix.Substring(suffix_match.Index + suffix_match.Length, suffix.Length - (suffix_match.Index + suffix_match.Length))) == true)
            {
                continue;
            }

            //Rule_IsA_ExtractConceptUsingRegexWithClause
            if (clause_rg.IsMatch(suffix.Substring(suffix_match.Index + suffix_match.Length, suffix.Length - (suffix_match.Index + suffix_match.Length)))==true)
            {
                continue;
            }

            //to fill the output row
            int[] tkss = JsonConvert.DeserializeObject<int[]>(row["TokenStarts"].String);
            int[] tkes = JsonConvert.DeserializeObject<int[]>(row["TokenEnds"].String);
            string s = row["Sentence"].String;
            string[] tks = new string[tkes.Length];

            for (int i = 0; i < tkss.Length; ++i)
            {
                tks[i] = s.Substring(tkss[i], tkes[i] - tkss[i]);
            }

            int left_start = 0;

            for (int i = 0; i < prefix_match.Index; ++i)
            {
                if (prefix[i] == ' ') left_start++;
            }

            if (left_start > 0 && string.Compare(pos[left_start - 1], "IN") == 0)
            {
                if (string.Compare(tks[left_start - 1], "that") != 0)
                {
                    continue;
                }
            }

            int left_end = keyword_start;    //open (i.e., "[x,y)")

            StringBuilder left_np = new StringBuilder(tks[left_start]);

            for (int l = left_start + 1; l < left_end; ++l)
            {
                left_np.Append(" ");
                left_np.Append(tks[l]);
            }

            int right_start = keyword_end + 1;
            int right_end = 1;

            for (int i = 0; i < suffix_match.Index + suffix_match.Length; ++i)
            {
                if (suffix[i] == ' ')
                {
                    right_end++;
                }
            }

            right_end += right_start;

            StringBuilder right_np = new StringBuilder(tks[right_start]);

            for (int l = right_start + 1; l < right_end && l < tks.Length; ++l)
            {
                right_np.Append(" ");
                right_np.Append(tks[l]);
            }

            output["Left"].Set(left_np.ToString());
            output["LeftStart"].Set(left_start);
            output["LeftEnd"].Set(left_end);
            output["Right"].Set(right_np.ToString());
            output["RightStart"].Set(right_start);
            output["RightEnd"].Set(right_end);

            foreach (var ci in row.Schema.Columns)
            {
                row[ci.Name].CopyTo(output[ci.Name]);
            }

            yield return output;
        }
    }
}