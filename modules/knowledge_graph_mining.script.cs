using Microsoft.SCOPE.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScopeRuntime;

using Newtonsoft.Json;

public class RelationProcessor : Processor
{
	public override Schema Produces(string[] requestedColumns, string[] args, Schema input)
	{
		Schema output = new Schema();
		output.Add(new ColumnInfo("Target", ColumnDataType.String));
		output.Add(new ColumnInfo("Source", ColumnDataType.String));
		output.Add(new ColumnInfo("Relation", ColumnDataType.String));
		output.Add(new ColumnInfo("Count", ColumnDataType.Integer));
		return output;
	}
	public override IEnumerable<Row> Process(RowSet input, Row outputRow, string[] args)
	{
		string type = args[0];
		HashSet<string> relation_stop_words = new HashSet<string>();
		/*
		relation_stop_words.Add("important");
		relation_stop_words.Add("well");
		relation_stop_words.Add("much");
		relation_stop_words.Add("old");
		relation_stop_words.Add("effective");
		relation_stop_words.Add("simple");
		relation_stop_words.Add("many");
		relation_stop_words.Add("good");
		relation_stop_words.Add("easy");
		relation_stop_words.Add("high");
		relation_stop_words.Add("long");
		relation_stop_words.Add("far");
		relation_stop_words.Add("soon");
		 */
		foreach (Row row in input.Rows)
		{
			string target = row["Left"].String.ToLower();
			string source = row["Right"].String.ToLower();
			outputRow["Target"].Set(target);
			outputRow["Source"].Set(source);
			switch (type)
			{
				case "closed_similes":
					string sentence = row["Sentence"].String;
					int[] tkss = JsonConvert.DeserializeObject<int[]>(row["TokenStarts"].String);
					int[] tkes = JsonConvert.DeserializeObject<int[]>(row["TokenEnds"].String);
					int pattern_start = Convert.ToInt32(row["PatternStart"].String);
					int pattern_end = Convert.ToInt32(row["PatternEnd"].String);
					string relation = sentence.Substring(tkss[pattern_start + 2], tkes[pattern_start + 2] - tkss[pattern_start + 2]).ToLower();
					if (relation_stop_words.Contains(relation))
					{
						continue;
					}
					outputRow["Relation"].Set(relation);
					outputRow["Count"].Set(1);
					break;
				case "isa_pattern":
					outputRow["Relation"].Set("?");
					outputRow["Count"].Set(1);
					break;
				case "open_similes":
					outputRow["Relation"].Set("NH");
					outputRow["Count"].Set(1);
					break;
				case "hypernym":
					outputRow["Relation"].Set("H");
					outputRow["Count"].Set(row["Count"].Integer);
					break;
			}
			yield return outputRow;
		}
	}
}