---
title: Исключения в задачах
description: "В отличие от потоков задачи без труда распространяют исключения. Таким образом, если код внутри задачи генерирует необработанное исключение (другими словами, если задача отказывает), то это исключение автоматически повторно сгенерируется при вызове метода Wait или доступе к свойству Result класса Task<TResult>. В посте детально раскрывается механизм обработки данных исключений."
author: RuslanGibadullin
date: 2022-03-27
categories: [Статьи]
tags: [Task, обработка исключений, многопоточность]
---

## Исключения

Представим код, в котором запущенная задача генерирует исключение `NullReferenceException`. 

```csharp
Task task = Task.Run (() => { throw null; });
try 
{
  task.Wait();
}
catch (AggregateException aex)
{
  if (aex.InnerException is NullReferenceException)
    Console.WriteLine ("Null!");
  else
    throw;
}
```

Проверить, отказала ли задача, можно без повторной генерации исключения посредством свойств `IsFaulted` и `IsCanceled` класса `Task`. Если оба свойства возвращают `false` , то ошибки не возникали; если `IsCanceled` равно `true`, то для задачи было сгенерировано исключение `OperationCanceledException`; если `IsFaulted` равно `true`, то было сгенерировано исключение другого типа и на ошибку укажет свойство `Exception`.

Так как множество запущенных задач задействуют множество потоков, то вполне возможна одновременная генерация двух и более исключений. Чтобы обеспечить получение сведений обо всех исключениях, они помещаются в контейнер `AggregateException`, свойство `InnerExceptions` которого содержит каждое из перехваченных исключений:

```csharp
Task t1 = new Task(() =>
{
  throw new OutOfMemoryException();
});
Task t2 = new Task(() =>
{
  throw new DivideByZeroException();
});
t1.Start(); t2.Start();
try
{
  Task.WaitAll(t1, t2);
}
catch (AggregateException aex)
{
  foreach (Exception ex in aex.InnerExceptions)
    Console.WriteLine(ex.Message);
}
```

## Flatten и Handle

Класс `AggregateException` предоставляет пару методов для упрощения обработки исключений: `Flatten` и `Handle`.

### Flatten

Если у задачи есть вложенные дочерние задачи, то объект `exc` представляет собой опять тип `AggregateException` и для обработки исключений дочерних задач необходимо во вложенном цикле обрабатывать элементы `exc.InnerExceptions`. Метод `Flatten` объекта `AggregateException` возвращает все исключения, возникнувшие в задачах и вложенных задачах в одном списке, делая более удобной обработку всех исключений.

```csharp
Task[] tasks = new Task[N];
// Объявляем и запускаем задачи
..
// Обработка исключений
try
{
  Task.WaitAll(tasks);
}
catch (AggregateException ae)
{
  foreach (Exception e in ae.Flatten().InnerExceptions)
    Console.WriteLine("Message:{ 0}", e.Message);
}
```

### Handle

Иногда полезно перехватывать исключения только специфических типов, а исключения других типов генерировать повторно. Метод `Handle` класса `AggregateException` предлагает удобное сокращение. Он принимает предикат исключений, который будет запускаться на каждом внутреннем исключении:
```csharp
public void Handle (Func<Exception, bool> predicate)
```
Если предикат возвращает `true`, то считается, что исключение "обработано". После того, как делегат запустится на всех исключениях, произойдет следующее:
- если все исключения были "обработаны" (делегат возвратил `true`), то исключение не генерируется повторно;
- если были исключения, для которых делегат возвратил `false` ("необработанные"), то строится новый объект `AggregateException`, содержащий такие исключения, и затем он генерируется повторно.
Например, приведенный далее код в конечном итоге повторно генерирует другой объект `AggregateException`, который содержит одиночное исключение `NullReferenceException`:

```csharp
var parent = Task.Factory.StartNew (() => 
{
  // We’ll throw 3 exceptions at once using 3 child tasks:
  
  int[] numbers = { 0 };
  
  var childFactory = new TaskFactory
  (TaskCreationOptions.AttachedToParent, TaskContinuationOptions.None);
  
  childFactory.StartNew (() => 5 / numbers[0]);   // Division by zero
  childFactory.StartNew (() => numbers [1]);      // Index out of range
  childFactory.StartNew (() => { throw null; });  // Null reference
});

try { parent.Wait(); }
catch (AggregateException aex)
{
  aex.Flatten().Handle (ex => // Note that we still need to call Flatten
  {
    if (ex is DivideByZeroException)
    {
      Console.WriteLine ("Divide by zero");
      return true;            // This exception is "handled"
    }
    if (ex is IndexOutOfRangeException)
    {
      Console.WriteLine ("Index out of range");
      return true;            // This exception is "handled"   
    }
    return false;    // All other exceptions will get rethrown
  });
}
```

