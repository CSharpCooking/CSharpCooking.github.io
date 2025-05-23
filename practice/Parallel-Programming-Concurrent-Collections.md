# Практическое задание по теме «Параллельные коллекции»

## Описание задания

Сравните производительность стандартных и параллельных коллекций при выполнении типичных операций (*добавление*, *удаление*, *поиск*). Для этого выполните следующие шаги.

1. Создайте набор тестов, который будет выполнять типичные операции на `List<T>`, `Dictionary<TKey,TValue>`, `ConcurrentBag<T>`, `ConcurrentDictionary<TKey,TValue>`. При этом определите несколько уровней объема данных для тестирования, например, 10 000, 100 000, и 1 000 000 элементов.

   > *Поиск* и *удаление* не являются типичными операциями для `ConcurrentBag`, поэтому для данного типа коллекции их можно пропустить.

2. Используйте многопоточность для работы с параллельными коллекциями и сравните их производительность с однопоточным использованием обычных коллекций.

3. Сделайте выводы о целесообразности использования параллельных коллекций в различных сценариях.

## Методические указания по выполнению

- Для выполнения задания рекомендуется ознакомиться с темой "Параллельные коллекции" 23-й главы «Параллельное программирование» книги [Албахари Д. C# 7.0. Справочник. Полное описание языка](https://csharpcooking.github.io/theory/AlbahariCSharp7Ru.pdf).
- Для написания и проверки кода рекомендуется использовать одно из следующих программных обеспечений:
  - [Visual Studio: IDE и редактор кода для разработчиков и групп, работающих с программным обеспечением](https://visualstudio.microsoft.com/)
  - [LINQPad – The .NET Programmer's Playground](https://www.linqpad.net/)

## Вариант решения

Если вы столкнулись с трудностями при выполнении данного задания, программное решение доступно по [ссылке](https://github.com/CSharpCooking/ParallelProgramming/blob/Concurrent-Collections/Concurrent-Collections-Task-Solution/Program.cs).