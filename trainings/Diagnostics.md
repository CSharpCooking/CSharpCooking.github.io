# Диагностика <a name="1"/>
1. [Условная компиляция](#1)  
2. [Классы Debug и Trace](#2)

## 1. Условная компиляция

```csharp
#define TESTMODE
static void Main()
{
#if TESTMODE
	Console.WriteLine("TESTMODE!");
#else 
	Console.WriteLine("NOT TESTMODE!");
#endif
}

```

### 1.1. Почему выбирают условную компиляцию?

- условное включение атрибута;
- изменение типа, объявляемого для переменной;
- переключение между разными пространствами имен или псевдонимами типов в директиве `using`:
  ```csharp
  using TestType =
  #if V2
  MyCompany.Widgets.GadgetV2;
  #else
  MyCompany.Widgets.Gadget;
  #endif
  ```

### 1.2. Атрибут Conditional

```csharp
static void LogStatus(string msg)
{
  string logFilePath = . . .
  System.IO.File.AppendAllText(logFilePath, msg + "\r\n");
}
---
#if LOGGINGMODE
  LogStatus ("Message Headers: " + GetMsgHeaders());
#endif
---
LogStatus ("Message Headers: " + GetComplexMessageHeaders());
---
[Conditional("LOGGINGMODE")]
static void LogStatus(string msg)
{
  ...
}
```

### 1.3. Альтернатива атрибуту Conditional


```csharp
class Program
{
  public static bool EnableLogging;
  static void LogStatus(Func<string> message)
  {
    string logFilePath = ...
    if (EnableLogging)
      System.IO.File.AppendAllText(logFilePath, 	 	message() + "\r\n");
  }
}
---
LogStatus(() => "Message Headers: " + GetComplexMessageHeaders());
```

<a name="2"/>

## 2. Классы Debug и Trace | [Оглавление](#title)<a name="1"/>

- все методы класса Debug определены с атрибутом [Conditional ("DEBUG") ];
- все методы класса Trace определены с атрибутом [Conditional ("TRACE") ].

Все обращения к Debug или Trace исключаются компилятором, если только не определен символ DEBUG или TRACE. 

По умолчанию в Visual Studio определены оба символа, DEBUG и TRACE, в конфигурации отладки и один лишь символ TRACE в конфигурации выпуска.

### 2.1. Методы Fail и Assert

```csharp
Debug.Fail ("File data.txt does not exist!");
Debug.Assert (File.Exists ("data.txt"), "File data.txt does not exist!");
```

![](.pastes\2021-10-01-18-03-58.png)

Утверждение представляет собой то, что в случае нарушения говорит об ошибке **в коде текущего метода**. Генерация исключения на основе проверки достоверности аргумента указывает на ошибку **в коде вызывающего компонента**.

### 2.2. TraceListener

Классы `Debug` и `Trace` имеют свойство `Listeners`, которое является статической коллекцией экземпляров `TraceListener`. 

Прослушиватели трассировки можно написать с нуля (создавая подкласс класса TraceListener) или воспользоваться одним из предопределенных типов:
- `TextWriterTraceListener` записывает в `Stream` или `TextWriter` либо добавляет в файл;
- `EventLogTraceListener` записывает в журнал событий Windows;
- `EventProviderTraceListener` записывает в подсистему трассировки событий для Windows (Event Tracing for Windows – ETW) в Windows Vista и последующих версиях;
- `WebPageTraceListener` записывает на веб-страницу ASP.NET.

```csharp
// Очистить стандартный прослушиватель:
Trace.Listeners.Clear();
// Добавить средство записи, дописывающее в файл trace.txt:
Trace.Listeners.Add(new TextWriterTraceListener("trace.txt"));

// Получить выходной поток Console и добавить его в качестве прослушивателя:
System.IO.TextWriter tw = Console.Out;
Trace.Listeners.Add(new TextWriterTraceListener(tw));

// Настроить исходный файл журнала событий и создать/добавить прослушиватель.
// Метод CreateEventSource требует повышения полномочий до уровня
// администратора, так что это обычно будет делаться при установке приложения.
if (!EventLog.SourceExists("DemoApp"))
EventLog.CreateEventSource("DemoApp", "Application");
Trace.Listeners.Add(new EventLogTraceListener("DemoApp"));
```

### 2.3. Сброс и закрытие прослушивателей

Некоторые прослушиватели, такие как `TextWriterTraceListener`, в итоге производят запись в поток, подлежащий кешированию. Результатом будут два последствия.

- Сообщение может не появиться в выходном потоке или файле немедленно.
- Перед завершением приложения прослушиватель потребуется закрыть (или, по крайней мере, сбросить); в противном случае потеряется все то, что находится в кеше (по умолчанию до 4 Кбайт данных, если осуществляется запись в файл).

Классы `Trace` и `Debug` предлагают статические методы `Close` и `Flush`, которые вызывают `Close` или `Flush` на всех прослушивателях (а эти методы в свою очередь вызывают `Close` или `Flush` на любых лежащих в основе средствах записи и потоках).

> Если применяются прослушиватели, основанные на потоках или файлах, то эффективной политикой будет установка в `true` свойства `AutoFlush` для экземпляров `Debug` и T`race. Иначе при возникновении исключения или критической ошибки последние 4 Кбайт диагностической информации могут быть утеряны.

