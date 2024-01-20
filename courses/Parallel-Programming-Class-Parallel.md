# Практическое задание по теме «Класс Parallel» 

## Описание задания

Ниже представлен код программы, которая выполняет проверку орфографии с использованием индексированной версии `Parallel.ForEach`.

```csharp
string wordLookupFile = Path.Combine(Path.GetTempPath(), "WordLookup.txt");

if (!File.Exists(wordLookupFile)) // Contains about 150000 words
  new WebClient().DownloadFile(
    "http://www.albahari.com/ispell/allwords.txt", wordLookupFile);

var wordLookup = new HashSet < string > (
  File.ReadAllLines(wordLookupFile),
  StringComparer.InvariantCultureIgnoreCase);

var random = new Random();
string[] wordList = wordLookup.ToArray();

string[] wordsToTest = Enumerable.Range(0, 1000000)
  .Select(i => wordList[random.Next(0, wordList.Length)])
  .ToArray();

// Introducing a few spelling mistakes
wordsToTest[12340] = "woozsh";
wordsToTest[12342] = "wubsie";
wordsToTest[12344] = "adgdgr";
wordsToTest[12348] = "dfgsie";

var misspellings = new ConcurrentBag < Tuple < int, string >> ();

Parallel.ForEach(wordsToTest, (word, state, i) => {
  if (!wordLookup.Contains(word))
    misspellings.Add(Tuple.Create((int) i, word));
});

misspellings.Dump();
```

Модифицируйте программу таким образом, чтобы продемонстрировать раннее прекращение параллельного метода  `Parallel.ForEach`, например, после выявления первой ошибки. Для решения используйте метод `Break` или `Stop` на объекте `ParallelLoopState` (реализуйте оба варианта).

## Методические указания по выполнению

- Для выполнения задания рекомендуется ознакомиться с темой "Класс Parallel" 23-й главы "Параллельное программирование" книги [Албахари Д. C# 7.0. Справочник. Полное описание языка](https://csharpcooking.github.io/theory/AlbahariCSharp7.zip).
- Для написания и проверки кода рекомендуется использовать одно из следующих программных обеспечений:
  - [Visual Studio: IDE и редактор кода для разработчиков и групп, работающих с программным обеспечением](https://visualstudio.microsoft.com/)
  - [LINQPad – The .NET Programmer's Playground](https://www.linqpad.net/)