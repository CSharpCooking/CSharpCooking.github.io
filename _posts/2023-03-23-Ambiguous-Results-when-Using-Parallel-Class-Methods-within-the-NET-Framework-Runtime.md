---
layout: post
title: "Неоднозначность результатов при использовании методов класса Parallel в рамках исполняющей среды .NET Framework"
date: 2023-03-22
---
Параллельное программирование – это способ написания программ, которые могут выполняться параллельно на нескольких процессорах или ядрах. Это позволяет программам обрабатывать большие объемы данных или выполнить более сложные вычисления за приемлемое время, чем это было бы возможно на одном процессоре. Преимущества параллельного программирования: увеличение производительности, распределение нагрузки, обработка больших объемов данных, улучшение отзывчивости, увеличение надежности. В целом, параллельное программирование имеет множество преимуществ, которые могут помочь улучшить производительность и надежность программных систем, особенно в условиях растущей сложности вычислительных задач и объемов данных. Однако параллельное программирование также может иметь свои сложности, связанные с управлением синхронизацией, гонками данных и другими аспектами, которые требуют дополнительного внимания и опыта со стороны программиста. В ходе тестирования параллельных программ можно получить неоднозначные результаты. Например, это может происходить, когда мы оптимизируем объединение данных типа `float` или `double` посредством методов `For` или `ForEach` класса `Parallel`. Подобное поведение программы заставляет усомниться в потокобезопасности написанного кода. Пост раскрывает возможную причину неоднозначности результатов, получаемых параллельной программой, и предлагает лаконичное решение вопроса.

## **Неоднозначность результатов в ходе параллельной агрегации локальных значений**

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

Таблица 1. Отличия между типами `double` и `decimal`  21-40  
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