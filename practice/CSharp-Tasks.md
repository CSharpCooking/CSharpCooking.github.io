# Практикум по языку программирования C# [↩︎](/sources)

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

## Автор

- **Гибадуллин Руслан Фаршатович**
  - Кандидат технических наук, доцент кафедры компьютерных систем Казанского национального исследовательского технического университета им. А.Н. Туполева–КАИ.
- **Контакты**
  - Telegram: [@RuslanGibadullin](https://t.me/RuslanGibadullin)
  - Электронная почта: [CSharpCooking@gmail.com](mailto:CSharpCooking@gmail.com)

<html>
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
</html>