---
layout: post
title: "Неоднозначность результатов при использовании методов класса Parallel в рамках исполняющей среды .NET Framework"
date: 2023-03-22
---
Параллельное программирование – это способ написания программ, которые могут выполняться параллельно на нескольких процессорах или ядрах. Это позволяет программам обрабатывать большие объемы данных или выполнить более сложные вычисления за приемлемое время, чем это было бы возможно на одном процессоре. Преимущества параллельного программирования: увеличение производительности, распределение нагрузки, обработка больших объемов данных, улучшение отзывчивости, увеличение надежности. В целом, параллельное программирование имеет множество преимуществ, которые могут помочь улучшить производительность и надежность программных систем, особенно в условиях растущей сложности вычислительных задач и объемов данных. Однако параллельное программирование также может иметь свои сложности, связанные с управлением синхронизацией, гонками данных и другими аспектами, которые требуют дополнительного внимания и опыта со стороны программиста. В ходе тестирования параллельных программ можно получить неоднозначные результаты. Например, это может происходить, когда мы оптимизируем объединение данных типа `float` или `double` посредством методов `For` или `ForEach` класса `Parallel`. Подобное поведение программы заставляет усомниться в потокобезопасности написанного кода. Пост раскрывает возможную причину неоднозначности результатов, получаемых параллельной программой, и предлагает лаконичное решение вопроса.

