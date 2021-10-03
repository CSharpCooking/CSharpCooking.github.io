# Диагностика [↩︎](/trainings)

Когда что-то пошло не так, важно иметь доступ к информации, которая поможет в диагностировании проблемы. Существенную помощь в этом оказывает интегрированная среда разработки или отладчик, но он обычно доступен только на этапе разработки. После поставки приложение обязано самостоятельно собирать и фиксировать диагностическую информацию.

<a name="0"/>**Содержание**

1. [Условная компиляция](#1)  
2. [Классы Debug и Trace](#2)
3. [Интеграция с отладчиком](#3)
4. [Процессы и потоки процессов](#4)
5. [StackTrace и StackFrame](#5)

---

## <a name="1"/>1. Условная компиляция [↩︎](#0)

С помощью директив препроцессора любой раздел кода C# можно компилировать условно. Директивы препроцессора представляют собой специальные инструкции для компилятора, которые начинаются с символа `#` (и в отличие от других конструкций C# должны полностью располагаться в одной строке). Логически они выполняются перед основной компиляцией (хотя на практике компилятор обрабатывает их во время фазы лексического анализа). Директивами препроцессора для условной компиляции являются `#if`, `#else`, `#endif` и `#elif`.

Директива `#if` указывает компилятору на необходимость игнорирования раздела кода, если не определен специальный символ. Определить такой символ можно либо с помощью директивы `#define`, либо посредством ключа компиляции.

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

Условная компиляция способна решать задачи, которые нельзя решить посредством переменных-флагов, например:
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

Атрибут `Conditional` указывает компилятору на необходимость игнорирования любых обращений к определенному классу или методу, если заданный символ не был определен.

```csharp
static void LogStatus(string msg)
{
  string logFilePath = . . .
  System.IO.File.AppendAllText(logFilePath, msg + "\r\n");
}
---
#if LOGGINGMODE // Постоянно писать такой код утомительно
  LogStatus ("Message Headers: " + GetMsgHeaders());
#endif
---
[Conditional("LOGGINGMODE")] // Идеальное решение
static void LogStatus(string msg)
{
  ...
}
```

В результате компилятор трактует вызовы `LogStatus`, как если бы они были помещены внутрь директивы `#if LOGGINGMODE`. Когда символ не определен, любые обращения к методу `LogStatus` полностью исключаются из компиляции, в том числе и выражения оценки его аргумента.

### 1.3. Альтернатива атрибуту Conditional

Атрибут `Conditional` бесполезен, когда во время выполнения необходима возможность динамического включения или отключения функциональности: вместо него должен применяться подход на основе переменных. Остается открытым вопрос о том, как элегантно обойти оценку аргументов при вызове условных методов регистрации. Проблема решается с помощью функционального подхода:

```csharp
class Program
{
  public static bool EnableLogging;
  static void LogStatus(Func<string> message)
  {
    string logFilePath = ...
    if (EnableLogging)
      System.IO.File.AppendAllText(logFilePath, message() + "\r\n");
  }
}
---
LogStatus(() => "Message Headers: " + GetComplexMessageHeaders());
```

## <a name="2"/>2. Классы Debug и Trace [↩︎](#0)

`Debug` и `Trace` – статические классы, которые предлагают базовые возможности регистрации и утверждений. Указанные два класса очень похожи; основное отличие связано с тем, для чего они предназначены. Класс `Debug` предназначен для отладочных сборок, а класс `Trace` – для отладочных и окончательных сборок. Чтобы достичь таких целей:

- все методы класса `Debug` определены с атрибутом `[Conditional ("DEBUG")]`;
- все методы класса `Trace` определены с атрибутом `[Conditional ("TRACE")]`.

Все обращения к `Debug` или `Trace` исключаются компилятором, если только не определен символ `DEBUG` или `TRACE`. 

По умолчанию в Visual Studio определены оба символа, DEBUG и TRACE, в конфигурации отладки и один лишь символ TRACE в конфигурации выпуска.

### 2.1. Методы Fail и Assert

```csharp
Debug.Fail ("File data.txt does not exist!");
Debug.Assert (File.Exists ("data.txt"), "File data.txt does not exist!");
```

![2021-10-01-18-03-58](pastes/2021-10-01-18-03-58.png)

Утверждение представляет собой то, что в случае нарушения говорит об ошибке в коде текущего метода. Генерация исключения на основе проверки достоверности аргумента указывает на ошибку в коде вызывающего компонента.

### 2.2. TraceListener

Классы `Debug` и `Trace` имеют свойство `Listeners`, которое является статической коллекцией экземпляров `TraceListener`. 

Прослушиватели трассировки можно написать с нуля (создавая подкласс класса `TraceListener`) или воспользоваться одним из предопределенных типов:
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

> Если применяются прослушиватели, основанные на потоках или файлах, то эффективной политикой будет установка в `true` свойства `AutoFlush` для экземпляров `Debug` и `Trace`. Иначе при возникновении исключения или критической ошибки последние 4 Кбайт диагностической информации могут быть утеряны.

## <a name="3"/>3. Интеграция с отладчиком [↩︎](#0)

Иногда для приложения удобно взаимодействовать с каким-нибудь отладчиком, если он доступен. На этапе разработки отладчик обычно предоставляется IDE-средой (например, Visual Studio), а после развертывания отладчиком, скорее всего, будет:

- DbgCLR;
- один из низкоуровневых инструментов отладки, такой как WinDbg, Cordbg или Mdbg.

Инструмент DbgCLR является усеченной версией Visual Studio, в которой оставлен только отладчик, и он свободно загружается в составе .NET Framework SDK. Это простейший вариант отладки при отсутствии доступа к IDE-среде, хотя он требует загрузки полного SDK.

### 3.1. Присоединение и останов

Статический класс `Debugger` из пространства имен` System. Diagnostics` предлагает базовые функции для взаимодействия с отладчиком, а именно – `Break`, `Launch`, `Log` и `IsAttached`.

Для отладки к приложению сначала потребуется присоединить отладчик. В случае запуска приложения из IDE-среды отладчик присоединяется автоматически, если только не запрошено противоположное (выбором пункта меню Start without debugging (Запустить без отладки)).

> Начиная с платформа .NET Framework 4, среда выполнения больше не выполняет тщательный контроль над запуском отладчика для `Break` метода, а вместо этого сообщает об ошибке подсистеме отчеты об ошибках Windows (WER). Если необходимо, чтобы отладчик запускался независимо от параметров WER, обязательно вызовите `Launch` метод.

### 3.2. Атрибуты отладчика

Атрибуты `DebuggerStepThrough` и `DebuggerHidden` предоставляют указания отладчику о том, как обрабатывать пошаговое выполнение для конкретного метода, конструктора или класса.

```csharp
[DebuggerStepThrough, DebuggerHidden]
void DoWorkProxy()
{
  // Настройка...
  DoWork();
  // Освобождение...
}
void DoWork() {...} // Реальный метод...
```

## <a name="4"/>4. Процессы и потоки процессов [↩︎](#0)

Класс `Process` также позволяет запрашивать и взаимодействовать с другими процессами, выполняющимися на том же самом или другом компьютере.

### 4.1. Исследование выполняющихся процессов

Методы `Process.GetProcessXXX` извлекают специфический процесс по имени либо идентификатору или все процессы, выполняющиеся на текущей либо указанной машине.

```csharp
foreach (Process р in Process.GetProcesses())
  using (р)
  {
    Console.WriteLine(р.ProcessName);
    Console.WriteLine(" PID: " + р.Id); // Идентификатор процесса
    Console.WriteLine(" Memory: " + р.WorkingSet64); // Память
    Console.WriteLine(" Threads: " + р.Threads.Count); //Количество потоков
  }
```

### 4.2. Исследование потоков в процессе

С помощью свойства `Process.Threads` можно также реализовать перечисление потоков других процессов.

```csharp
public void EnumerateThreads(Process p)
{
  foreach (ProcessThread pt in p.Threads)
  {
    Console.WriteLine(pt.Id);
    Console.WriteLine(" State: " + pt.ThreadState); // Состояние
    Console.WriteLine(" Priority: " + pt.PriorityLevel); // Приоритет
    Console.WriteLine(" Started: " + pt.StartTime); // Запущен
    Console.WriteLine(" CPU time: " + pt.TotalProcessorTime); // Время ЦП
  }
}
```

Объект `ProcessThread` предоставляет диагностическую информацию о лежащем в основе потоке.

## <a name="5"/>5. StackTrace и StackFrame [↩︎](#0)

Классы `StackTrace` и `StackFrame` предлагают допускающее только чтение представление стека вызовов и являются частью стандартной инфраструктуры .NET Framework для настольных приложений. Трассировки стека можно получать для текущего потока, другого потока в том же самом процессе или объекта `Exception`. Экземпляр `StackTrace` представляет полный стек вызовов, a `StackFrame` – одиночный вызов метода внутри стека.

После получения экземпляра `StackTrace` можно исследовать любой отдельный фрейм с помощью вызова метода `GetFrame` или же все фреймы посредством вызова `GetFrames`:

```csharp
static void Main() { A(); }
static void A() { В(); }
static void В() { C(); }
static void C()
{
  StackTrace s = new StackTrace(true);
  Console.WriteLine("Total frames: " + s.FrameCount); // Всего фреймов
  Console.WriteLine("Current method: " + s.GetFrame(0).GetMethod().Name);
					// Текущий метод
  Console.WriteLine("Calling method: " + s.GetFrame(1).GetMethod().Name);
					// Вызывающий метод
  Console.WriteLine("Entry method: " + s.GetFrame
					// Входной метод
  (s.FrameCount - 1).GetMethod().Name);
  Console.WriteLine("Call Stack:");
					// Стек вызовов
  foreach (StackFrame f in s.GetFrames())
    Console.WriteLine(
    " File: " + f.GetFileName() + /* Файл */
    " Line: " + f.GetFileLineNumber() + /* Строка */
    " Col: " + f.GetFileColumnNumber() + /* Колонка */
    " Offset: " + f.GetILOffset() + /* Смещение */
    " Method: " + f.GetMethod().Name); /* Метод */
}
```

Ниже показан вывод:

```
Total frames: 4
Current method: C
Calling method: В
Entry method: Main
Call stack:
  File: C:\Test\Program.cs
  File: C:\Test\Program.cs
  File: C:\Test\Program.cs
  File: C:\Test\Program.cs
  Line: 15 Col: 4 Offset: 7 Method: C
  Line: 12 Col: 22 Offset: 6 Method: В
  Line: 11 Col: 22 Offset: 6 Method: A
  Line: 10 Col: 25 Offset: 6 Method: Main
```

Пример с применением объекта `Exception`:

```csharp
static Exception threadEx;
static void Main()
{
  Thread worker = new Thread(DoWork);
  worker.Start();
  worker.Join();
  if (threadEx != null) 
  {
    StackTrace trace = new StackTrace(threadEx);
    Console.WriteLine(trace);
  }
}
static void DoWork()
{
  try
  {
    throw new Exception("Boom!");
  }
  catch (Exception ex) 
  {
    threadEx = ex;
  }
}
```