using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenHardwareMonitor.Hardware;

namespace FarmBot.Hardware {

  class HardwareMonitor {

    private Boolean CPUEnabled;
    private Boolean MainboardEnabled;
    private Boolean FanControllerEnabled;
    private Boolean RAMEnabled;
    private Boolean GPUEnabled;
    private Boolean HDDEnabled;
    private int cpu_n;
    private int gpu_ati_n;
    private int gpu_nv_n;
    private int hdd_n;
    private CPU[] CPU;
    private Mainboard Mainboard;
    private CPU[] GPU_ATI;
    private CPU[] GPU_NV;
    private CPU[] HDD;

    public HardwareMonitor(Boolean CPU = true, Boolean Mainboard = true, Boolean FanController = true, Boolean RAM = true, Boolean GPU = true, Boolean HDD = false){
      this.CPUEnabled = false;
      this.MainboardEnabled = false;
      this.FanControllerEnabled = false;
      this.RAMEnabled = false;
      this.GPUEnabled = false;
      this.HDDEnabled = false;
      this.cpu_n = 0;
      this.gpu_ati_n = 0;
      this.gpu_nv_n = 0;
      this.hdd_n = 0;
      if (CPU) this.CPUEnabled = true;
      if (Mainboard) this.MainboardEnabled = true;
      if (FanController) this.FanControllerEnabled = true;
      if (RAM) this.RAMEnabled = true;
      if (GPU) this.GPUEnabled = true;
      if (HDD) this.HDDEnabled = true;
      this.CPU = null;
      this.Mainboard.mainboard = "";
      this.Mainboard.superIO = "";
      this.Mainboard.volt_core = 0.0f;
      this.Mainboard.volt_ram = 0.0f;
      this.Mainboard.volt_yellow = 0.0f;
      this.Mainboard.volt_red = 0.0f;
      this.Mainboard.volt_orange = 0.0f;
      this.Mainboard.volt_bat = 0.0f;
      this.Mainboard.temp_cpu = 0.0f;
      this.Mainboard.temp_chipset = 0.0f;
      this.Mainboard.fan_cpu = 0.0f;
      this.Mainboard.fans = null;
      this.GPU_ATI = null;
      this.GPU_NV = null;
      this.HDD = null;
      this.doLoad();
    }

