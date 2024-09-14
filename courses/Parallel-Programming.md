# Учебный курс «Параллельное программирование на языке C#» 

## Аннотация

В учебном курсе раскрываются многопоточные API-интерфейсы и конструкции, направленные на использование преимуществ многоядерных процессоров: Parallel LINQ, класс Parallel, параллелизм задач, работа с AggregateException, параллельные коллекции.

## Материалы

1. **Parallel LINQ**
  - [Конспект](https://docs.google.com/document/d/1ml3sl3OuUKsrFjB1dccf6L0Yj4Xvp8EdICzV-r8WSY4/)
  - [Презентация](https://docs.google.com/presentation/d/17ck5HP8plNOskZErLhYqEUMb1I09cIulUXPO-GEVG5o/)
  - [Видеолекция](https://youtu.be/68vVLvOEOQk)
  - [Тест](https://www.classmarker.com/online-test/start/test-intro/?quiz=kg66384e87528fc7)
  - [Задание](https://csharpcooking.github.io/courses/Parallel-Programming-Parallel-LINQ)
2. **Класс Parallel**
  - [Конспект](https://docs.google.com/document/d/1-N5YSDCPnd2m6e3oZIxZeIlTE055tRp74mElEu1a09I/)
  - [Презентация](https://docs.google.com/presentation/d/1PrLVw8yPrQln7bGVA-1rggYV2GOsT0xspGu7U-rGvgQ/)
  - [Видеолекция](https://youtu.be/3Zbc7Ykj_OU)
  - [Тест](https://www.classmarker.com/online-test/start/test-intro/?quiz=k67638746d849e77)
  - [Задание](https://csharpcooking.github.io/courses/Parallel-Programming-Class-Parallel)
3. **Параллелизм задач**
  - [Конспект](https://docs.google.com/document/d/1OpuY5eMTAHo8ijH6vsf_TzyQcu63TjCFQz5IDB-Kegg/)
  - [Презентация](https://docs.google.com/presentation/d/1IjvaNTtpNb3GXGAYo6j_r7yQrHA152V4B8c30TAyzcE/)
  - [Видеолекция](https://youtu.be/98Hyw6Xjn6o)
  - [Тест](https://www.classmarker.com/online-test/start/test-intro/?quiz=eny6388f0205dd09)
  - [Задание](https://csharpcooking.github.io/courses/Parallel-Programming-Task-Parallelism)
4. **Работа с AggregateException**
  - [Конспект](https://docs.google.com/document/d/1teq2EWz-0sifjbp7W1PvE5xUZTzQANvz/)
  - [Презентация](https://docs.google.com/presentation/d/1JLtQefzgP0uiarGSI58CHUKR_iGHb-08uem5Lv6rzfo/)
  - [Видеолекция](https://youtu.be/5U6fk6XC6AU)
  - [Тест](https://www.classmarker.com/online-test/start/?quiz=hcy65ac2749d1445)
  - [Задание](https://csharpcooking.github.io/courses/Parallel-Programming-Working-with-AggregateException)
5. **Параллельные коллекции**
  - [Конспект](https://docs.google.com/document/d/1cnThA4kU_GkdYrmOVY0vpUhZ2bxSxHkPApbRBCLmJF0/)
  - [Презентация](https://docs.google.com/presentation/d/1f1ihTc_LCigsvPHbp0Ncg7nUgC7su3hKayvtKrv1Szc/)
  - [Видеолекция](https://youtu.be/VpgkBGA-98s)
  - [Тест](https://www.classmarker.com/online-test/start/test-intro/?quiz=3cq638a131bf365c)
  - [Задание](https://csharpcooking.github.io/courses/Parallel-Programming-Concurrent-Collections)

## Источники

- Учебный курс подготовлен на основе главы «Параллельное программирование» книги [Албахари Д. C# 7.0. Справочник. Полное описание языка](https://csharpcooking.github.io/theory/AlbahariCSharp7.zip).
- Для написания и проверки программных кодов рекомендуется использовать один из следующих инструментов:
  - [Visual Studio: IDE и редактор кода для разработчиков и групп, работающих с программным обеспечением](https://visualstudio.microsoft.com/)
  - [LINQPad – The .NET Programmer's Playground](https://www.linqpad.net/)
- Для прохождения тестов возможно потребуется применение VPN сервиса, например, [Zoog VPN](https://zoogvpn.com/ru-ru/?a_aid=65957b40c9435).

## Автор

- **Гибадуллин Руслан Фаршатович**
  - Кандидат технических наук, доцент кафедры компьютерных систем Казанского национального исследовательского технического университета им. А.Н. Туполева–КАИ.
- **Контакты**
  - Telegram: [@RuslanGibadullin](https://t.me/RuslanGibadullin)
  - Электронная почта: [CSharpCooking@gmail.com](mailto:CSharpCooking@gmail.com)

{%- if site.plainwhite.disqus_shortname -%}
<div id="disqus_thread" style="margin-top:25px"></div>
<script>
    var disqus_config = function () {
        this.page.url = '{{ page.url | absolute_url }}';
        this.page.identifier = '{{ page.url | absolute_url }}';
    };
    (function () {
        var d = document, s = d.createElement('script');
        s.src = 'https://{{ site.plainwhite.disqus_shortname }}.disqus.com/embed.js';
        s.setAttribute('data-timestamp', +new Date());
        (d.head || d.body).appendChild(s);
    })();
</script>
<noscript>Пожалуйста, включите JavaScript для просмотра <a href="https://disqus.com/?ref_noscript" rel="nofollow">комментариев, поддерживаемых Disqus</a>.</noscript>
{%- endif -%}