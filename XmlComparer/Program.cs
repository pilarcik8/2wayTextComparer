using System.Xml.Linq;


string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
var percCorrectness = new List<double>();
int index = 0;
int countNotValidXMLFiles = 0;
int countFileCoparitions = 0;

bool ordersMatters = UserAnswerOrderMatters();
string pathToFiles = UserInputDirToFiles();
while (true)
{
    string pathMergedXml = Path.Combine(pathToFiles, $"mergedResult{index}.xml");
    string pathExpectedXML = Path.Combine(pathToFiles, index.ToString(), $"expectedResult{index}.xml");

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

    // vypočítaj percento správnosti a ulož do zoznamu
    double percent = Compare(merged, expected, ordersMatters);
    percCorrectness.Add(percent);

    index++;
}
if (countFileCoparitions == 0)
{
    Console.WriteLine("Nenájdený žiaden pár súborov.");
    return;
}

Console.WriteLine($"Porovnaných {countFileCoparitions} súborov, z toho {countNotValidXMLFiles} nebolo validních XML.");

// bezpečne získať priemer (ak žiadne porovnania, priemer = 0)
double average = percCorrectness.Average();
Console.WriteLine($"Percento správnosti: {average}%");

// vytvorenie súboru: prvý riadok = priemer, potom každý výsledok na samostatnom riadku
string outputsDir = Path.Combine(projectDir, "outputs");
Directory.CreateDirectory(outputsDir);
string resultsFile = Path.Combine(outputsDir, "results.txt");

var outputLines = new List<string>
{
    $"{average:F2}%"
};
outputLines.AddRange(percCorrectness.Select(p => $"{p:F2}%"));

File.WriteAllLines(resultsFile, outputLines);
Console.WriteLine($"Výsledky zapísané do: {resultsFile}");


bool UserAnswerOrderMatters()
{
    Console.WriteLine("Zaleží na poradí atribútov/elementov?");
    Console.WriteLine("Odpovedz: yes/no");
    string? input = "";
    // Cyklus pokračuje, dokiaľ nie je zadané "yes" alebo "no"
    while (input != "yes" && input != "no")
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

string UserInputDirToFiles()
{
    Console.WriteLine("V priečinku majte očíslované priečinky od 0");
    Console.WriteLine("V očísloslovanom priečinku majte súbory expectedResult'číslo iterácie'.xml a mergedResult'číslo iterácie.xml'");
    Console.WriteLine("Prvý priečinok by mal: '0/mergedResult0.xml'. Súbor 'expectedResult0.xml' by mal byť s ostantnými týmtito súbormi v predokovi adresáta '0'");
    Console.WriteLine("A tak ďalej... Ak sa priečinok alebo súbor z danej iterácie nenájde, program končí.");
    Console.WriteLine("---------------------------------------------------------------------------------------------------------------");
    Console.WriteLine("Vložte absolútnu cestu k priečinku so súbormi");

    while (true)
    {
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("Zadajte platnú cestu (nie prázdnu). Skúste znova:");
            continue;
        }

        input = input.Trim().Trim('"');

        try
        {
            string full = Path.GetFullPath(input);

            if (!Directory.Exists(full))
            {
                Console.WriteLine($"Adresár neexistuje: {full}. Skontrolujte cestu a skúste znova:");
                continue;
            }

            return full;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Neplatná cesta: {ex.Message}. Skúste znova:");
        }
    }
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
        return PercentageInRightOrder(expected, merged);
    }
}

double CheckUnordered(string[] expected, string[] merged)
{
    if (AreEqualOrderDoesNotMatter(expected, merged))
    {
        return 100.0;
    }

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

