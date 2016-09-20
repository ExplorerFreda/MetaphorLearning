using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;


/// <summary>
/// 
/// </summary>
public class ConceptualizationProcessor : Processor
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
        return new Schema("Target:string, Source:string, Plausibility:float");
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
        int numConsidered = int.Parse(args[0]);

        List<Tuple<string, double>> target_hypernyms = new List<Tuple<string, double>>();
        List<Tuple<string, double>> source_hypernyms = new List<Tuple<string, double>>();
        double target_denu = 0;
        double source_denu = 0;

        foreach (Row row in input.Rows)
        {
            target_hypernyms.Clear();
            source_hypernyms.Clear();
            target_denu = source_denu = 0;

            foreach (string kv_str in row["Target_Hypernyms"].String.Split(new string[] { ";sp;" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] kv = kv_str.Split(new string[] { ":sp:" }, StringSplitOptions.RemoveEmptyEntries);
                Tuple<string, double> tp = new Tuple<string, double>(kv[0], double.Parse(kv[1]));
                target_denu += tp.Item2;
                target_hypernyms.Add(tp);

                if (target_hypernyms.Count >= numConsidered) break;
            }

            foreach (string kv_str in row["Source_Hypernyms"].String.Split(new string[] { ";sp;" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] kv = kv_str.Split(new string[] { ":sp:" }, StringSplitOptions.RemoveEmptyEntries);
                Tuple<string, double> tp = new Tuple<string, double>(kv[0], double.Parse(kv[1]));
                source_denu += tp.Item2;
                source_hypernyms.Add(tp);

                if (source_hypernyms.Count >= numConsidered) break;
            }

            //v1.1
            //int freq = int.Parse(row["Frequency"].String);
            //float mentionWeight = (float)Math.Log(1 + freq);
            //v1.2
            float freq = float.Parse(row["Frequency"].String);
            float mentionWeight = (float)Math.Log(freq);
            //v1.3
            //double target_min = target_hypernyms[target_hypernyms.Count - 1].Item2;
            //target_denu = target_hypernyms[0].Item2 - target_min;
            //double source_min = source_hypernyms[source_hypernyms.Count - 1].Item2;
            //source_denu = source_hypernyms[0].Item2 - source_min;

            foreach (var tkv in target_hypernyms)
            {
                foreach (var skv in source_hypernyms)
                {
                    output["Target"].Set(tkv.Item1);
                    output["Source"].Set(skv.Item1);
                    //v1.1
                    //float p = (float)tkv.Item2 / (float)target_denu * (float)skv.Item2 / (float)source_denu * mentionWeight;
                    //v1.2
                    float p = (float)(tkv.Item2 + skv.Item2 + mentionWeight);
                    //v1.3
                    //float p = (float)((target_denu == 0 ? 1.0 : (tkv.Item2 - target_min) / target_denu) + (source_denu == 0 ? 1.0 : (skv.Item2 - source_min) / source_denu));
                    output["Plausibility"].Set(p);
                    yield return output;
                }
            }
        }
    }
}
