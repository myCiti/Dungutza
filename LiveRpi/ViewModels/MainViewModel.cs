using Avalonia.Threading;
using CommonLogic;
using LiveRpi.Models;
using MoreLinq;
using ReactiveUI;
using System;
using System.Device.Gpio;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiveRpi.ViewModels
{
    class MainViewModel : ReactiveObject
    {
        public Logic Logic { get; }

        int counter;
        public int Counter { get => counter; set => this.RaiseAndSetIfChanged(ref counter, value); }

        int pulseCounter;
        public int PulseCounter { get => pulseCounter; set => this.RaiseAndSetIfChanged(ref pulseCounter, value); }

        Direction direction;
        public Direction Direction { get => direction; set => this.RaiseAndSetIfChanged(ref direction, value); }

        double frequency;
        public double Frequency { get => frequency; set => this.RaiseAndSetIfChanged(ref frequency, value); }

        bool receivedResult;
        public bool ReceivedResult { get => receivedResult; set => this.RaiseAndSetIfChanged(ref receivedResult, value); }

        public InputModel[] Inputs { get; } = Logic.Pins.Zip(new[] { 2, 3, 4, 17, 27 }, (name, pinId) => (name, pinId))
            .Select(w => new InputModel { Text = w.name, PinId = w.pinId })
            .ToArray();

        readonly Dispatcher mainDispatcher;
        readonly Thread logicThread;
        readonly GpioController gpioController = new();

        public MainViewModel()
        {
            mainDispatcher = Dispatcher.UIThread;

            Inputs.ForEach(i => gpioController.OpenPin(i.PinId, PinMode.Input));

            var logStream = new StreamWriter(new FileStream("log.csv", FileMode.Append, FileAccess.ReadWrite, FileShare.Read, 1024 * 1024));
            Logic = new((counter, pulseCounter, direction, frequency) =>
                {
                    _ = mainDispatcher.InvokeAsync(() => (Counter, PulseCounter, Direction, Frequency, ReceivedResult) = (counter, pulseCounter, direction, frequency, true));
                    _ = logStream.WriteLineAsync($"{DateTime.Now},{counter},{pulseCounter},{direction},{frequency}");
                },
                pin => gpioController.Read(Inputs[pin].PinId) == PinValue.High);

            logicThread = new(() => { while (true) Logic.Step(); }) { Name = "Logic Thread", IsBackground = true };
            logicThread.Start();
        }
    }
}
