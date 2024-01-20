# Практическое задание по теме «Параллельные коллекции» 

## Описание задания

Ниже представлена *очередь* производителей/потребителей с использованием задач.

```csharp
void Main()
{
  using (var pcQ = new PCQueue(1))
  {
    Task task1 = pcQ.Enqueue(() => Console.WriteLine("Too"));
    Task task2 = pcQ.Enqueue(() => Console.WriteLine("Easy!"));

    task1.ContinueWith(_ => "Task 1 complete".Dump());
    task2.ContinueWith(_ => "Task 2 complete".Dump());
  }
}

public class PCQueue : IDisposable
{
  BlockingCollection<Task> _taskQ = new BlockingCollection<Task>();

  public PCQueue(int workerCount)
  {
    // Create and start a separate Task for each consumer:
    for (int i = 0; i < workerCount; i++)
      Task.Factory.StartNew(Consume);
  }

  public Task Enqueue(Action action, CancellationToken cancelToken = default(CancellationToken))
  {
    var task = new Task(action, cancelToken);
    _taskQ.Add(task);
    return task;
  }

  public Task<TResult> Enqueue<TResult>(Func<TResult> func,
    CancellationToken cancelToken = default(CancellationToken))
  {
    var task = new Task<TResult>(func, cancelToken);
    _taskQ.Add(task);
    return task;
  }

  void Consume()
  {
    foreach (var task in _taskQ.GetConsumingEnumerable())
      try
      {
        if (!task.IsCanceled) task.RunSynchronously();
      }
      catch (InvalidOperationException) { }  // Race condition
  }

  public void Dispose() { _taskQ.CompleteAdding(); }
}
```

На основе данного кода программы выполните следующие шаги:

1. Реализуйте *стек* производителей/потребителей. 
2. Задействуйте при работе со стеком обобщенный метод `Enqueue<TResult>`, задав максимальную степень параллелизма значением 2.

## Методические указания по выполнению

- Для выполнения задания рекомендуется ознакомиться с темой "Параллельные коллекции" 23-й главы «Параллельное программирование» книги [Албахари Д. C# 7.0. Справочник. Полное описание языка](https://csharpcooking.github.io/theory/AlbahariCSharp7.zip).
- Для написания и проверки кода рекомендуется использовать одно из следующих программных обеспечений:
  - [Visual Studio: IDE и редактор кода для разработчиков и групп, работающих с программным обеспечением](https://visualstudio.microsoft.com/)
  - [LINQPad – The .NET Programmer's Playground](https://www.linqpad.net/)