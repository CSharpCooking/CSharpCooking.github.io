# Практическое задание по теме «Parallel LINQ»

## Описание задания

Ниже представлен код программы, который загружает словарь английских слов.

```csharp
string wordLookupFile = Path.Combine(Path.GetTempPath(), "WordLookup.txt");

if (!File.Exists(wordLookupFile)) // Contains about 150000 words
  new WebClient().DownloadFile(
    "https://csharpcooking.github.io/data/allwords.txt", wordLookupFile);
```

Расширьте этот код, добавив следующую функциональность:  
- Поиск по шаблону: реализуйте возможность поиска слов в словаре, которые соответствуют заданному регулярному выражению.
- Шифрование найденных слов: все слова, соответствующие заданному регулярному выражению, зашифруйте с использованием алгоритма AES.
- Использование Parallel LINQ: для повышения скорости обработки данных примените технологию Parallel LINQ.

## Методические указания по выполнению

- Для выполнения задания рекомендуется ознакомиться с материалами книги [Албахари Д. C# 7.0. Справочник. Полное описание языка](https://csharpcooking.github.io/theory/AlbahariCSharp7Ru.pdf):
  - глава "Регулярные выражения";
  - тема "Симметричное шифрование" главы "Криптография";
  - тема "Parallel LINQ" главы "Параллельное программирование";
- Для написания и проверки кода рекомендуется использовать одно из следующих программных обеспечений:
  - [Visual Studio: IDE и редактор кода для разработчиков и групп, работающих с программным обеспечением](https://visualstudio.microsoft.com/)
  - [LINQPad – The .NET Programmer's Playground](https://www.linqpad.net/)

## Вариант решения

Если вы столкнулись с трудностями при выполнении этого задания, решение доступно по следующей [ссылке](https://github.com/CSharpCooking/ParallelProgramming/blob/Parallel-LINQ/Parallel-LINQ-Task-Solution/Program.cs).