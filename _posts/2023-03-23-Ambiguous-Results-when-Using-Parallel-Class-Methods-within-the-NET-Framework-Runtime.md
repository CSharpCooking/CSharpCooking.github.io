---
title: Неоднозначность результатов при использовании методов класса Parallel в рамках исполняющей среды .NET Framework
description: "Параллельное программирование – это способ написания программ, которые могут выполняться параллельно на нескольких процессорах или ядрах. Это позволяет программам обрабатывать большие объемы данных или выполнить более сложные вычисления за приемлемое время, чем это было бы возможно на одном процессоре. Преимущества параллельного программирования: увеличение производительности, распределение нагрузки, обработка больших объемов данных, улучшение отзывчивости, увеличение надежности. В целом, параллельное программирование имеет множество преимуществ, которые могут помочь улучшить производительность и надежность программных систем, особенно в условиях растущей сложности вычислительных задач и объемов данных. Однако параллельное программирование также может иметь свои сложности, связанные с управлением синхронизацией, гонками данных и другими аспектами, которые требуют дополнительного внимания и опыта со стороны программиста. В ходе тестирования параллельных программ можно получить неоднозначные результаты. Например, это может происходить, когда мы оптимизируем объединение данных типа `float` или `double` посредством методов `For` или `ForEach` класса `Parallel`. Подобное поведение программы заставляет усомниться в потокобезопасности написанного кода. Пост раскрывает возможную причину неоднозначности результатов, получаемых параллельной программой, и предлагает лаконичное решение вопроса."
author: RuslanGibadullin
date: 2023-03-22
categories: [Статьи]
tags: [параллельное программирование, класс Parallel]
---

## Неоднозначность результатов в ходе параллельной агрегации локальных значений[^1]

Методы `Parallel.For` и `Parallel.ForEach` предлагают набор перегруженных версий, которые работают с аргументом обобщенного типа по имени `TLocal`. Такие перегруженные версии призваны помочь оптимизировать объединение данных из циклов с интенсивными итерациями. Ниже представлена перегруженная версия метода `Parallel.For`, которую далее мы будем использовать в предметном анализе.

```csharp
public static ParallelLoopResult For(
 int fromInclusive,
 int toExclusive,
 Func localInit,
 Func<int, ParallelLoopState, TLocal, TLocal> body,
 Action localFinally);
```

Где:

- `fromInclusive` – начальный индекс, включительно.
- `toExclusive` – конечный индекс, не включительно.
- `localInit` – делегат функции, который возвращает начальное состояние локальных данных для каждой задачи.
- `body` – делегат, который вызывается один раз за итерацию.
- `localFinally` – делегат, который выполняет финальное действие с локальным результатом каждой задачи.
- `TLocal` – тип данных, локальных для потока.
- Возвращаемый объект – структура `ParallelLoopResult`, в которой содержатся сведения о выполненной части цикла.

Применим данный метод на практике, чтобы просуммировать квадратные корни чисел от 1 до 10<sup>7</sup>

```csharp

object locker = new object();
double grandTotal = 0;
Parallel.For(1, 10000000,
 () => 0.0, // Initialize the local value.
 (i, state, localTotal) => // Body delegate. Notice that it
  localTotal + Math.Sqrt(i), // returns the new local total.
 localTotal => // Add the local
  { lock (locker) grandTotal += localTotal; } // to the master value.
);
Console.WriteLine(grandTotal);
```

Данное решение может выдавать неоднозначный результат, например:

- 21081849486,4431; 
- 21081849486,4428;
- 21081849486,4429.

Причина неоднозначности результатов является комплексной. Во-первых, имеют место ошибки округления вещественных чисел. Во-вторых, выполнение делегата, отвечающего за формирование локального накопителя, в потоках пула носит порциональный характер. Рассмотрим и то и другое более детально.