## Неоднозначность результатов в ходе параллельной агрегации локальных значений

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
- Возвращаемый объект – структура (`ParallelLoopResult`), в которой содержатся сведения о выполненной части цикла.

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
float x = 0.1f;  // Не точно 0.1
Console.WriteLine (x + x + x + x + x + x + x + x + x + x);    // 1.0000001
```

Именно потому типы `float` и `double` не подходят для финансовых вычислений. В противоположность им тип `decimal` работает в десятичной системе счисления, так что он способен точно представлять дробные числа вроде 0.1, выразимые в десятичной системе (а также в системах счисления с основаниями-множителями 10 – двоичной и пятеричной). Поскольку вещественные литералы являются десятичными, тип `decimal` может точно представлять такие числа, как 0.1. Тем не менее, ни `double`, ни `decimal` не могут точно представлять дробное число с периодическим десятичным представлением:

```csharp
decimal m = 1M / 6M; // 0.1666666666666666666666666667M
double d = 1.0 / 6.0; // 0.16666666666666666
```

Это приводит к накапливающимся ошибкам округления:

```csharp
decimal notQuiteWholeM = m+m+m+m+m+m; // 1.0000000000000000000000000002M
double notQuiteWholeD = d+d+d+d+d+d; // 0.99999999999999989
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
.tg .tg-c3ow{border-color:inherit;text-align:center;vertical-align:top}
.tg .tg-0pky{border-color:inherit;text-align:left;vertical-align:top}
.tg .tg-c6of{background-color:#ffffff;border-color:inherit;text-align:left;vertical-align:top}
</style>
<table class="tg">
<thead>
  <tr>
    <th class="tg-c3ow"><span style="font-weight:bold">Характеристика</span></th>
    <th class="tg-c3ow"><span style="font-weight:bold">double</span></th>
    <th class="tg-c3ow"><span style="font-weight:bold">decimal</span></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td class="tg-0pky">Внутреннее представление</td>
    <td class="tg-0pky">Двоичное</td>
    <td class="tg-0pky">Десятичное</td>
  </tr>
  <tr>
    <td class="tg-0pky">Десятичная точность</td>
    <td class="tg-0pky">15–16 значащих цифр</td>
    <td class="tg-0pky">28–29 значащих цифр</td>
  </tr>
  <tr>
    <td class="tg-0pky">Диапазон</td>
    <td class="tg-c6of"><p>&plusmn;(~1<span style="font-weight: 400;">0</span><sup><span style="font-weight: 400;">-324</span></sup><span style="font-weight: 400;">&ndash;~</span><span style="font-weight: 400;">10</span><span style="font-weight: 400;"><sup>308</sup>)</span></p></td>
    <td class="tg-0pky"><p>&plusmn;(~1<span style="font-weight: 400;">0</span><sup><span style="font-weight: 400;">-28</span></sup><span style="font-weight: 400;">&ndash;~</span><span style="font-weight: 400;">10</span><span style="font-weight: 400;"><sup>28</sup>)</span></p></td>
  </tr>
  <tr>
    <td class="tg-0pky">Специальные значения</td>
    <td class="tg-0pky">+0, -0, +∞, -∞ и NaN</td>
    <td class="tg-0pky">Отсутствуют</td>
  </tr>
  <tr>
    <td class="tg-0pky">Скорость обработки</td>
    <td class="tg-0pky">Присущая процессору</td>
    <td class="tg-0pky">Не присущая процессору (примерно<br /> в 10 раз медленнее, чем в случае double)</td>
  </tr>
</tbody>
</table>

Раскроем тип decimal более детально, чтобы ответить на вопрос, почему обработка данных типа decimal не является присущей процессору. 

Двоичное представление decimal числа состоит из 1-битового знака, 96-битового целого числа и коэффициента масштабирования, используемого для деления целочисленного числа и указания его части десятичной дроби. Коэффициент масштабирования неявно представляет собой число 10, возведенное в степень в диапазоне от 0 до 28. Таким образом, bits – это массив из четырех элементов, состоящий из 32-разрядных целых чисел со знаком:

- its[0], bits[1] и bits[2] содержат низкие, средние и высокие 32 биты 96-разрядного целого числа.
- bits[3]:
  - 0-15 не используются;
  - 16-23 (8 бит) содержат экспоненту от 0 до 28, что указывает на степень 10 для деления целочисленного числа;
  - 24-30 не используются;
  - 31 содержит знак (0 означает положительное значение, а 1 – отрицательное).

Разбиение на основе порций работает путем предоставления каждому рабочему потоку возможности периодически захватывать из входной последовательности небольшие “порции” элементов с целью их обработки. Например (рис. 1), инфраструктура Parallel LINQ начинает с выделения очень маленьких порций (один или два элемента за раз) и затем по мере продвижения запроса увеличивает размер порции: это гарантирует, что небольшие последовательности будут эффективно распараллеливаться, а крупные последовательности не приведут к чрезмерным циклам полного обмена. Если рабочий поток получает “простые” элементы (которые обрабатываются быстро), то в конечном итоге он сможет получить больше порций. Такая система сохраняет каждый поток одинаково занятым (а процессорные ядра “сбалансированными”); единственный недостаток состоит в том, что извлечение элементов из разделяемой входной последовательности требует синхронизации – и в результате могут появиться некоторые накладные расходы и состязания.

![](\pastes\2023-03-23-04-44-43.png)  
Рисунок 1. Разделение на основе порций

Метод `For` класса `Parallel` работает схожим образом, разница лишь в том, что вместо элемента входной последовательности выступает номер итерации, который как правило учитывается при выполнении тела цикла (точнее делегата типа `Action<int>`). Реализация разделения основана на механизме разбиения на порции, при котором размер порции потенциально увеличивается в случае положительной динамики обработки итераций. Такой подход помогает обеспечить качественную балансировку нагрузки при небольшом количестве итераций и минимизировать число монопольных блокировок (в ходе назначения диапазонов номеров итераций для рабочих потоков) при их большом количестве. При этом обеспечивается, чтобы большинство итераций потока было сосредоточено в одной и той же области итерационного пространства для достижения высокой локальности кэша.

## Исследование метода Parallel.For для детализации причины неоднозначности конечного результата

Реализация метода `For` сложна и требует детального рассмотрения, которое выходит за рамки данной статьи. Тем не менее отметим некоторые моменты программной реализации метода Parallel.For с аргументом обобщенного типа.

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
      //…
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
Метод rootTask.RunSynchronously запускает исполнение задач в рабочих потоках пула, при этом число задач задается свойством parallelOptions.EffectiveMaxConcurrencyLevel. Метод FindNewWork32 определяет рабочий диапазон для каждого потока пула. В представленном коде можно увидеть, что выполнение любой задачи не ограничивается выполнением первоначально определенного диапазона, потоки пула продолжают работу для вновь задаваемых диапазонов в операторе while.

Проведем детализацию работы метода Parallel.For с аргументом обобщенного типа на ранее представленном примере по суммированию квадратных корней чисел, расширив код следующим образом.

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

Программный код позволяет учесть:

- идентификатор каждой задачи TaskId; 
- количество итераций выполненное в рамках каждой задачи Iterations; 
- StartTime – время начала работы каждой задачи, выраженное в тиках посредством класса Stopwatch (один тик является меньше одной микросекунды);
- диапазоны номеров обработанных итераций каждой задачи.

Например, по результатам работы программы на машине, способной выполнять 8 потоков параллельно на аппаратном уровне, можно получить следующие показатели TaskId, Iterations, StartTime (табл. 2). Диапазоны номеров обработанных итераций представлены в таблице 3. 