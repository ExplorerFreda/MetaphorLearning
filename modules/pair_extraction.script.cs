using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using System.Text.RegularExpressions;
using locatenamesegment;
using opennlp;
using Newtonsoft.Json;


/// <summary>
/// input a sentence
/// output left-hand side noun phrase and right-hand side noun phrase
/// </summary>
public class SegmentationProcessor : Processor
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
        return new Schema("Left:string, LeftStart:int, LeftEnd:int, Right:string, RightStart:int, RightEnd:int, Tokens:string");
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
        string maltparsemodel_name = Path.GetFileName(args[12]);
        maltparsemodel_name = maltparsemodel_name.Substring(0, maltparsemodel_name.Length - 4);
        Locator nlp_models = new Locator(Path.GetFileName(args[0]), Path.GetFileName(args[1]), Path.GetFileName(args[2]), Path.GetFileName(args[3]), Path.GetFileName(args[4]), Path.GetFileName(args[5]), Path.GetFileName(args[6]), Path.GetFileName(args[7]), Path.GetFileName(args[8]), Path.GetFileName(args[9]), Path.GetFileName(args[10]), Path.GetFileName(args[11]), maltparsemodel_name);

        Regex keyword_rg = new Regex(args[13]);
        string np = @"(DT ){0,1}(((RB|JJ|OF|OF DT|JJR|JJS|RBR|RBS|CD|NN|NNS|NNP|NNPS|VBG|VBN|,|CC) ){0,}(NN|NNS|NNP|NNPS)( CD)?)";
        Regex left_np_rg = new Regex("(?<=(^|IN |, ))" + np + "$");
        Regex right_np_rg = new Regex("^" + np + "(?!(( (VBZ|VB|VBD|VBT))|( (, ){0,1}(WDT|WP|WRB|CC VBZ))))");
        Regex right_np_postmodifier_rg = new Regex("^" + np + "( (, ){0,1}((RB |JJR ){0,}(IN |TO |TO VB )|(RB |JJR ){0,}(IN |TO VB ){1,2}|(RB |RBR ){0,}(VBG |VBN |IN |TO |TO VB )(IN |TO ){0,2})" + np + @"){0,}(?=(( \.)?$))");

        foreach (Row row in input.Rows)
        {
            string st = row["Sentence"].String;
            var keyword_match = keyword_rg.Match(st);

            if (keyword_match.Success == true)
            {
                opennlp.tools.util.Span[] tkpos = nlp_models.sentenceTokenizer.tokenizePos(st);

                if (tkpos == null) continue;

                int keyword_start = -1;
                for (int i = 0; i < tkpos.Length; ++i)
                {
                    if (tkpos[i].getStart() == keyword_match.Index)
                    {
                        keyword_start = i;
                        break;
                    }
                }

                int keyword_end = -1;    //closed (i.e., "]")
                for (int i = keyword_start + 1; i < tkpos.Length; ++i)
                {
                    if (tkpos[i].getEnd() == keyword_match.Index + keyword_match.Length)
                    {
                        keyword_end = i;
                        break;
                    }
                }

                if (keyword_start == -1 || keyword_end == -1) continue;

                string[] tks = new string[tkpos.Length];

                for (int i = 0; i < tkpos.Length; ++i)
                {
                    tks[i] = st.Substring(tkpos[i].getStart(), tkpos[i].getEnd() - tkpos[i].getStart());
                }

                string[] pos = nlp_models.tagger.tag(tks);

                StringBuilder query = new StringBuilder();

                for (int i = 0; i < keyword_start; ++i)
                {
                    if (i > 0) query.Append(" ");

                    query.Append(pos[i]);
                }

                string prefix = query.ToString();
                var prefix_match = left_np_rg.Match(prefix);

                if (prefix_match.Success == false) continue;

                query.Clear();
                for (int i = keyword_end + 1; i < pos.Length; ++i)
                {
                    if (i != keyword_end + 1) query.Append(" ");

                    query.Append(pos[i]);
                }

                string suffix = query.ToString();
                var suffix_match = right_np_rg.Match(suffix);

                if (suffix_match.Success == false) continue;

                var suffix_postmodifier_match = right_np_postmodifier_rg.Match(suffix);
                if (suffix_postmodifier_match.Success == true) suffix_match = suffix_postmodifier_match;

                //to fill the output row
                int left_start = 0;

                for (int i = 0; i < prefix_match.Index; ++i)
                {
                    if (prefix[i] == ' ') left_start++;
                }

                if (left_start > 0 && string.Compare(pos[left_start - 1], "IN") == 0)
                {
                    if (string.Compare(tks[left_start - 1], "that") != 0) continue;
                }

                int left_end = keyword_start;    //open (i.e., ")")

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
                    if (suffix[i] == ' ') right_end++;
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
                output["Tokens"].Set(JsonConvert.SerializeObject(tks));
                yield return output;
            }
        }
    }
}
