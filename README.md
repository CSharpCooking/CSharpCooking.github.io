# C# Cooking

[![Build and Deploy](https://github.com/CSharpCooking/CSharpCooking.github.io/actions/workflows/pages-deploy.yml/badge.svg)](https://github.com/CSharpCooking/CSharpCooking.github.io/actions/workflows/pages-deploy.yml)

Исходный код сайта **[csharpcooking.github.io](https://csharpcooking.github.io)**: рецепты вкусных программ на языке C#.

Это образовательный ресурс по программированию на C# и .NET. На нём собраны статьи, учебные курсы, практикумы, хакатоны и записи трансляций для студентов и разработчиков.

Автор: Руслан Гибадуллин, кандидат технических наук, доцент кафедры компьютерных систем КНИТУ-КАИ. Подробнее на странице [«О сайте»](https://csharpcooking.github.io/about/).

## Что на сайте

- **Статьи** о многопоточности, управляемой памяти и асинхронности в .NET, например [«Методы оптимизации асинхронных операций в .NET»](https://csharpcooking.github.io/posts/Methods-for-Optimizing-Asynchronous-Operations-in-NET/).
- **Учебные курсы:** [«Параллельное программирование»](https://csharpcooking.github.io/posts/Parallel-Programming/) и [«Параллелизм и асинхронность»](https://csharpcooking.github.io/posts/Concurrency-and-Asynchrony/).
- **Практикумы:** [по языку C#](https://csharpcooking.github.io/posts/CSharp-Tasks/), [по сетевому программированию](https://csharpcooking.github.io/posts/Network-Programming/), [по SQL и LINQ](https://csharpcooking.github.io/posts/SQL-LINQ/).
- **Хакатоны:** [онлайн-хакатон по параллельному программированию](https://csharpcooking.github.io/posts/Online-Hackathon-on-Parallel-Programming/) и [II онлайн-хакатон](https://csharpcooking.github.io/posts/2-Online-Hackathon-on-Parallel-Programming/).
- **Трансляции** по параллелизму, асинхронности и основам UML.
- **Источники:** [теоретические](https://csharpcooking.github.io/posts/Sources-Theory/) и [практические](https://csharpcooking.github.io/posts/Sources-Practice/) материалы, [инструментальные средства](https://csharpcooking.github.io/posts/Tools/).

Каналы: [YouTube](https://www.youtube.com/CSharpCooking), [Rutube](https://rutube.ru/u/csharpcooking/), [Telegram](https://t.me/CSharpCooking).

## Материалы для скачивания

PDF, архивы с кодом и данные хакатонов лежат не в репозитории, а в релизе [materials](https://github.com/CSharpCooking/CSharpCooking.github.io/releases/tag/materials). Ссылки в статьях ведут туда.

Новые крупные файлы стоит добавлять в этот релиз, а не в репозиторий: у GitHub Pages лимит 1 ГБ на опубликованный сайт.

## Как устроен репозиторий

| Путь | Что там |
|---|---|
| `_posts/` | статьи и страницы разделов в Markdown |
| `_tabs/` | страницы «О сайте», «Архив», «Категории», «Теги» |
| `pastes/` | скриншоты, которые показываются в статьях |
| `assets/` | аватар, обложки хакатонов, иконки |
| `_config.yml` | настройки сайта |
| `.github/workflows/pages-deploy.yml` | сборка и публикация |

Сайт собирается генератором [Jekyll](https://jekyllrb.com) с темой [Chirpy](https://github.com/cotes2020/jekyll-theme-chirpy). После каждого push в `main` GitHub Actions собирает сайт, проверяет внутренние ссылки и картинки (html-proofer) и публикует его на GitHub Pages.

## Локальный запуск

Нужны [Ruby](https://www.ruby-lang.org) 3.x и Bundler.

```bash
bundle install
bundle exec jekyll serve      # сайт откроется на http://127.0.0.1:4000
```

Перед push можно проверить сборку так же, как это делает GitHub Actions (в Linux, macOS или Git Bash):

```bash
bash tools/test.sh            # сборка в режиме production и проверка ссылок
```

Если html-proofer падает с ошибкой кодировки (`Encoding::InvalidByteSequenceError`), задайте UTF-8: `export LANG=C.UTF-8 LC_ALL=C.UTF-8`.

## Лицензия

Код шаблона [chirpy-starter](https://github.com/cotes2020/chirpy-starter) распространяется по лицензии MIT, см. [LICENSE](LICENSE). Права на статьи и авторские учебные материалы принадлежат автору.
