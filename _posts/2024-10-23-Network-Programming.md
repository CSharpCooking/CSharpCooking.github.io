---
title: "Практикум по сетевому программированию"
description: "Методические указания по выполнению практических заданий по по сетевому программированию на языке C#."
author: RuslanGibadullin
date: 2024-10-23
categories: [Источники]
tags: [сетевое программирование]
---

## Задания

1. Протестировать приложение [Multi-Client/Server GUI Application](https://csharpcooking.github.io/practice/Multi-Client-Chat-Server-Original-Version.zip), найти и устранить ошибки, которые возникают в ходе работы с данным приложением.
2. Развить решение первого задания, обеспечив передачу данных между клиентами и сервером по защищенному каналу с использованием криптографического алгоритма AES.
3. Разработать программу для авторизации на сайте [bookland.com](http://www.bookland.com) и вывода списка товаров корзины пользователя.

## Рекомендации

* Для выполнения практикума рекомендуется руководствоваться источником:
  [Албахари Д. C# 7.0. Справочник. Полное описание языка / Албахари Д., Албахари Б. // Пер. с англ. – Москва: Альфа-Книга. – 2018](https://csharpcooking.github.io/theory/AlbahariCSharp7.zip). (См. главы 16 «Взаимодействие с сетью», 21 «Безопасность», 26 «Регулярные выражения».)

* Если в ходе выполнения первого задания возникли значительные трудности, то корректную версию приложения можно посмотреть по [ссылке](https://csharpcooking.github.io/practice/Multi-Client-Chat-Server-Correct-Version.zip). Проанализируйте исходную и корректную версии программ, сделайте выводы по внесенным изменениям.

* Для выполнения второго задания рекомендуется ознакомиться с примерами исходных кодов серверного и клиентского модулей на базе алгоритма шифрования DES:

  ```csharp
  using System;
  using System.Text;
  using System.IO;
  using System.Net;
  using System.Net.Sockets;
  using System.Security.Cryptography;
  namespace Server
  {
    static class Program
    {
      static void Main(string[] args)
      {
        byte[] bytesRecv = new byte[4096];
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5001);
        listener.Start();
        Socket tc = listener.AcceptSocket();
        //Получение
        tc.Receive(bytesRecv);
        string msg = Encoding.Unicode.GetString(bytesRecv);
        Console.WriteLine(msg.Remove(msg.IndexOf('\0')).Decrypt());
        //Отправка
        tc.Send(Encoding.Unicode.GetBytes(Crypt("Secret message from server.")));
        tc.Close();
      }
      private static byte[] key = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };
      private static byte[] iv = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };
      public static string Crypt(this string text)
      {
        SymmetricAlgorithm algorithm = DES.Create();
        ICryptoTransform transform = algorithm.CreateEncryptor(key, iv);
        byte[] inputbuffer = Encoding.Unicode.GetBytes(text);
        byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
        return Encoding.Unicode.GetString(outputBuffer);
      }
      public static string Decrypt(this string text)
      {
        SymmetricAlgorithm algorithm = DES.Create();
        ICryptoTransform transform = algorithm.CreateDecryptor(key, iv);
        byte[] inputbuffer = Encoding.Unicode.GetBytes(text);
        byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
        return Encoding.Unicode.GetString(outputBuffer);
      }
    }
  }
  ```
  
* Для выполнения третьего задания рекомендуется ознакомиться с примером авторизации на сайте (с сериализацией/десериализацией cookie-наборов):
  
  ```csharp
  void Main()
  {
    func().Wait();
  }
  async Task func()
  {
    CookieContainer cc = new CookieContainer();
    var handler = new HttpClientHandler { CookieContainer = cc };
    var request = new HttpRequestMessage(HttpMethod.Post, "адрес регистрационной формы сайта");
    request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
    { // ...
      { "login", "user" },
      { "password", "xxxx" }, 
      // ...
    });
    var client = new HttpClient(handler);
    var response = await client.SendAsync(request);
    string responseString = await response.Content.ReadAsStringAsync();
    using (StreamWriter sw = new StreamWriter(@"..\Downloads\code.html", false, System.Text.Encoding.Default))
    {
      sw.Write(responseString);
    }
    var formatter = new SoapFormatter();
    using (Stream s = File.Create(@"..\Downloads\cookies.dat"))
      formatter.Serialize(s, cc);
    CookieContainer cc2 = null;
    using (Stream s = File.OpenRead(@"..\Downloads\cookies.dat"))
      cc2 = (CookieContainer)formatter.Deserialize(s);
    var handler2 = new HttpClientHandler { CookieContainer = cc2 };
    var client2 = new HttpClient(handler2);
    var response2 = await client2.GetAsync("адрес сайта");
    string responseString2 = await response2.Content.ReadAsStringAsync();
    using (StreamWriter sw = new StreamWriter(@"..\Downloads\code2.html", false, System.Text.Encoding.Default))
    {
      sw.Write(responseString2);
    }
  }
  ```
  Информацию об отсылаемых полях легко получить, воспользовавшись инструментальными средствами браузера.

  ![](https://raw.githubusercontent.com/CSharpCooking/CSharpCooking.github.io/refs/heads/main/pastes/2022-01-31-21-21-30.png)  
  Рис. Отсылаемые поля в POST-запросе
