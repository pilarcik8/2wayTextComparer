using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;


string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
var percCorrectness = new List<double>();

var addedElementCounts = new List<int>();
var missingElementCounts = new List<int>();
var wrongPositionCounts = new List<int>();
var wrongValueCounts = new List<int>();

int index = 0;
int countNotValidXMLFiles = 0;
int countFileCoparitions = 0;
int countCorrectFiles = 0;

bool ordersMatters = UserAnswerOrderMatters();
string pathToFiles = UserInputDirToFiles();
while (true)
{
    string pathMergedXml = Path.Combine(pathToFiles, $"mergedResult{index}.xml");
    string pathExpectedXML = Path.Combine(pathToFiles, index.ToString(), $"expectedResult{index}.xml");

    if (!FilesExists(pathMergedXml, pathExpectedXML)) break; //vypne sa ked uz nenajde dvojicu suborov s indexom

    // orezáva biele znaky a odstraňuje prázdné riadky
    string[] expected = File.ReadAllLines(pathExpectedXML).Select(line => line.Trim()).Where(line => line != "").ToArray();
    string[] merged = File.ReadAllLines(pathMergedXml).Select(line => line.Trim()).Where(line => line != "").ToArray();


    if (!IsValidXml(pathExpectedXML))
    {
        Console.Error.WriteLine("Veľký problém, XML generátor vytvoril nefungujúci XML");
        return;
    }

    countFileCoparitions++;
    if (!IsValidXml(pathMergedXml))
    {
        countNotValidXMLFiles++;
        index++;
        continue;
    }

    // rychla kontrola, či sú súbory úplne rovnaké, ak áno, nemusíme porovnávať elementy a hodnoty
    if (ordersMatters)
    {
        if (AreEqualOrderMatters(expected, merged))
        {
            countCorrectFiles++;
            index++;
            continue;
        }
    }
    else
    {
        if (AreEqualOrderDoesNotMatter(expected, merged))
        {
            countCorrectFiles++;
            index++;
            continue;
        }
    }

    // porovnáme elementy a hodnoty, aby sme zistili, čo presne je zle
    var addedRemovedChangedCounts = GetAddedMissingWrongValueCounts(expected, merged);

    var addedCount = addedRemovedChangedCounts[0];
    var missingCount = addedRemovedChangedCounts[1];
    var wrongValueCount = addedRemovedChangedCounts[2];
    var wrongPositionCount = ordersMatters ? ElementsInWrongPosition(expected, merged) : 0;

    if (addedCount == 0 && missingCount == 0 && wrongValueCount == 0 && wrongPositionCount == 0)
    {
        Console.Error.WriteLine("Nejaká chyba v porovnávaní, súbory nejsou stejné ale neidentifikovali jsme žádný rozdíl");
        countCorrectFiles++;
    }

    addedElementCounts.Add(addedCount);
    missingElementCounts.Add(missingCount);
    wrongValueCounts.Add(wrongValueCount);
    wrongPositionCounts.Add(wrongPositionCount);

    index++;
}

Console.WriteLine($"Porovnaných {countFileCoparitions} súborov, z toho {countNotValidXMLFiles} nebolo validních XML a {countFileCoparitions - countCorrectFiles} nebolo rovnakých ale boli valídne");
double averageCorrectness = countFileCoparitions > 0 ? (double)countCorrectFiles / countFileCoparitions * 100 : 0;
Console.WriteLine($"Priemerná správnosť: {averageCorrectness}%");

for (int i = 0; i < addedElementCounts.Count; i++)
{
    if (addedElementCounts[i] == 0 && missingElementCounts[i] == 0 && wrongValueCounts[i] == 0 && wrongPositionCounts[i] == 0)
    {
        continue;
    }
    Console.WriteLine($"Súbor {i}: Přidané elementy: {addedElementCounts[i]}, Chybějící elementy: {missingElementCounts[i]}, Nesprávné hodnoty: {wrongValueCounts[i]}, Nesprávné pozice: {wrongPositionCounts[i]}");
}

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

int[] GetAddedMissingWrongValueCounts(string[] expected, string[] merged)
{
    var addedElements = merged.Except(expected);
    var missingElements = expected.Except(merged);
    var wrongValueCount = 0;

    foreach (var element in addedElements)
    {
        var label = element.Substring(0, element.IndexOf('>'));
        // Ak removed aj added obsahujú rovnako pomenovaný element, znamená to že daný element má nesprávnu hodnotu
        if (missingElements.Any(r => r.StartsWith(label)))
        {
            wrongValueCount++;
        }
    }
    var addedCount = addedElements.Count() - wrongValueCount;
    var removedCount = missingElements.Count();
 
    var changed = expected.Intersect(merged).Count();
    return new int[] { addedCount, removedCount, wrongValueCount };
}

int ElementsInWrongPosition(string[] expected, string[] merged)
{
    int wrongPositionCount = 0;
    var expectedSet = new HashSet<string>(expected);
    int loops = Math.Min(expected.Length, merged.Length);
    for (int i = 0; i < loops; i++)
    {
        if (expectedSet.Contains(merged[i]) && expected[i] != merged[i])
        {
            wrongPositionCount++;
        }
    }
    return wrongPositionCount;
}

string UserInputDirToFiles()
{
    Console.WriteLine("V priečinku majte očíslované priečinky od 0");
    Console.WriteLine("V očísloslovanom priečinku majte súbory expectedResult'číslo iterácie'.xml a mergedResult'číslo iterácie.xml'");
    Console.WriteLine("Prvý priečinok by mal: '0/mergedResult0.xml'. Súbor 'expectedResult0.xml' by mal byť s ostatnými týmito súbormi v predkovi adresára '0'");
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
    if (!File.Exists(pathMergedXml) || !File.Exists(pathExpectedXML))
    {
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

