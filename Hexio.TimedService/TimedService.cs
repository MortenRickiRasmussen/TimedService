﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Hexio.TimedService
{
    public static class AutofacExtensions
    {
        public static IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> AddTimedService<T>(this ContainerBuilder builder) where T : ITimedService
        {
            builder.RegisterType<TimedService<T>>().AsImplementedInterfaces();

            return builder.RegisterType<T>();
        }
    }
    
    public interface ITimedService
    {
        /// <summary>
        /// The interval between each iteration, the value is called during each iteration.
        /// </summary>
        TimeSpan Interval { get; }

        Task Execute();
    }
    
    public class TimedService<T> : BackgroundService where T : ITimedService
    {
        private TimeSpan _interval;
        private readonly IServiceProvider _services;

        public TimedService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            Log.Information("Timed service {TimedService} registered", typeof(T).Name);

            while (!token.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var service = scope.ServiceProvider.GetService<T>();

                    _interval = service.Interval;

                    try
                    {
                        Log.Information("{TimedService} started running", typeof(T).Name);

                        Stopwatch watch = Stopwatch.StartNew();
                        
                        await service.Execute();
                        
                        watch.Stop();
                        
                        Log.Information("{TimedService} finished execution in {Elapsed} ms", typeof(T).Name, watch.ElapsedMilliseconds);

                        _interval = service.Interval;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed during execution of timed service {TimedService}", typeof(T).Name);
                    }
                }
                await Task.Delay(_interval, token);
            }
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("Stopping timed service {TimedService}", typeof(T).Name);

            return Task.CompletedTask;
        }
    }
}
