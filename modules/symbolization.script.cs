using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using Newtonsoft.Json;

public class Tokens2Sentence
{
    public static string Process(string tks_json)
    {
        StringBuilder s = new StringBuilder();

        bool fb = true;

        foreach (string t in JsonConvert.DeserializeObject<string[]>(tks_json))
        {
            if (fb == true)
            {
                fb = false;
            }
            else
            {
                s.Append(" ");
            }

            s.Append(t);
        }

        return s.ToString();
    }
}