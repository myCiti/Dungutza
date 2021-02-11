using System;
using System.Diagnostics;
using System.Linq;

namespace CommonLogic
{
    public enum Direction { Up, Down }

    public class Logic
    {
        public static readonly string[] Pins = new[] { "Close Limit", "Open Limit", "Index", "Pulse", "Stop" };

        const int ClosePin = 0;
        const int OpenPin = 1;
        const int IndexPin = 2;
        const int PulsePin = 3;
        const int StopPin = 4;

        readonly Action<int /*counter*/,int /* pulseCounter */, Direction, double /*freq in Hz*/> finished;
        readonly Func<int, bool> readPin;

        public Logic(Action<int, int, Direction, double> finished, Func<int, bool> readPin) =>
            (this.finished, this.readPin) = (finished, readPin);

        int step, counter, cycleCounter, pulseCounter;
        bool isHitStop;
        readonly Stopwatch cycleStart = Stopwatch.StartNew();
        Direction direction;

        public void Step()
        {
            void callFinished() 
            { 
               finished?.Invoke(counter, pulseCounter, direction, cycleCounter / cycleStart.Elapsed.TotalSeconds); 
               (cycleCounter, counter, pulseCounter) = (0, 0, 0);
               isHitStop = false;
               cycleStart.Restart(); 
            }

            switch (step)
            {
                case 0: if (readPin(OpenPin)) { step = 1; direction = Direction.Down; } else if (readPin(ClosePin)) { step = 1; direction = Direction.Up; } break;
                case 1: if (readPin(IndexPin)) step = 2; break;
                case 2: 
                    if (readPin(StopPin)) { pulseCounter = counter; isHitStop = true; }
                    else if(readPin(IndexPin) && isHitStop ) callFinished();
                    else if (readPin(PulsePin)) { step = 3; ++counter; } 
                    break;
                case 3: 
                    if (readPin(StopPin)) { pulseCounter = counter; isHitStop = true; }
                    else if(readPin(IndexPin) && isHitStop ) callFinished();
                    else if (readPin(PulsePin)) { step = 2;  } 
                    break;
            }

            ++cycleCounter;
        }
    }
}