Типы `float` и `double` внутренне представляют числа в двоичной форме. По указанной причине точно представляются только числа, которые могут быть выражены в двоичной системе счисления. На практике это означает, что большинство литералов с дробной частью (которые являются десятичными) не будут представлены точно. Например:

```csharp
Console.WriteLine((double)0.1f); // 0,100000001490116
```

Именно потому типы `float` и `double` не подходят для финансовых вычислений. В противоположность им тип `decimal` работает в десятичной системе счисления, так что он способен точно представлять дробные числа вроде 0,1, выразимые в десятичной системе (а также в системах счисления с основаниями-множителями 10 – двоичной и пятеричной). Поскольку вещественные литералы являются десятичными, тип `decimal` может точно представлять такие числа, как 0,1. Тем не менее, ни `double`, ни `decimal` не могут точно представлять дробное число с периодическим десятичным представлением:

```csharp
decimal m = 1M / 6M; // 0,1666666666666666666666666667M
double d = 1.0 / 6.0; // 0,16666666666666666
```

Это приводит к накапливающимся ошибкам округления:

```csharp
decimal notQuiteWholeM = m+m+m+m+m+m; // 1,0000000000000000000000000002M
double notQuiteWholeD = d+d+d+d+d+d; // 0,99999999999999989
```

которые нарушают работу операций эквивалентности и сравнения:

```csharp
Console.WriteLine (notQuiteWholeM == 1M);  // False
Console.WriteLine (notQuiteWholeD < 1.0);  // True
```

Ниже в таблице 1 представлен обзор отличий между типами `double` и `decimal`.

