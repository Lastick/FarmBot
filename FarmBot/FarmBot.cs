using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using FarmBot.Hardware;
using FarmBot.IM;

namespace FarmBot {

  class FarmBot {

    static void Main(string[] args){
      //int n = 1;
      //while (n < 6){
      //  Console.WriteLine("Current value of n is {0}", n);
      //  n++;
      //}
      //new test();
      new im();
    }
  }

  class test {

    public test(){
      Console.WriteLine("Тестирование вывода");
      HardwareMonitor monitor = new HardwareMonitor();
      Console.WriteLine("Информация о микропроцессорах");
      CPU[] cpu = monitor.getCPU();
      Console.WriteLine("Количество микропроцессоров: " + cpu.Length + "\n");
      foreach (var cpu_target in cpu){
        Console.WriteLine("Псевдоним микропроцессора: " + cpu_target.name);
        Console.WriteLine("Количество ядер: " + cpu_target.cors);
        Console.WriteLine("Нагрузка: " + cpu_target.load);
        Console.WriteLine("Частота шины: " + cpu_target.bus);
        for (int a = 0; a < cpu_target.cors; a++){
          Console.WriteLine("Частота ядра #" + a + ": " + cpu_target.clocks[a]);
          Console.WriteLine("Нагрузка ядра #" + a + ": " + cpu_target.loads[a]);
        }
      }
    }

  }

  class im {

    public im(){
      IMC imc = new IMC("jabber.org.ua", "bot.farmbot", "pass");
      imc.setPresenceStatus("Привіт, Україно!");
      imc.setPresenceType(0);
      while (true){
        Thread.Sleep(1000);
        String[] a = new String[2];
        a[0] = "";
        a[1] = "";
        if (imc.doInbox()){
          a[0] = imc.getInboxMess()[0];
          a[1] = imc.getInboxMess()[1];
        }
        if (a[0].IndexOf("/") > 0){
          a[0] = a[0].Substring(0, a[0].IndexOf("/"));
          if (imc.getOutboxStatus()){
            imc.setMess(a);
          }
        }
      }
    }

  }

}