    private void doLoad(){
      Computer machine = new Computer();
      machine.CPUEnabled = this.CPUEnabled;
      machine.MainboardEnabled = this.MainboardEnabled;
      machine.FanControllerEnabled = this.FanControllerEnabled;
      machine.RAMEnabled = this.RAMEnabled;
      machine.GPUEnabled = this.GPUEnabled;
      machine.HDDEnabled = this.HDDEnabled;
      this.cpu_n = 0;
      this.gpu_ati_n = 0;
      this.gpu_nv_n = 0;
      this.hdd_n = 0;
      machine.Open();
      foreach (var hardware in machine.Hardware){
        if (hardware.HardwareType == HardwareType.CPU) this.cpu_n++;
        if (hardware.HardwareType == HardwareType.GpuAti) this.gpu_ati_n++;
        if (hardware.HardwareType == HardwareType.GpuNvidia) this.gpu_nv_n++;
        if (hardware.HardwareType == HardwareType.HDD) this.hdd_n++;
      }
      this.CPU = new CPU[this.cpu_n];
      this.Mainboard.mainboard = "";
      this.Mainboard.superIO = "";
      this.Mainboard.volt_core = 0.0f;
      this.Mainboard.volt_ram = 0.0f;
      this.Mainboard.volt_yellow = 0.0f;
      this.Mainboard.volt_red = 0.0f;
      this.Mainboard.volt_orange = 0.0f;
      this.Mainboard.volt_bat = 0.0f;
      this.Mainboard.temp_cpu = 0.0f;
      this.Mainboard.temp_chipset = 0.0f;
      this.Mainboard.fan_cpu = 0.0f;
      this.Mainboard.fans = null;
      this.GPU_ATI = new CPU[this.gpu_ati_n];
      this.GPU_NV = new CPU[this.gpu_nv_n];
      this.HDD = new CPU[this.hdd_n];
      int cpu_c = 0;
      foreach (var hardware in machine.Hardware){
        hardware.Update();
        if (this.CPUEnabled && hardware.HardwareType == HardwareType.CPU && cpu_c < this.cpu_n){
          int core_n = 0;
          foreach (var sensor in hardware.Sensors){
            if (sensor.SensorType == SensorType.Load && sensor.Name.IndexOf("Core") >= 0) core_n++;
          }
          this.CPU[cpu_c].name = hardware.Name;
          this.CPU[cpu_c].cors = core_n;
          this.CPU[cpu_c].loads = new float[core_n];
          this.CPU[cpu_c].clocks = new float[core_n];
          int loads_c = 0;
          int clocks_c = 0;
          foreach (var sensor in hardware.Sensors){
            if (sensor.SensorType == SensorType.Load && sensor.Name.IndexOf("Core") >= 0){
              if (loads_c < core_n) this.CPU[cpu_c].loads[loads_c] = sensor.Value.GetValueOrDefault();
              loads_c++;
            }
            if (sensor.SensorType == SensorType.Clock && sensor.Name.IndexOf("Core") >= 0){
              if (clocks_c < core_n) this.CPU[cpu_c].clocks[clocks_c] = sensor.Value.GetValueOrDefault();
              clocks_c++;
            }
            if (sensor.SensorType == SensorType.Load && sensor.Name.IndexOf("CPU Total") == 0){
              this.CPU[cpu_c].load = sensor.Value.GetValueOrDefault();
            }
            if (sensor.SensorType == SensorType.Clock && sensor.Name.IndexOf("Bus Speed") == 0){
              this.CPU[cpu_c].bus = sensor.Value.GetValueOrDefault();
            }
          }
          cpu_c++;
        }
        if (hardware.HardwareType == HardwareType.Mainboard){
          Console.WriteLine("\nMainboard: (" + hardware.Name + ")");
          this.Mainboard.mainboard = hardware.Name;
          foreach (var subhardware in hardware.SubHardware){
            subhardware.Update();
            if (subhardware.HardwareType == HardwareType.SuperIO){
              this.Mainboard.superIO = subhardware.Name;
              Console.WriteLine(" " + subhardware.Name + ": " + subhardware.HardwareType.ToString() + " (SuperIO)");
              foreach (var sensor in subhardware.Sensors){
                Console.WriteLine(sensor.Name + ": " + sensor.Value.GetValueOrDefault());
                if (sensor.SensorType == SensorType.Voltage && sensor.Name.IndexOf("CPU VCore") == 0){
                  this.Mainboard.volt_core = sensor.Value.GetValueOrDefault();
                }
                if (sensor.SensorType == SensorType.Voltage && sensor.Name.IndexOf("VBat") == 0){
                  this.Mainboard.volt_core = sensor.Value.GetValueOrDefault();
                }
              }
            }
          }
        }
        if (hardware.HardwareType == HardwareType.RAM){
          Console.WriteLine("\nRAM: " + hardware.Name);
          foreach (var sensors in hardware.Sensors){
            Console.WriteLine(sensors.Name + ": " + sensors.Value.GetValueOrDefault());
          }
        }
        if (hardware.HardwareType == HardwareType.GpuAti){
          Console.WriteLine("\nGpuAti: " + hardware.Name);
          foreach (var sensors in hardware.Sensors){
            Console.WriteLine(sensors.Name + ": " + sensors.Value.GetValueOrDefault());
          }
        }
      }
      machine.Close();
    }

    public CPU[] getCPU(){
      return this.CPU;
    }

    public Mainboard getMainboard(){
      return this.Mainboard;
    }

  }

  public struct CPU {
    public String name;
    public float load;
    public float bus;
    public int cors;
    public float[] loads;
    public float[] clocks;
  }

  public struct Mainboard {
    public String mainboard;
    public String superIO;
    public float volt_core;
    public float volt_ram;
    public float volt_yellow;
    public float volt_red;
    public float volt_orange;
    public float volt_bat;
    public float temp_cpu;
    public float temp_chipset;
    public float fan_cpu;
    public float[] fans;
  }

}
