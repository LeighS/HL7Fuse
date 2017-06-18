using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using SuperSocket.SocketBase;
using SuperSocket.SocketEngine;
using HL7Fuse;


namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private static Dictionary<string, ControlCommand> m_CommandHandlers = new Dictionary<string, ControlCommand>((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
        private static bool setConsoleColor;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }


        private static void CheckCanSetConsoleColor()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.ResetColor();
                setConsoleColor = true;
            }
            catch
            {
                setConsoleColor = false;
            }
        }

        private static void SetConsoleColor(ConsoleColor color)
        {
            if (!setConsoleColor)
                return;
            Console.ForegroundColor = color;
        }

        private static void AddCommand(string name, string description, Func<IBootstrap, string[], bool> handler)
        {
            ControlCommand controlCommand = new ControlCommand()
            {
                Name = name,
                Description = description,
                Handler = handler
            };
            m_CommandHandlers.Add(controlCommand.Name, controlCommand);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {

            Console.WriteLine("Welcome to HL7Fuse!");
            CheckCanSetConsoleColor();
            Console.WriteLine("Initializing...");
            IBootstrap bootstrap = BootstrapFactory.CreateBootstrap();
            if (!bootstrap.Initialize())
            {
                SetConsoleColor(ConsoleColor.Red);
                Console.WriteLine("Failed to initialize HL7Fuse! Please check error log for more information!");
            }
            else
            {
                Console.WriteLine("Starting...");

                StartResult startResult = bootstrap.Start();
                Console.WriteLine("-------------------------------------------------------------------");
                foreach (IWorkItem workItem in bootstrap.AppServers)
                {
                    if (workItem.State == ServerState.Running)
                    {
                        SetConsoleColor(ConsoleColor.Green);
                        Console.WriteLine("- {0} has been started", (object)workItem.Name);
                    }
                    else
                    {
                        SetConsoleColor(ConsoleColor.Red);
                        Console.WriteLine("- {0} failed to start", (object)workItem.Name);
                    }
                }
                Console.ResetColor();
                Console.WriteLine("-------------------------------------------------------------------");
                switch (startResult)
                {
                    case StartResult.None:
                        SetConsoleColor(ConsoleColor.Red);
                        Console.WriteLine("No server is configured, please check you configuration!");
                        return;
                    case StartResult.Success:
                        Console.WriteLine("The HL7Fuse has been started!");
                        break;
                    case StartResult.PartialSuccess:
                        SetConsoleColor(ConsoleColor.Red);
                        Console.WriteLine("Some server instances were started successfully, but the others failed! Please check error log for more information!");
                        break;
                    case StartResult.Failed:
                        SetConsoleColor(ConsoleColor.Red);
                        Console.WriteLine("Failed to start the HL7Fuse! Please check error log for more information!");
                        return;
                }
                Console.ResetColor();
                Console.WriteLine("Enter key 'quit' to stop the ServiceEngine.");
                while (!cancellationToken.IsCancellationRequested)
                {
                    Trace.TraceInformation("Working");
                    await Task.Delay(10000);
                }
                Console.WriteLine("The HL7Fuse has been stopped!");
            }
        }
    }
}
