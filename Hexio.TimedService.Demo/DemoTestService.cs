using System;
using System.Threading.Tasks;

namespace Hexio.TimedService.Demo
{
    public class DemoTestService : ITimedService
    {
        public TimeSpan Interval { get; } = TimeSpan.FromSeconds(10);
        public async Task Execute()
        {
            await Task.Delay(1250);
        }
    }
}