using System.Xml.Linq;

bool ordersMatters = true; // Set to false if order does not matter

string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
string pathMergedXml = Path.Combine(projectDir, @"inputs\0\result0.xml");
string pathExpectedXML = Path.Combine(projectDir, @"inputs\0\base0.xml");


void Compare(string pairedXml, string expectedXML, bool ordersDoesMatters)
{
    if (!validFilesXLM(pathMergedXml, pathExpectedXML)) return;

    // Prepis po riadoch
    string[] linesMer, linesExp;

    try
    {
        linesMer = File.ReadAllLines(pathMergedXml);
        linesExp = File.ReadAllLines(pathExpectedXML);
    } catch (Exception ex) { 
        Console.WriteLine($"{ex.Message}"); 
        return; 
    }

    // orezáva biele znaky a odstraňuje prázdné riadky
    string[] expected = linesExp.Select(line => line.Trim()).Where(line => line != "").ToArray();
    string[] merged = linesMer.Select(line => line.Trim()).Where(line => line != "").ToArray();

    if (ordersDoesMatters)
    {
        checkOrdered(expected, merged);
    } 
    else {
        checkUnordered(expected, merged);
    }
}

void checkOrdered(string[] expected, string[] merged)
{
    if (areEqualOrderMatters(expected, merged))
    {
        Console.WriteLine("Súbory jsou identické (pořadí řádků je stejné).");
    }
    else
    {
        int addedLines = expected.Except(merged).Count();
        int missingLines = merged.Except(expected).Count();
        Console.WriteLine($"Soubory nejsou identické. Přidané řádky: {addedLines}, Chybějící řádky: {missingLines}");
        double rightPositionLinePerc = percentageInRightPosition(expected, merged);
    }
}

void checkUnordered(string[] expected, string[] merged)
{
    if (areEqualOrderDoesNotMatter(expected, merged))
    {
        Console.WriteLine("Súbory jsou identické (pořadí řádků není důležité).");
    }
    else
    {
        int addedLines = expected.Except(merged).Count();
        int missingLines = merged.Except(expected).Count();
        Console.WriteLine($"Soubory nejsou identické. Přidané řádky: {addedLines}, Chybějící řádky: {missingLines}");
    }
}

double percentageInRightPosition(string[] expected, string[] comparedTo)
    {
        int loops = Math.Min(expected.Length, comparedTo.Length);
        int rightPositionCount = 0;
        for (int i = 0; i < loops; i++)
        {
            if (expected[i] == comparedTo[i])
            {
                rightPositionCount++;
            }
        }
        return (double)rightPositionCount / expected.Length * 100;
    }

bool validFilesXLM(string pathGeneratedXML, string pathMergerdedXML)
{
    if (!File.Exists(pathMergedXml))
    {
        Console.WriteLine($"File not found: {pathMergedXml}");
        return false;
    }
    if (!File.Exists(pathExpectedXML))
    {
        Console.WriteLine($"File not found: {pathExpectedXML}");
        return false;
    }
    
    if (!IsValidXml(pathExpectedXML)) // snad sa to nestane, ale pre istotu
    {
        Console.WriteLine("Generovaný výsledok je neplatný XML súbor.");
        return false;
    }

    if (!IsValidXml(pathMergedXml))
    {
        Console.WriteLine("Pospájaný výsledok nie je platný XML súbor.");
        return false;
    }
    return true;
}

bool areEqualOrderMatters(string[] a, string[] b)
{
    return a.SequenceEqual(b);
}

bool areEqualOrderDoesNotMatter(string[] a, string[] b)
{
    var setA = new HashSet<string>(a);
    var setB = new HashSet<string>(b);
    return setA.SetEquals(setB);
}

bool IsValidXml(string path)
{
    try
    {
        XDocument.Load(path);
        return true;
    }
    catch (System.Xml.XmlException)
    {
        return false;
    }
}

