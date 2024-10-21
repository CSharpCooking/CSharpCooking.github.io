---
title: "Освобождение \"ненужных\" объектов при использовании LazyInitializer"
description: "Ленивая инициализация это способ доступа к объекту, скрывающий за собой механизм, позволяющий отложить создание этого объекта до момента первого обращения к нему. Класс LazyInitializer обеспечивает ленивую инициализацию разделяемого поля в безопасной к потокам манере. Однако, в ходе применения данного класса могут создаваться лишние объекты. В посте предлагается решение по освобождению "ненужных" объектов, которые создаются в ходе применения класса LazyInitializer для ленивой инициализации в многопоточном сценарии."
author: RuslanGibadullin
date: 2021-04-05
categories: [Статьи]
tags: [LazyInitializer, ленивая инициализация, многопоточность]
---

## Проблематика [^1]

Частой проблемой в области многопоточности является определение способа ленивой инициализации разделяемого поля в безопасной к потокам манере. Такая потребность возникает при наличии поля, которое относится к типу, затратному в плане конструирования:

```csharp
class A
{
  public readonly Expensive Expensive = new Expensive();
  //...
}
class Expensive { }
```

Проблема с показанным кодом заключается в том, что создание экземпляра `A` оказывает влияние на производительность из-за создания экземпляра класса `Expensive`, причем независимо от того, будет позже осуществляться доступ к полю `Expensive` или нет. Очевидное решение предусматривает конструирование экземпляра по требованию:

```csharp
class A
{
  Expensive _expensive;
  public Expensive Expensive
  {
    get
    {
      if (_expensive == null) 
        _expensive = new Expensive();
      return _expensive;
    }
  }
  //...
}
```

Здесь возникает вопрос: является ли такой код безопасным в отношении потоков? Давайте подумаем, что произойдет, если два потока обратятся к свойству `Expensive` одновременно. Они оба могут дать `true` в условии оператора `if`, и каждый поток в конечном итоге получит другой экземпляр `Expensive`. Поскольку это может привести к возникновению тонких ошибок, в общем можно было бы сказать, что код не является безопасным к потокам.

Упомянутая проблема решается применением блокировки к коду проверки и инициализации объекта:

```csharp
class A
{
  Expensive _expensive;
  readonly object _expenseLock = new object();
  public Expensive Expensive
  {
    get
    {
      lock (_expenseLock)
      {
        if (_expensive == null) 
          _expensive = new Expensive();
        return _expensive;
      }
    }
  }
}
```

Начиная с версии `.NET Framework 4.0`, стал доступным класс `Lazy<T>`, который помогает обеспечивать ленивую инициализацию. В случае создания его экземпляра с аргументом `true` он реализует только что описанный шаблон инициализации, безопасной в отношении потоков.

> Класс `Lazy<T>` на самом деле реализует микро-оптимизированную версию этого шаблона, которая называется блокированием с двойным контролем. Блокирование с двойным контролем выполняет дополнительное volatile-чтение, чтобы избежать затрат на получение блокировки, если объект уже инициализирован.

> В многопроцессорной системе операция volatile-запись гарантирует, что значение, записываемое в область памяти, сразу же становится видимым для всех процессоров. Операция volatile-чтение получает самое последнее значение, записанное в область памяти любым процессором. Для этих операций может потребоваться очистка кэша процессора, что может повлиять на производительность.

> Например, запустив релиз нижеуказанного кода, можно увидеть, что программа не завершается за приемлемый временный промежуток.

  ```csharp
  class Program
  {
    static bool finish = false;
    static void Main()
    {
      new Thread(ThreadProc).Start();
      int x = 0;
      while (!finish)
        x++;
    }
    static void ThreadProc()
    {
      Thread.Sleep(1000);
      finish = true;
    }
  }
  ```

> Почему так? Дело в том, что компилятор, среда CLR и процессор имеют право изменять порядок следования инструкций и кешировать переменные в регистрах центрального процессора в целях улучшения производительности – до тех пор, пока такие оптимизации не изменяют поведение однопоточной программы (или многопоточной программы, в которой используются блокировки). В представленном примере оптимизатор определил, что переменная `finish` с точки зрения основного потока не меняется, и указал ограничиться чтением значения из регистра. А так как в условиях многопоточного программирования невозможно прогнозировать число операций чтения устаревших данных, то получаем непредсказуемый результат. Решением является применение volatile-операций.

  ```csharp
  class Program
  {
    static bool finish = false;
    static void Main()
    {
      new Thread(ThreadProc).Start();
      int x = 0;
      while (!Volatile.Read(ref finish))
        x++;
    }
    static void ThreadProc()
    {
      Thread.Sleep(1000);
      Volatile.Write(ref finish, true);
    }
  }
  ```

Для использования `Lazy<T>` создайте его экземпляр с делегатом фабрики значений, который сообщает, каким образом инициализировать новое значение, и аргументом `true`. Затем получайте доступ к его значению через свойство `Value`:

