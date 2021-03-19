using Iot.Device.CpuTemperature;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Device.Pwm;
using System.Device.Pwm.Drivers;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace GpioFanController
{
    class Program
    {
        private static PwmChannel _fan;
        private static CpuTemperature _cpuTemperature;
        private static int _pin;
        private static List<Gate> _gates;

        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var root = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("settings.json")
                .Build();

            int interval = root.GetSection("Interval").Get<int>();
            _pin = root.GetSection("Pin").Get<int>();
            _gates = root.GetSection("Gates").Get<List<Gate>>()
                .OrderBy(x => x.Temperature)
                .ToList();

            _cpuTemperature = new CpuTemperature();
            _fan = new SoftwarePwmChannel(pinNumber: _pin, frequency: 400, dutyCycle: 0);
            _fan.Start();

            using Timer timer = new Timer(interval);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };
            _quitEvent.WaitOne();

            _fan.Dispose();
            _cpuTemperature.Dispose();
            timer.Dispose();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            double cpuTemp = _cpuTemperature.Temperature.DegreesCelsius;
            Gate gate = _gates.Where(x => x.Temperature <= cpuTemp)
                .OrderByDescending(x => x.Temperature)
                .FirstOrDefault();
            
            _fan.DutyCycle = gate.Speed;
        }
    }
}