Таблица 1. Отличия между типами `double` и `decimal`  
<style type="text/css">
.tg  {border-collapse:collapse;border-color:#ccc;border-spacing:0;}
.tg td{background-color:#fff;border-color:#ccc;border-style:solid;border-width:1px;color:#333;
  font-family:Arial, sans-serif;font-size:14px;overflow:hidden;padding:10px 5px;word-break:normal;}
.tg th{background-color:#f0f0f0;border-color:#ccc;border-style:solid;border-width:1px;color:#333;
  font-family:Arial, sans-serif;font-size:14px;font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;}
.tg .tg-amwm{font-weight:bold;text-align:center;vertical-align:top}
.tg .tg-0lax{text-align:left;vertical-align:top}
</style>
<table class="tg">
<thead>
  <tr>
    <th class="tg-amwm"><span style="font-weight:700;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Характеристика</span></th>
    <th class="tg-amwm"><span style="font-weight:700;font-style:normal;text-decoration:none;color:#000;background-color:transparent">double</span></th>
    <th class="tg-amwm"><span style="font-weight:700;font-style:normal;text-decoration:none;color:#000;background-color:transparent">decimal</span></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Внутреннее представление</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Двоичное</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Десятичное</span></td>
  </tr>
  <tr>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Десятичная точность</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">15–16 значащих цифр</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">28–29 значащих цифр</span></td>
  </tr>
  <tr>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Диапазон</span></td>
    <td class="tg-0lax"><p>&plusmn;(~1<span style="font-weight: 400;">0</span><sup><span style="font-weight: 400;">-324</span></sup><span style="font-weight: 400;">&ndash;~</span><span style="font-weight: 400;">10</span><span style="font-weight: 400;"><sup>308</sup>)</span></p></td>
    <td class="tg-0lax"><p>&plusmn;(~1<span style="font-weight: 400;">0</span><sup><span style="font-weight: 400;">-28</span></sup><span style="font-weight: 400;">&ndash;~</span><span style="font-weight: 400;">10</span><span style="font-weight: 400;"><sup>28</sup>)</span></p></td>
  </tr>
  <tr>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Специальные значения</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">+0, -0, +∞, -∞ и NaN</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Отсутствуют</span></td>
  </tr>
  <tr>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Скорость обработки</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Присущая процессору</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Не присущая процессору (примерно в 10 </span><br><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">раз медленнее, чем в случае double)</span></td>
  </tr>
</tbody>
</table>

Раскроем тип `decimal` более детально, чтобы ответить на вопрос, почему обработка данных типа `decimal` не является присущей процессору. 

Двоичное представление `decimal` числа состоит из 1-битового знака, 96-битового целого числа и коэффициента масштабирования, используемого для деления целочисленного числа и указания его части десятичной дроби. Коэффициент масштабирования неявно представляет собой число 10, возведенное в степень в диапазоне от 0 до 28.  

Таким образом, `decimal` число представляет собой массив `m`, который состоит из четырех 32-разрядных элементов, где:

- `m[0]`, `m[1]` и `m[2]` содержат младшие, средние и высшие разряды 96-разрядного целого числа.
- `m[3]`:
  - 0-15 не используются;
  - 16-23 (8 бит) содержат экспоненту от 0 до 28, что указывает на степень 10 для деления целочисленного числа;
  - 24-30 не используются;
  - 31 содержит знак (0 означает положительное значение, а 1 – отрицательное).

Разбиение на основе порций работает путем предоставления каждому рабочему потоку возможности периодически захватывать из входной последовательности небольшие “порции” элементов с целью их обработки. Например (см. рисунок), инфраструктура Parallel LINQ начинает с выделения очень маленьких порций (один или два элемента за раз) и затем по мере продвижения запроса увеличивает размер порции: это гарантирует, что небольшие последовательности будут эффективно распараллеливаться, а крупные последовательности не приведут к чрезмерным циклам полного обмена. Если рабочий поток получает “простые” элементы (которые обрабатываются быстро), то в конечном итоге он сможет получить больше порций. Такая система сохраняет каждый поток одинаково занятым (а процессорные ядра “сбалансированными”); единственный недостаток состоит в том, что извлечение элементов из разделяемой входной последовательности требует синхронизации – и в результате могут появиться некоторые накладные расходы и состязания.

![](https://raw.githubusercontent.com/CSharpCooking/csharpcooking.github.io/refs/heads/main/pastes/2023-03-23-04-44-43.png)

Рис. Разделение на основе порций

Метод `For` класса `Parallel` работает схожим образом, разница лишь в том, что в качестве элемента входной последовательности выступает номер итерации, который учитывается при выполнении тела цикла (точнее делегата типа `Action<int>`). Реализация разделения основана на механизме разбиения на порции, при котором размер порции потенциально увеличивается в случае положительной динамики обработки итераций. Такой подход помогает обеспечить качественную балансировку нагрузки при небольшом количестве итераций и минимизировать число монопольных блокировок (в ходе назначения диапазонов номеров итераций для рабочих потоков) при их большом количестве. При этом обеспечивается, чтобы большинство итераций потока было сосредоточено в одной и той же области итерационного пространства для достижения высокой локальности кэша.

## Исследование метода Parallel.For для детализации причины неоднозначности конечного результата

Реализация метода `For` сложна и требует детального рассмотрения, которое выходит за рамки данной статьи. Тем не менее отметим некоторые моменты программной реализации метода `Parallel.For` с аргументом обобщенного типа.

```csharp
public static ParallelLoopResult For(int fromInclusive, int toExclusive, Func localInit, Func<int, ParallelLoopState, TLocal, TLocal> body, …)
{
  …
  return ForWorker(fromInclusive, toExclusive, s_defaultParallelOptions,
  null, null, body, localInit, localFinally);
}
private static ParallelLoopResult ForWorker(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action body, …)
{
  …
  rootTask = new ParallelForReplicatingTask(parallelOptions, delegate
  {
    if (rangeWorker.FindNewWork32(
     out var nFromInclusiveLocal,
     out var nToExclusiveLocal) &&
     !sharedPStateFlags.ShouldExitLoop(nFromInclusiveLocal))
    {
      …
      do
      {
        if (body != null)
        {
          for (int i = nFromInclusiveLocal; i < nToExclusiveLocal; i++)
          {
            if (sharedPStateFlags.LoopStateFlags != ParallelLoopStateFlags.PLS_NONE
          && sharedPStateFlags.ShouldExitLoop())
            {
              break;
            }
            body(i);
          }
        }
      }
      while (rangeWorker.FindNewWork32(out nFromInclusiveLocal, out nToExclusiveLocal) …);
      …
    }
  }, creationOptions, internalOptions);
  rootTask.RunSynchronously(parallelOptions.EffectiveTaskScheduler);
  rootTask.Wait();
  …
}
internal ParallelForReplicatingTask(…)
{
  m_replicationDownCount = parallelOptions.EffectiveMaxConcurrencyLevel;
  …
}
```
Метод `rootTask.RunSynchronously` запускает исполнение задач в рабочих потоках пула, при этом число задач задается свойством `parallelOptions.EffectiveMaxConcurrencyLevel`. Метод `FindNewWork32` определяет рабочий диапазон для каждого потока пула. В представленном коде можно увидеть, что выполнение любой задачи не ограничивается выполнением первоначально определенного диапазона, потоки пула продолжают работу для вновь задаваемых диапазонов в операторе `while`.

Проведем детализацию работы метода `Parallel.For` с аргументом обобщенного типа на ранее представленном примере по суммированию квадратных корней чисел, расширив код следующим образом.

```csharp
object locker = new object();
double grandTotal = 0;
ConcurrentBag<(int?, double)> cb1 = new ConcurrentBag<(int?, double)>();
ConcurrentDictionary<int?, long> cd = new ConcurrentDictionary<int?, long>();
ConcurrentBag<(int?, int)> cb2 = new ConcurrentBag<(int?, int)>();
var time = Stopwatch.StartNew();
time.Start();
Parallel.For(1, 1000,
 () => { return 0.0; },
 (i, state, localTotal) =>
 {
   cb1.Add((Task.CurrentId, localTotal));
   if (!cd.ContainsKey(Task.CurrentId)) cd[Task.CurrentId] = time.ElapsedTicks;
   cb2.Add((Task.CurrentId, i));
   return localTotal + Math.Sqrt(i);
 },
 localTotal =>
 { lock (locker) grandTotal += localTotal; }
);
cb1.GroupBy(_ => _.Item1).Select(_ => new
{
  TaskId = _.Key,
  Iterations = _.Count(),
  StartTime = cd[_.Key]
}).OrderBy(_ => _.StartTime).Dump();
var query = cb2.OrderBy(_ => _.Item2).GroupBy(_ => _.Item1, _ => _.Item2);
foreach (var grouping in query)
{
  Console.WriteLine("TaskId: " + grouping.Key);
  var r = grouping.GetEnumerator();
  int? i = null;
  bool onlyOne = true;
  foreach (int iteration in grouping)
  {
    if (i == null)
      Console.Write("{" + $"{iteration}");
    else
    {
      if (iteration - i != 1)
        Console.Write(",...," + i + "}, {" + iteration);
      onlyOne = false;
    }
    i = iteration;
  }
  if (onlyOne) Console.WriteLine("}");
  else Console.WriteLine(",...," + i + "}");
}
```

Программный код позволяет учесть:
- идентификатор каждой задачи `TaskId`; 
- количество итераций выполненное в рамках каждой задачи `Iterations`; 
- `StartTime` – время начала работы каждой задачи, выраженное в тиках посредством класса `Stopwatch` (один тик является меньше одной микросекунды);
- диапазоны номеров обработанных итераций каждой задачи.

Например, по результатам работы программы на машине, способной выполнять 8 потоков параллельно на аппаратном уровне, можно получить следующие показатели `TaskId`, `Iterations`, `StartTime` (табл. 2). Диапазоны номеров обработанных итераций представлены в таблице 3. 

Таблица 2. Показатели `TaskId`, `Iterations`, `StartTime` после завершения метода `Parallel.For`

<style type="text/css">
.tg  {border-collapse:collapse;border-color:#ccc;border-spacing:0;}
.tg td{background-color:#fff;border-color:#ccc;border-style:solid;border-width:1px;color:#333;
  font-family:Arial, sans-serif;font-size:14px;overflow:hidden;padding:10px 5px;word-break:normal;}
.tg th{background-color:#f0f0f0;border-color:#ccc;border-style:solid;border-width:1px;color:#333;
  font-family:Arial, sans-serif;font-size:14px;font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;}
.tg .tg-baqh{text-align:center;vertical-align:top}
.tg .tg-amwm{font-weight:bold;text-align:center;vertical-align:top}
</style>
<table class="tg">
<thead>
  <tr>
    <th class="tg-amwm"><span style="font-weight:700;font-style:normal;text-decoration:none;color:#000;background-color:transparent">TaskId</span></th>
    <th class="tg-amwm"><span style="font-weight:700;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Iterations</span></th>
    <th class="tg-amwm"><span style="font-weight:700;font-style:normal;text-decoration:none;color:#000;background-color:transparent">StartTime</span></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">20</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">205</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">54568</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">21</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">1</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">54597</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">16</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">1</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">54709</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">22</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">159</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">54846</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">18</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">204</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">54986</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">24</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">161</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">55689</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">17</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">111</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">55689</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">15</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">1</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">55821</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">19</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">156</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">55880</span></td>
  </tr>
</tbody>
</table>

Таблица 3. Диапазоны номеров обработанных итераций каждой задачи

<style type="text/css">
.tg  {border-collapse:collapse;border-color:#ccc;border-spacing:0;}
.tg td{background-color:#fff;border-color:#ccc;border-style:solid;border-width:1px;color:#333;
  font-family:Arial, sans-serif;font-size:14px;overflow:hidden;padding:10px 5px;word-break:normal;}
.tg th{background-color:#f0f0f0;border-color:#ccc;border-style:solid;border-width:1px;color:#333;
  font-family:Arial, sans-serif;font-size:14px;font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;}
.tg .tg-baqh{text-align:center;vertical-align:top}
.tg .tg-amwm{font-weight:bold;text-align:center;vertical-align:top}
.tg .tg-0lax{text-align:left;vertical-align:top}
</style>
<table class="tg">
<thead>
  <tr>
    <th class="tg-amwm"><span style="font-weight:700;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Идентификатор задачи</span></th>
    <th class="tg-amwm"><span style="font-weight:700;font-style:normal;text-decoration:none;color:#000;background-color:transparent">Диапазоны номеров обработанных итераций</span></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">24</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{1,...,31}, {40,...,55}, {88,...,103}, </span><br><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{120,...,124}, {142,...,173}, {206,...,221}, </span><br><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{266,...,281}, {330,...,345}, {484,...,496} </span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">22</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{32,...,39}, {56,...,87}, {104,...,119}, </span><br><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{126,...,141}, {174,...,189}, {222,...,237}, </span><br><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{250,...,265}, {298,...,313}, {346,...,361}, </span><br><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{993,...,999}</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">15</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{125}</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">20</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{190,...,205}, {238,...,248}, {282,...,297}, </span><br><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{314,...,329}, {362,...,372}, {858,...,992}</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">16</span></td>
    <td class="tg-0lax"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">{249}</span></td>
  </tr>
</tbody>
</table>

По результатам работы программы можно увидеть, что рабочие диапазоны различны. Некоторые задачи состоят из единственной итерации. Но это не является недостатком алгоритма по которому реализован исследуемый метод, а следствием того, что обработка одной итерации представляет собой нетрудоемкую с вычислительной точки зрения процедуры. Так, например, если в целевой метод делегата, представляющий четвертый параметр метода `Parallel.For`, добавить строки:

```csharp
for (int k = 0; k < 1000000; k++)
  Math.Sqrt(k);
```

тем самым существенно усложнив обработку каждой итерации цикла, то можно получить равномерное распределение диапазонов по задачам (табл. 4).

Таблица 4. Показатели `TaskId`, `Iterations`, `StartTime` после усложнения обработки каждой итерации цикла

<style type="text/css">
.tg  {border-collapse:collapse;border-color:#ccc;border-spacing:0;}
.tg td{background-color:#fff;border-color:#ccc;border-style:solid;border-width:1px;color:#333;
  font-family:Arial, sans-serif;font-size:14px;overflow:hidden;padding:10px 5px;word-break:normal;}
.tg th{background-color:#f0f0f0;border-color:#ccc;border-style:solid;border-width:1px;color:#333;
  font-family:Arial, sans-serif;font-size:14px;font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;}
.tg .tg-baqh{text-align:center;vertical-align:top}
.tg .tg-amwm{font-weight:bold;text-align:center;vertical-align:top}
</style>
<table class="tg">
<thead>
  <tr>
    <th class="tg-amwm"><span style="font-style:normal;text-decoration:none;color:#000;background-color:transparent">TaskId</span></th>
    <th class="tg-amwm"><span style="font-style:normal;text-decoration:none;color:#000;background-color:transparent">Iterations</span></th>
    <th class="tg-amwm"><span style="font-style:normal;text-decoration:none;color:#000;background-color:transparent">StartTime</span></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">13</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">79</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">50828</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">10</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">63</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">50849</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">12</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">79</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">51226</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">16</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">79</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">51698</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">15</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">79</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">52224</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">11</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">95</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">52788</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">19</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">108</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">53181</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">17</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">84</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">53640</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">14</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">79</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">53976</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">20</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">32</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">3263706</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">21</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">32</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">3355186</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">22</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">32</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">3462087</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">23</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">32</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">3543335</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">24</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">29</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">3562197</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">25</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">39</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">3575327</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">26</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">29</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">3639235</span></td>
  </tr>
  <tr>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">27</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">29</span></td>
    <td class="tg-baqh"><span style="font-weight:400;font-style:normal;text-decoration:none;color:#000;background-color:transparent">3797790</span></td>
  </tr>
</tbody>
</table>

Таким образом, результаты апробации метода `Parallel.For` показывают, что в ходе повторных запусков программы с данным методом создается различное число задач и рабочих диапазонов, отличных друг от друга. Данное поведение программы при обработке данных типа `float` и `double` приводит к неоднозначности результата выполнения делегата `localFinally`, определяющего финальное действие с локальным результатом каждой задачи.

Чтобы обеспечить высокую точность проводимых вычислений, следует обеспечить переход на тип `decimal`:

```csharp
object locker = new object();
decimal grandTotal = 0;
Parallel.For(1, 10000000,
 () => (decimal)0,
 (i, state, localTotal) =>
 localTotal + (decimal)Math.Sqrt(i),
 localTotal =>
 { lock (locker) grandTotal += localTotal; }
);
grandTotal.Dump();
```

Такой переход сопряжен с накладными расходами по быстродействию программы (при вычислении суммы квадратных корней чисел от 1 до 10<sup>7</sup> на четырехъядерном процессоре Intel Core i5 9300H время выполнения составляет приблизительно 0,260 мсек. при использовании типа `decimal`, в то время как при использовании типа `double` это занимает лишь 0,02 мсек.) и может быть неоправданным из-за отсутствия необходимости в результатах повышенной точности. Однако взамен на выходе обеспечивается однозначный результат: 21081849486,44249240077608.

## Использованный источник

[^1]: Албахари Д., Албахари Б. C# 7.0. Справочник. Полное описание языка.: Пер. с англ. – СпБ.: ООО «Альфа-книга», 2018. (См. главу 2. Основы языка C#, п. Ошибки округления вещественных чисел; главу 23. Параллельное программирование, п. Оптимизация PLINQ, п. Parallel.For и Parallel.ForEach.)