```csharp
Lazy<Expensive> _expensive =
  new Lazy<Expensive>(() => new Expensive(), true);

public Expensive Expensive
{
  get { return _expensive.Value; }
}
```

Если конструктору класса `Lazy<T>` передать `false`, тогда он реализует шаблон ленивой инициализации, небезопасной к потокам, который был описан в начале – это имеет смысл, когда класс `Lazy<T>` необходимо применять в однопоточном контексте.

`LazyInitializer` – статический класс, который работает в точности как `Lazy<T>` за исключением перечисленных ниже моментов.
- Его функциональность открыта через статический метод, который оперирует прямо на поле вашего типа, что позволяет избежать дополнительного уровня косвенности, улучшая производительность в ситуациях, когда нужна высшая степень оптимизации.
- Он предлагает другой режим инициализации, при котором множество потоков могут состязаться за инициализацию.

Чтобы использовать класс `LazyInitializer`, перед доступом к полю необходимо вызвать его метод `EnsureInitialized`, передав ему ссылку на поле и фабричный делегат:

```csharp
Expensive _expensive;
public Expensive Expensive
{
  get 
  { 
    LazyInitializer.EnsureInitialized
      (ref _expensive, () => new Expensive());
    return _expensive;
  }
}
```

В случае, если к методу `EnsureInitialized` одновременно обращаются несколько потоков, может быть создано несколько экземпляров класса `Expensive`, но только один будет сохранен в `_expensive`.

```csharp
public static T EnsureInitialized<T>
  (ref T target, Func<T> valueFactory) 
{
  if (Volatile.Read(ref target) != null)
  {
    return target;
  }
  return EnsureInitializedCore(ref target, valueFactory);
}
private static T EnsureInitializedCore<T>
  (ref T target, Func<T> valueFactory)
{
  T val = valueFactory();
  if (val == null)
  {
    throw new InvalidOperationException(…);
  }
  Interlocked.CompareExchange(ref target, val, null);
  return target;
}
```

Однако, данный метод не будет удалять объекты, которые не были сохранены. Таким образом, если инициализатор создает объект, требующий освобождения, то ставший "ненужным" такой объект не сможет быть освобожден без написания дополнительной логики. 

Ниже предлагается решение по освобождению ресурсов, выделенных в ходе выполнения такой инициализации.

## Решение

Решение связано с применением метода `ThreadPool.RegisterWaitForSingleObject` в фабричном делегате. Данный метод запускает делегат в пуле потоков, в котором вызывается `Dispose` "ненужного" объекта, когда дескриптор ожидания `_starter` сигнализируется. Следует отметить, что простота реализации обеспечивается захватом внешних переменных лямбда-выражением.

```csharp
static ManualResetEvent _starter = 
  new ManualResetEvent(false);
const int N = 4;
static void Main()
{
  A a = new A();
  Thread[] T = new Thread[N];
  for (int i = 0; i < N; i++)
  {
    T[i] = new Thread(() => 
      { Expensive e = a.Expensive; });
    T[i].Start();
  }
  for (int i = 0; i < N; i++)
    T[i].Join();
  _starter.Set();
  Thread.Sleep(1000); // some work
}
class A
{
  Expensive _expensive;
  public Expensive Expensive
  {
    get
    {
      LazyInitializer.EnsureInitialized
        (ref _expensive, () =>
      {
        Expensive e = new Expensive();
        var tokenReady = new ManualResetEventSlim();
        RegisteredWaitHandle reg = null;
        reg = ThreadPool.RegisterWaitForSingleObject
        (_starter,
        (data, timeOut) =>
        {
          tokenReady.Wait();
          tokenReady.Dispose();
          if (e != _expensive) e.Dispose();
          else e.NotDispose();
          reg.Unregister(_starter);
        },
        null, -1, true);
        tokenReady.Set();
        return e;
      });
      return _expensive;
    }
  }
}
class Expensive : IDisposable
{
  public void Dispose()
  {
    Console.WriteLine("Object with hash code " + 
      $"{this.GetHashCode().ToString()} is disposed.");
  }
  public void NotDispose()
  {
    Console.WriteLine($"Object with hash code " +
      $"{this.GetHashCode().ToString()} not disposed.");
  }
}
```

Обратите внимание, что в программе использовался другой дескриптор ожидания `tokenReady`, чтобы избежать освобождения неуправляемого дескриптора обратного вызова (посредством метода `Unregister`) до присваивания переменной `reg` признака регистрации.

## Использованный источник

[^1]: Албахари Д., Албахари Б. C# 7.0. Справочник. Полное описание языка.: Пер. с англ. – СпБ.: ООО «Альфа-книга», 2018. – С. 877-879. (См. главу 22. Расширенная многопоточность, п. Ленивая инициализация.)