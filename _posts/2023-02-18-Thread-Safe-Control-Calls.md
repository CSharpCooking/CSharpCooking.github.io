---
title: "Потокобезопасные вызовы элементов управления в приложениях WPF, UWP и Windows Forms"
description: "Многопоточность может повысить производительность в приложениях Windows Presentation Foundation (WPF), Universal Windows Platform (UWP) и Windows Forms, но доступ к элементам управления не является потокобезопасным. Не потокобезопасный код может стать причиной для серьезных и сложных ошибок. Два или более потока, оказывающих влияние на элемент управления, могут привести к нестабильному состоянию приложения и вызвать условия состязаний. Данный пост посвящен раскрытию темы вызова элементов управления пользовательского интерфейса потокобезопасным образом, в частности объяснению понятия \"контекст синхронизации\"."
author: RuslanGibadullin
date: 2023-02-18
categories: [Статьи]
tags: [контекст синхронизации, многопоточность, SynchronizationContext, потокобезопасные вызовы]
---

## Многопоточность в обогащенных клиентских приложениях [^1]

В приложениях WPF, UWP и Windows Forms выполнение длительных по времени операций в главном потоке снижает отзывчивость приложения, потому что главный поток обрабатывает также цикл сообщений, который отвечает за визуализацию и под­держку событий клавиатуры и мыши. Поэтому в обогащенных клиентских приложениях, где реализуется различный функционал, то и дело приходится сталкиваться с многопоточностью. Популярный подход предусматривает настройку “рабочих” потоков для выполнения длительных по времени операций. Код в рабочем потоке запускает длительную операцию и по ее завершении обновляет пользовательский интерфейс. Тем не менее все обогащенные клиентские приложения поддерживают потоковую модель, в кото­рой элементы управления пользовательского интерфейса могут быть доступны только из создавшего их потока (обычно главного потока пользовательского интерфейса). Нарушение данного правила приводит либо к непредсказуемому поведению, либо к генерации исключения. Последнее можно отключить заданием свойства `Control.CheckForIllegalCrossThreadCalls` значением `false`.

Следовательно, когда нужно обновить пользовательский интерфейс из рабочего потока, запрос должен быть перенаправлен потоку пользовательского интерфейса (формально это называется маршализацией). Вот как это выглядит:
- в приложении WPF вызовите метод `Beginlnvoke` или `Invoke` на объекте `Dispatcher` элемента;
- в приложении UWP вызовите метод `RunAsync` или `Invoke` на объекте `Dispatcher`;
- в приложении Windows Forms вызовите метод `Beginlnvoke` или `Invoke` на элементе управления.

Все упомянутые методы принимают делегат, ссылающийся на метод, который требу­ется запустить. Методы `Beginlnvoke/RunAsync` работают путем постановки этого де­легата в очередь сообщений потока пользовательского интерфейса (та же очередь, которая обрабатывает события, поступающие от клавиатуры, мыши и таймера). Метод `Invoke` делает то же самое, но затем блокируется до тех пор, пока сообщение не будет прочита­но и обработано потоком пользовательского интерфейса. По указанной причине метод Invoke позволяет получить возвращаемое значение из метода. Если возвращаемое зна­чение не требуется, то методы `Beginlnvoke/RunAsync` предпочтительнее из-за того, что они не блокируют вызывающий компонент и не привносят возможность возникно­вения взаимоблокировки.

Вы можете представлять себе, что при вызове метода `Application.Run` выполняется следующий псевдокод:

```csharp
while (приложение не завершено) 
{
  Ожидать появления чего-нибудь в очереди сообщений.
  Что-то получено: к какому виду сообщений оно относится?
    Сообщение клавиатуры/мыши -> запустить обработчик событий.
    Пользовательское сообщение Beginlnvoke -> выполнить делегат.
    Пользовательское сообщение Invoke -> 
        выполнить делегат и отправить результат.
}
```

Цикл такого вида позволяет рабочему потоку маршализовать делегат для выполнения в потоке пользовательского интерфейса.

В целях демонстрации предположим, что имеется окно WPF с текстовым полем по имени `txtMessage`, содержимое которого должно быть обновлено рабочим пото­ком после выполнения длительной задачи (эмулируемой с помощью вызова метода `Thread.Sleep`). Ниже приведен необходимый код:

```csharp
void Main()
{
  new MyWindow().ShowDialog();
}
partial class MyWindow : Window
{
  TextBox txtMessage;  
  public MyWindow()
  {
    InitializeComponent();
    new Thread (Work).Start();
  }  
  void Work()
  {
    Thread.Sleep (5000);           // Simulate time-consuming task
    UpdateMessage ("The answer");
  }  
  void UpdateMessage (string message)
  {
    Action action = () => txtMessage.Text = message;
    Dispatcher.BeginInvoke (action);
  }  
  void InitializeComponent()
  {
    SizeToContent = SizeToContent.WidthAndHeight;
    WindowStartupLocation = WindowStartupLocation.CenterScreen;
    Content = txtMessage = new TextBox { Width=250, Margin=new Thickness (10), Text="Ready" };
  }
}
```

