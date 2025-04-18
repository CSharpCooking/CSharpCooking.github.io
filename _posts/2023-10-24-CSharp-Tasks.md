---
title: "Практикум по языку программирования C#"
description: "Методические указания по выполнению практических заданий по языку программирования C#."
author: RuslanGibadullin
date: 2023-10-24
tags: [программирование, csharp]
---

## Задания

Для выполнения [практикума](https://csharpcooking.github.io/practice/CSharpTestTasks.zip) необходимо реализовать интерфейсы-задания в соответствии с формализованными спецификациями.

Все интерфейсы-задания разделяются на следующие типы:
1. Базовые: ITest0, ITest1, ITest2.
2. Продвинутые: ITest3, … ,ITest9.
    В случае, если интерфейс-задание реализовано не полностью, то следует обеспечить генерацию исключения `NotImplementedException` в пропущенных методах.

Требуется разработать сборку, которая реализует спецификации, указанные в заданных интерфейсах.

## Критерии оценивания

- Количество реализованных базовых и продвинутых интерфейсов-заданий.
- Корректность работы методов при адекватных входных параметрах, устойчивость реализации к неадекватным входным параметрам.
- Работа методов под плотной многопоточной нагрузкой (справедливо для тех методов, у которых это явно упомянуто в комментарии).
- Быстродействие реализации с эталонными замерами.
- Стиль оформления кода.
