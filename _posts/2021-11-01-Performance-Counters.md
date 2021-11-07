---
layout: post
title: "Счетчики производительности"
date: 2021-11-01
---
Счетчики производительности сгруппированы в категории, такие как «Система», «Процессор», «Память .NET CLR» и т.д. Примерами счетчиков производительности в категории «Память .NET CLR» могут быть «% времени сборки мусора», «# байтов во всех кучах» и «Выделено байтов/с». Каждая категория может дополнительно иметь один или более экземпляров, допускающих независимый мониторинг. Это полезно, например, для счетчика производительности «% процессорного времени» из категории «Процессор», который позволяет отслеживать утилизацию центрального процессора. На многопроцессорной машине данный счетчик поддерживает экземпляры для всех процессоров, позволяя независимо проводить мониторинг использования каждого процессора. В посте демонстрируются программы по перечислению и чтению доступных счетчиков производительности, а также по созданию специализированных счетчиков и записи в них данных.

## Перечисление доступных счетчиков производительности

```csharp
PerformanceCounterCategory[] cats =
  PerformanceCounterCategory.GetCategories();
foreach (PerformanceCounterCategory cat in cats)
{
  Console.WriteLine("Category: " + cat.CategoryName); // Категория
  string[] instances = cat.GetInstanceNames();
  if (instances.Length == 0)
  {
    foreach (PerformanceCounter ctr in cat.GetCounters())
      Console.WriteLine(" Counter: " + ctr.CounterName); // Счетчик
  }
  else // Вывести счетчики, имеющие экземпляры
  {
    foreach (string instance in instances)
    {
      Console.WriteLine(" Instance: " + instance); // Экземпляр
      if (cat.InstanceExists(instance))
        foreach (PerformanceCounter ctr in cat.GetCounters(instance))
          Console.WriteLine(" Counter: " + ctr.CounterName); // Счетчик
    }
  }
}
```

> Результат содержит свыше 10 000 строк! В реальной системе настолько детальная информация извлекается только по требованию.

В приведенном далее примере с помощью запроса LINQ извлекаются лишь счетчики производительности, связанные с .NET, а результат помещается в XML-файл:

```csharp
var x =
  new XElement("counters",
	from PerformanceCounterCategory cat in
	  PerformanceCounterCategory.GetCategories()
	where cat.CategoryName.StartsWith(".NET")
	let instances = cat.GetInstanceNames()
	select new XElement("category",
	  new XAttribute("name", cat.CategoryName),
	  instances.Length == 0
	  ?
	    from c in cat.GetCounters()
	    select new XElement("counter",
		  new XAttribute("name", c.CounterName))
	  :
	    from i in instances
	    select new XElement("instance", new XAttribute("name", i),
		  !cat.InstanceExists(i)
		  ?
		    null
		  :
		    from c in cat.GetCounters(i)
		    select new XElement("counter",
			  new XAttribute("name", c.CounterName))
	    )
	)
  );
x.Save("counters.xml");
```

## Чтение данных счетчиков производительности

Чтобы извлечь значение счетчика производительности, необходимо создать объект `PerformanceCounter` и затем вызвать его метод `NextValue` или `NextSample`. Метод `NextValue` возвращает простое значение типа `float`, а метод `NextSample` – объект `CounterSample`, который открывает доступ к более широкому набору свойств наподобие `CounterFrequency` (частота счетчика), `RawValue` (начальное значение счетчика) и др.

Конструктор `PerformanceCounter` принимает имя категории, имя счетчика и необязательный экземпляр. Таким образом, чтобы отобразить сведения о текущей утилизации всех процессоров, потребуется написать следующий код:

```csharp
using (PerformanceCounter pc =
  new PerformanceCounter("Processor","% Processor Time","_Total"))
{
  Console.WriteLine(pc.NextValue());
  Thread.Sleep(500);
  Console.WriteLine(pc.NextValue());
}
```
> Первый вызов `NextValue` всегда будет возвращать 0.0, так как вычисляемое значение счетчика зависит от двух состояний. Поэтому необходимо задать задержку между вызовами метода `NextValue`, чтобы позволить счетчику выполнить следующее добавочное чтение.

Класс `PerformanceCounter` не открывает доступ к событию `ValueChanged`, поэтому для отслеживания изменений потребуется реализовать опрос. В следующем примере опрос производится каждые 200 миллисекунд – пока не поступит сигнал завершения от `EventWaitHandle`:

```csharp
// Необходимо импортировать пространства имен 
// System.Threading и System.Diagnostics
static void Monitor(string category, string counter, 
                    string instance, EventWaitHandle stopper)
{
  if (!PerformanceCounterCategory.Exists(category))
    throw new InvalidOperationException("Category does not exist");
  if (!PerformanceCounterCategory.CounterExists(counter, category))
    throw new InvalidOperationException("Counter does not exist");
  if (instance == null) instance = ""; //"" == экземпляры отсутствуют (не null!)
  if (instance != "" &&
  !PerformanceCounterCategory.InstanceExists(instance, category))
    throw new InvalidOperationException("Instance does not exist");
  float lastValue = 0f;
  using (PerformanceCounter pc = 
         new PerformanceCounter(category, counter, instance))
  {
    while (!stopper.WaitOne(200))
    {
      float value = pc.NextValue();
      if (value != lastValue) // Записывать значение, только
      {                       // если оно изменилось.
        Console.WriteLine(value);
        lastValue = value;
      }
    }
  }
}
void Main()
{
  EventWaitHandle stopper = new ManualResetEvent(false);
  new Thread(() => Monitor("Processor", "% Processor Time", 
                           "_Total", stopper)).Start();
  new Thread(() => Monitor("LogicalDisk", "% Idle Time", 
                           "C:", stopper)).Start();
  Console.WriteLine("Monitoring - press any key to quit");
  Console.ReadKey();
  stopper.Set();
}
```

## Создание счетчиков и запись данных о производительности

Перед записью данных счетчика производительности понадобится создать категорию производительности и счетчик. После того, как счетчик создан, его значение можно обновить, создав экземпляр `PerformanceCounter`, установив его свойство `Readonly` в `false` и затем установив его свойство `RawValue`. Для обновления существующего значения можно также применять методы `Increment` и `IncrementBy`.

```csharp
using System;
using System.Threading;
using System.Diagnostics;

namespace CSharpCooking
{
  class S
  {
    static void CreateCookedProgramsCounter()
    {
      string category = "CSharpCooking";
      string categoryDescription = "CSharpCooking Monitoring";
      string counter1 = "Delicious programs";
      string counterDescription1 = "Number of delicious programs";
      string counter2 = "Bad programs";
      string counterDescription2 = "Number of bad programs";

      if (!PerformanceCounterCategory.Exists(category))
      {
        CounterCreationDataCollection cd = new CounterCreationDataCollection();
        cd.Add(new CounterCreationData(counter1, counterDescription1,
          PerformanceCounterType.NumberOfItems32));
        cd.Add(new CounterCreationData(counter2, counterDescription2,
          PerformanceCounterType.NumberOfItems32));
        PerformanceCounterCategory.Create(category, categoryDescription,
          PerformanceCounterCategoryType.SingleInstance, cd);
      }
    }
    static void WriteCookedProgramsCounter(string category, string counter, int frequency,
                         EventWaitHandle stopper)
    {
      using (PerformanceCounter pc = new PerformanceCounter(category, counter, ""))
      {
        pc.ReadOnly = false;
        pc.RawValue = 0;
        pc.Increment();
        while (!stopper.WaitOne(1000 / frequency))
        {
          pc.Increment(); // or pc.IncrementBy(1);
        }
      }
    }
    static void DeleteCookedProgramsCounter()
    {
      string category = "CSharpCooking";
      if (PerformanceCounterCategory.Exists(category))
        PerformanceCounterCategory.Delete(category);
    }
    static void Main()
    {
      CreateCookedProgramsCounter();
      EventWaitHandle stopper = new ManualResetEvent(false);
      new Thread(() => WriteCookedProgramsCounter("CSharpCooking",
        "Delicious programs", 10, stopper)).Start();
      new Thread(() => WriteCookedProgramsCounter("CSharpCooking",
        "Bad programs", 2, stopper)).Start();
      Console.ReadKey();
      stopper.Set();
      // DeleteCookedProgramsCounter();
    }
  }
}
```

Новые счетчики появятся в инструменте мониторинга производительности Windows при выборе опции Add Counters (Добавить счетчики), как показано на рис. 1.

![](\pastes\2021-10-22-18-11-49.png)  
Рис. 1. Специальные счетчики производительности

Через системный монитор визуально можно проследить динамику изменения счетчиков производительности во времени.

![](\pastes\2021-10-22-18-37-49.png)  
Рис. 2. Динамика изменения счетчика производительности

Если позже понадобится определить дополнительные счетчики в той же самой категории, то старая категория должна быть сначала удалена вызовом метода `PerformanceCounterCategory.Delete`.

> Создание и удаление счетчиков производительности требует наличия административных полномочий.