После запуска показанного кода немедленно появляется окно. Спустя пять секунд текстовое поле обновляется. Для случая Windows Forms код будет похож, но только в нем вызывается метод `Beginlnvoke` объекта `Form`:

```csharp
void UpdateMessage (string message)
{
  Action action = () => txtMessage.Text = message;
  this.BeginInvoke (action);
}
```

Допускается иметь множество потоков пользовательского интерфейса, если каж­дый из них владеет своим окном. Основным сценарием может служить приложение с несколькими высокоуровневыми окнами, которое часто называют приложением с однодокументным интерфейсом (Single Document Interface – SDI), например, Microsoft Word. Каждое окно SDI обычно отображает себя как отдельное “приложение” в па­нели задач и по большей части оно функционально изолировано от других окон SDI. За счет предоставления каждому такому окну собственного потока пользовательско­го интерфейса окна становятся более отзывчивыми.

## Контексты синхронизации

В пространстве имен `System.ComponentModel` определен абстрактный класс `SynchronizationContext`, который делает возможным обобщение маршализации потоков. Необходимость в таком обобщении подробно описана в статье Стивена Клири [^2].

В API-интерфейсах для мобильных и настольных приложений (UWP, WPF и Windows Forms) определены и созданы экземпляры подклассов `SynchronizationContext`, которые можно получить через статическое свойство `SynchronizationContext.Current` (при выполнении в потоке пользовательского интерфейса). Захват этого свойства позволяет позже “отправлять” сообщения элемен­там управления пользовательского интерфейса из рабочего потока:

```csharp
partial class MyWindow : Window
{
  TextBox txtMessage;
  SynchronizationContext _uiSyncContext;

  public MyWindow()
  {
    InitializeComponent();
    // Capture the synchronization context for the current UI thread:
    _uiSyncContext = SynchronizationContext.Current;
    new Thread (Work).Start();
  }
  
  void Work()
  {
    Thread.Sleep (5000);           // Simulate time-consuming task
    UpdateMessage ("The answer");
  }
  
  void UpdateMessage (string message)
  {
    // Marshal the delegate to the UI thread:
    _uiSyncContext.Post (_ => txtMessage.Text = message, null);
  }
  
  void InitializeComponent()
  {
    SizeToContent = SizeToContent.WidthAndHeight;
    WindowStartupLocation = WindowStartupLocation.CenterScreen;
    Content = txtMessage = new TextBox { Width=250, Margin=new Thickness (10), Text="Ready" };
  }
}
```

Удобство в том, что один и тот же подход работает со всеми обогащенными API-интерфейсами. Правда не все реализации `SynchronizationContext` гарантируют порядок выполнения делегатов или их синхронизацию (см. таблицу). Реализации `SynchronizationContext` на основе UI этим условиям удовлетворяют, тогда как `ASP.NET SynchronizationContext` обеспечивает только синхронизацию (т.е. фокусируется на синхронизации доступа к общим ресурсам в многопоточной среде, но не гарантирует порядок выполнения асинхронных операций или делегатов).

