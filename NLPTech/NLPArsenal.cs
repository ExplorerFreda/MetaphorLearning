using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

using opennlpinterface;

namespace NLPTech
{
    public class NLPArsenal
    {
        NLPToolBox player = null;

        public NLPArsenal(string[] model_paths)
        {
            player = new NLPToolBox(model_paths[0], model_paths[1], model_paths[2], model_paths[3], model_paths[4], model_paths[5], model_paths[6], model_paths[7], model_paths[8], model_paths[9], model_paths[10], model_paths[11], model_paths[12].Substring(0, model_paths[12].Length - 4));
        }

        public opennlp.tools.util.Span[] SentenceSegmentation(string text)
        {
            return player.sentenceDetector.sentPosDetect(text);
        }

        public opennlp.tools.util.Span[] Tokenization(string s)
        {
            return player.sentenceTokenizer.tokenizePos(s);
        }

        public string[] PartOfSpeechTagging(string[] tks)
        {
            return player.tagger.tag(tks);
        }

        public opennlp.tools.util.Span[] PersonDetection(string[] tks)
        {
            return player.persontagger.find(tks);
        }

        public opennlp.tools.util.Span[] OrganizationDetection(string[] tks)
        {
            return player.orgtagger.find(tks);
        }

        public opennlp.tools.util.Span[] LocationDetection(string[] tks)
        {
            return player.loctagger.find(tks);
        }

        public string[] Chunking(string[] tks, string[] postags)
        {
            return player.chunker.chunk(tks, postags);
        }

        /*
        CoNLL format input:
         * tab seperated 6 columns
            * index (not zero-based)
            * token
            * _
            * POS tag (Penn Treebank POS tagset)
            * POS tag (must be exactly same with the 4th column)
            * _
        
        CoNLL format output:
         * tab seperated columns
         * get non zero-based index of parent node and,
         * the dependency
         * as follow:
        int[] parents = new int[conlls.Length];
        string[] deptypes = new string[conlls.Length];
        for (int i = 0; i < conlls.Length; i++)
        {
            string[] eles = conlls[i].Split(seperator_tab, StringSplitOptions.RemoveEmptyEntries);
            parents[i] = int.Parse(eles[6]);
            deptypes[i] = eles[7];
        }
        */
        public string[] DependencyParsing(string[] tks, string[] postags)
        {
            string[] conlls = new string[tks.Length];

            if (conlls.Length == 0) return conlls;

            for (int i = 0; i < conlls.Length; i++)
            {
                conlls[i] = (i + 1).ToString() + "\t" + (tks[i] == "" ? "_" : tks[i]) + "\t_\t" + postags[i] + "\t" + postags[i] + "\t_";
            }

            return player.maltparser.parseTokens(conlls);
        }
    }
}
