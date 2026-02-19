using System.Xml.Linq;


string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));

int index = 0;
int countNotValidXMLFiles = 0;
int countFileCoparitions = 0;

bool ordersMatters = UserAnswerOrderMatters();
double[] percCorrectness = [];


while (true)
{
    string pathMergedXml = Path.Combine(projectDir, $@"inputs\{index}\result{index}.xml");
    string pathExpectedXML = Path.Combine(projectDir, $@"inputs\expected.xml");

    if (!FilesExists(pathMergedXml, pathExpectedXML)) break; //vypne sa ked uz nenajde dvojicu suborov s indexom

    // array riadok
    string[] linesMer = File.ReadAllLines(pathMergedXml);
    string[] linesExp = File.ReadAllLines(pathExpectedXML);

    // orezáva biele znaky a odstraňuje prázdné riadky
    string[] expected = linesExp.Select(line => line.Trim()).Where(line => line != "").ToArray();
    string[] merged = linesMer.Select(line => line.Trim()).Where(line => line != "").ToArray();


    if (!IsValidXml(pathExpectedXML))
    {
        Console.Error.WriteLine("Veľký problém, XML generátor vytvoril nefungujúci XML");
        // CLOSEFILE todo
        return;
    }

    // odtialto kontrolujeme xml a dostaneme 1 vysledok
    countFileCoparitions++;
    if (!IsValidXml(pathMergedXml))
    {
        countNotValidXMLFiles++;
        index++;
        continue;
    }

    index++;
}

bool UserAnswerOrderMatters()
{
    Console.WriteLine("Zaleží na poradí atribútov/elementov?");
    Console.WriteLine("Odpovedz: yes/no");
    string? input = "";
    while (input != "yes" || input != "no")
    {
        input = Console.ReadLine();
        if (input == null)
        {
            input = "";
            continue;
        }
        input = input.ToLower();
    }
    if (input == "yes") return true;

    return false;    
}

// Vrati % spravnosti XML
double Compare(string[] linesMer, string[] linesExp, bool ordersDoesMatters)
{

    if (ordersDoesMatters)
    {
        return CheckOrdered(linesExp, linesMer);
    } 
    return CheckUnordered(linesExp, linesMer);
}

double CheckOrdered(string[] expected, string[] merged)
{
    if (AreEqualOrderMatters(expected, merged))
    {
        return 100.0;
    }
    else
    {
        //int addedLines = expected.Except(merged).Count();
        //int missingLines = merged.Except(expected).Count();
        //Console.WriteLine($"Soubory nejsou identické. Přidané řádky: {addedLines}, Chybějící řádky: {missingLines}");
        return PercentageInRightOrder(expected, merged);
    }
}

double CheckUnordered(string[] expected, string[] merged)
{
    if (AreEqualOrderDoesNotMatter(expected, merged))
    {
        return 100.0;
    }

    //int addedLines = expected.Except(merged).Count();
    //int missingLines = merged.Except(expected).Count();
    //Console.WriteLine($"Soubory nejsou identické. Přidané řádky: {addedLines}, Chybějící řádky: {missingLines}");
    return PercentageRight(expected, merged);

}

double PercentageInRightOrder(string[] expected, string[] comparedTo)
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

double PercentageRight(string[] expected, string[] comparedTo)
{
    int correct = expected.Intersect(comparedTo).Count();

    return (double)correct / expected.Length * 100;
}

bool FilesExists(string pathMergedXml, string pathExpectedXML)
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
    return true;
}

bool AreEqualOrderMatters(string[] a, string[] b)
{
    return a.SequenceEqual(b);
}

bool AreEqualOrderDoesNotMatter(string[] a, string[] b)
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