Таблица – Сводное описание реализаций `SynchronizationContext`  
<style type="text/css">
.tg  {border-collapse:collapse;border-color:#ccc;border-spacing:0;}
.tg td{background-color:#fff;border-color:#ccc;border-style:solid;border-width:1px;color:#333;
  font-family:Arial, sans-serif;font-size:14px;overflow:hidden;padding:10px 5px;word-break:normal;}
.tg th{background-color:#f0f0f0;border-color:#ccc;border-style:solid;border-width:1px;color:#333;
  font-family:Arial, sans-serif;font-size:14px;font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;}
.tg .tg-c3ow{border-color:inherit;text-align:center;vertical-align:top}
.tg .tg-0pky{border-color:inherit;text-align:left;vertical-align:top}
.tg .tg-7btt{border-color:inherit;font-weight:bold;text-align:center;vertical-align:top}
.tg .tg-fymr{border-color:inherit;font-weight:bold;text-align:left;vertical-align:top}
</style>
<table class="tg">
<thead>
  <tr>
    <th class="tg-0pky"></th>
    <th class="tg-7btt">Выполнение делегатов в определенном потоке</th>
    <th class="tg-7btt">Делегаты выполняются по одному за раз</th>
    <th class="tg-7btt">Делегаты выполняются в порядке очереди</th>
    <th class="tg-7btt">Send может напрямую вызывать делегат</th>
    <th class="tg-7btt">Post может напрямую вызывать делегат</th>
  </tr>
</thead>
<tbody>
  <tr>
    <td class="tg-fymr">Windows Forms</td>
    <td class="tg-c3ow">Да</td>
    <td class="tg-c3ow">Да</td>
    <td class="tg-c3ow">Да</td>
    <td class="tg-c3ow">Если вызывается из UI-потока</td>
    <td class="tg-c3ow">Никогда</td>
  </tr>
  <tr>
    <td class="tg-fymr">WPF/Silverlight</td>
    <td class="tg-c3ow">Да</td>
    <td class="tg-c3ow">Да</td>
    <td class="tg-c3ow">Да</td>
    <td class="tg-c3ow">Если вызывается из UI-потока</td>
    <td class="tg-c3ow">Никогда</td>
  </tr>
  <tr>
    <td class="tg-fymr">По умолчанию</td>
    <td class="tg-c3ow">Нет</td>
    <td class="tg-c3ow">Нет</td>
    <td class="tg-c3ow">Нет</td>
    <td class="tg-c3ow">Всегда</td>
    <td class="tg-c3ow">Никогда</td>
  </tr>
  <tr>
    <td class="tg-fymr">ASP.NET</td>
    <td class="tg-c3ow">Нет</td>
      <td class="tg-c3ow">Да<sup>a</sup></td>
    <td class="tg-c3ow">Нет</td>
    <td class="tg-c3ow">Всегда</td>
    <td class="tg-c3ow">Всегда</td>
  </tr>
</tbody>
</table>
<p>&nbsp;</p>

> <sup> a</sup>  ASP.NET позволяет асинхронным операциям выполняться параллельно, при условии, что они не взаимодействуют с ресурсами, требующими синхронизации. Это ключевой момент для обеспечения высокой производительности и масштабируемости веб-приложений, обрабатывающих множество запросов одновременно.

`SynchronizationContext` по умолчанию не гарантирует ни порядка выполнения, ни синхронизации, где базовая реализация методов `Send` и `Post` выглядит следующим образом:

```csharp
public virtual void Send (SendOrPostCallback d, object state)
{
    d (state);
}
public virtual void Post (SendOrPostCallback d, object state)
{
    ThreadPool.QueueUserWorkItem (d.Invoke, state);
}
```

Как видим, `Send` просто выполняет делегат в вызывающем потоке, `Post` делает то же самое, но используя пул потоков для асинхронности. Но в API-интерфейсах данные методы переопределены и реализуют концепцию очереди сообщений: вызов метода Post эквивалентен вызову `Beginlnvoke` на объекте `Dispatcher` (для WPF) или `Control` (для Windows Forms), а метод `Send` является эквивалентом `Invoke`.

> В версии .NET Framework 2.0 был введен класс `BackgroundWorker`, который использует класс `SynchronizationContext` для упрощения работы по управлению рабочими потоками в обогащенных клиентских приложениях. Позже класс `BackgroundWorker` стал избыточным из-за появления классов задач и асинхронных функций, которые также имеют дело с `SynchronizationContext`.

## BackgroundWorker

Класс `BackgroundWorker` позволяет обогащенным клиент­ским приложениям запускать рабочий поток и сообщать о проценте выполненной ра­боты без необходимости в явном захвате контекста синхронизации. Например:

```csharp
var worker = new BackgroundWorker { WorkerSupportsCancellation = true };
worker.DoWork += (sender, args) =>
{ // Выполняется в рабочем потоке
  if (args.Cancel) return;
  Thread.Sleep(1000);
  args.Result = 123;
};
worker.RunWorkerCompleted += (sender, args) =>
{ // Выполняется в потоке пользовательского интерфейса
  // Здесь можно безопасно обновлять элементы управления
  // пользовательского интерфейса
  if (args.Cancelled)
    Console.WriteLine("Cancelled");
  else if (args.Error != null)
    Console.WriteLine("Error: " + args.Error.Message);
  else
    Console.WriteLine("Result is: " + args.Result);
};
worker.RunWorkerAsync();  // Захватывает контекст синхронизации
                          // и запускает операцию
```

Метод `RunWorkerAsync` запускает операцию, инициируя событие `DoWork` в рабочем потоке из пула. Он также захватывает контекст синхронизации, и когда опе­рация завершается (или отказывает), через данный контекст генерируется событие `RunWorkerCompleted` (подобно признаку продолжения).

Класс `BackgroundWorker` порождает крупномодульный параллелизм, при котором событие `DoWork` инициируется полностью в рабочем потоке. Если в этом обработчи­ке событий нужно обновлять элементы управления пользовательского интерфейса (помимо отправки сообщения о проценте выполненных работ), тогда придется использовать `Beginlnvoke` или похожий метод.

## Использованные источники

[^1]: Албахари Д., Албахари Б. C# 7.0. Справочник. Полное описание языка.: Пер. с англ. – СпБ.: ООО «Альфа-книга», 2018. – С. 570-572. (См. главу 14. Параллелизм и асинхронность, п. Многопоточность в обогащенных клиентских приложениях.)

[^2]: Стивен К. Параллельные вычисления – Все дело в SynchronizationContext [Электронный ресурс]. Электрон. жур. Февраль 2011. Том 26, Номер 2 . URL: [https://learn.microsoft.com/ru-ru/archive/msdn-magazine/2011/february/msdn-magazine-parallel-computing-it-s-all-about-the-synchronizationcontext](https://learn.microsoft.com/ru-ru/archive/msdn-magazine/2011/february/msdn-magazine-parallel-computing-it-s-all-about-the-synchronizationcontext) (Дата обращения: 23.10.2022)