## Исключения и автономные задачи [^1]

В автономных задачах, работающих по принципу "установить и забыть" (для которых не требуется взаимодействие через метод `Wait` или свойство `Result` либо продолжение, делающее то же самое), общепринятой практикой является явное написание кода обработки исключений во избежание молчаливого отказа (в точности, как с фоновым потоком).

Подписаться на необнаруженные исключения на глобальном уровне можно через статическое событие `TaskScheduler.UnobservedTaskException`:

```csharp
static void Main()
{
  TaskScheduler.UnobservedTaskException +=
   (object sender, UnobservedTaskExceptionEventArgs eventArgs) =>
     {
       ((AggregateException)eventArgs.Exception).Handle(ex =>
       {
         Console.WriteLine("Exception: {0}", ex.Message);
         return true;
       });      
     };

  Task.Factory.StartNew(() =>
  {
    throw new ArgumentNullException();
  });

  Task.Factory.StartNew(() =>
  {
    throw new ArgumentOutOfRangeException();
  });

  Thread.Sleep(100);
  GC.Collect();

  Console.WriteLine("Done");
}

// ВЫВОД:
// Exception: Значение не может быть неопределенным.
// Exception: Заданный аргумент находится вне диапазона допустимых значений.
// Done
```

Событие `TaskScheduler.UnobservedTaskException` происходит, когда сборщик мусора обнаруживает, что объект задачи (`Task`), содержащий необработанное исключение, становится недостижимым (то есть к нему больше нет активных ссылок и он подлежит утилизации). Это означает, что исключение, возникшее внутри задачи, не было "наблюдаемым" – другими словами, не было поймано или обработано через механизмы ожидания завершения задачи (`Wait`, `Result`) или через обработку состояний (`ContinueWith` при условии обработки исключения, доступ к свойству `Exception` или проверка `IsFaulted`).

Важные моменты, которые стоит помнить о `TaskScheduler.UnobservedTaskException`:

- Обработчик этого события дает последний шанс обработать исключение, прежде чем оно будет "проигнорировано". Это может быть полезно для логирования ошибок или для предотвращения аварийного завершения приложения из-за необработанных исключений.
- Если исключение не помечено как "наблюдаемое" через обработчик события `UnobservedTaskException` (например, если обработчик не вызывает метод `SetObserved` объекта `UnobservedTaskExceptionEventArgs`), сборщик мусора считает, что исключение не обработано. В зависимости от версии .NET и настроек приложения это может привести к аварийному завершению процесса.

  > В приведенном выше коде, использование `.Handle(...)` внутри обработчика `TaskScheduler.UnobservedTaskException` делает явный вызов `SetObserved` необязательным.
- Событие вызывается асинхронно и не обязательно сразу после того, как задача становится недостижимой. Точное время вызова зависит от того, когда сборщик мусора решит выполнить чистку.

Игнорирование исключений нормально в ситуации, когда исключение только указывает на неудачу при получении результата, который больше не интересует. Например, если пользователь отменяет запрос на загрузку веб-страницы, то мы не должны переживать, если выяснится, что веб­-страница не существует. 

Игнорирование исключений проблематично, когда исключение указывает на ошибку в программе, по двум причинам:
- ошибка может оставить программу в недопустимом состоянии;
- в результате ошибки позже могут возникнуть другие исключения, и отказ от регистрации первоначальной ошибки может затруднить диагностику.

Есть пара интересных нюансов относительно того, какое исключение считать необнаруженным.
- Задачи, ожидающие с указанием тайм-аута, будут генерировать необнаруженное исключение, если ошибки возникают после истечения интервала тайм-аута.
- Действие по проверке свойства `Exception` задачи после ее отказа помечает исключение как обнаруженное.

## Использованный источник

[^1]: Албахари Д., Албахари Б. C# 7.0. Справочник. Полное описание языка.: Пер. с англ. – СпБ.: ООО «Альфа-книга», 2018. – С. 577-578. (См. главу 14. Параллелизм и асинхронность, п. Исключения.)