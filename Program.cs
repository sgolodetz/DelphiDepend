using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
	private enum DependencyType
	{
		IMPLEMENTATION,
		INTERFACE
	}

	private class Dependency
	{
		public string From { get; set; }
		public string To { get; set; }
		public DependencyType Type { get; set; }
	}

	private static IEnumerable<Dependency> AnalyseDependenciesInPath(string path)
	{
		foreach(string filename in Directory.EnumerateFiles(path, "*.pas", SearchOption.AllDirectories))
		{
			foreach(Dependency dependency in AnalyseDependenciesInFile(filename))
			{
				yield return dependency;
			}
		}
	}

	private static IEnumerable<Dependency> AnalyseDependenciesInFile(string filename)
	{
		return AnalyseDependenciesInSource(File.ReadAllText(filename));
	}

	private static IEnumerable<Dependency> AnalyseDependenciesInSource(string source)
	{
		source = RemoveCommentsFromSource(source);

		Match unitMatch = Regex.Match(source, @"\bunit\b\s*(?'unitName'\w+)\s*;", RegexOptions.IgnoreCase);
		Match interfaceMatch = Regex.Match(source, @"\binterface\b", RegexOptions.IgnoreCase);
		Match implementationMatch = Regex.Match(source, @"\bimplementation\b", RegexOptions.IgnoreCase);
	
		string from = unitMatch.Groups["unitName"].ToString();
		if(from != "")
		{
			string interfaceSection = source.Substring(interfaceMatch.Index, implementationMatch.Index - interfaceMatch.Index);
			string implementationSection = source.Substring(implementationMatch.Index);

			Match interfaceUsesMatch = Regex.Match(interfaceSection, @"\buses\b([^;]*);", RegexOptions.IgnoreCase);
			foreach(string to in interfaceUsesMatch.Groups[1].ToString().Split(',').Select(s => s.Trim()))
			{
				if(to != "")
				{
					yield return new Dependency { From = from, To = to, Type = DependencyType.INTERFACE };
				}
			}

			Match implementationUsesMatch = Regex.Match(implementationSection, @"\buses\b([^;]*);", RegexOptions.IgnoreCase);
			foreach(string to in implementationUsesMatch.Groups[1].ToString().Split(',').Select(s => s.Trim()))
			{
				if(to != "")
				{
					yield return new Dependency { From = from, To = to, Type = DependencyType.IMPLEMENTATION };
				}
			}
		}
	}

	private static string RemoveCommentsFromSource(string source)
	{
		string result;
		result = Regex.Replace(source, @"\(\*.*\*\)", "");
		result = Regex.Replace(source, @"{[^}]*}", "");
		result = Regex.Replace(result, @" *//.*\n", "\n");
		return result;
	}

	private static IEnumerable<Dependency> RemoveExternalDependencies(IEnumerable<Dependency> dependencies)
	{
		var internals = new HashSet<string>(dependencies.Select(d => d.From));
		return dependencies.Where(d => internals.Contains(d.To));
	}

	static void Main(string[] args)
	{
		if(args.Length != 4)
		{
			Console.WriteLine("Usage: DelphiDepend {+|-}e {+|-}m {+|-}n <path>");
			return;
		}

		try
		{
			Console.WriteLine("digraph");
			Console.WriteLine("{");

			IEnumerable<Dependency> dependencies = AnalyseDependenciesInPath(Path.GetFullPath(args[3]));

			// Remove external dependencies if desired.
			if(args[0] == "-e")
			{
				dependencies = RemoveExternalDependencies(dependencies);
			}

			foreach(var d in dependencies)
			{
				switch(d.Type)
				{
					case DependencyType.IMPLEMENTATION:
						if(args[1] == "+m")
						{
							Console.WriteLine("\t" + d.From + " -> " + d.To + " [color=red];");
						}
						break;
					case DependencyType.INTERFACE:
						if(args[2] == "+n")
						{
							Console.WriteLine("\t" + d.From + " -> " + d.To + " [color=green];");
						}
						break;
				}
			}

			Console.WriteLine("}");
		}
		catch(Exception e)
		{
			Console.WriteLine(e.Message);
		}
	}
}
