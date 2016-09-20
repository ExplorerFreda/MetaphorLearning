using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using Newtonsoft.Json;

public class ScriptFunction {
    public static string Tokens2Sentence(string tks_js)
    {
        string[] tks = JsonConvert.DeserializeObject<string[]>(tks_js);
        StringBuilder st = new StringBuilder();

        for (int i = 0; i < tks.Length; ++i)
        {
            if (i > 0)
            {
                st.Append(" ");
            }

            st.Append(tks[i]);
        }

        return st.ToString();
    }
}