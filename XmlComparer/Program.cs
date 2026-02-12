using System.IO.Enumeration;
using System.Xml.Serialization;


string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
string fileName = Path.Combine(projectDir, @"inputs\0\base0.xml");
IEnumerable<string> lines;

try
{
    lines = File.ReadLines(fileName);
} catch (Exception ex) { 
    Console.WriteLine($"{ex.Message}"); 
    return; 
}

string[] trimmed_lines = lines.Select(line => line.Trim()).Where(line => line != "").ToArray();

foreach (var line in trimmed_lines) 
{
    Console.WriteLine(line);
}


