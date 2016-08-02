#region

using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Logging;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using System.IO;

#endregion


namespace PokemonGo.RocketAPI.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException
                += delegate (object sender, UnhandledExceptionEventArgs eargs)
                {
                    Exception exception = (Exception)eargs.ExceptionObject;
                    System.Console.WriteLine(@"Unhandled exception: " + exception);
                    Environment.Exit(1);
                };

            CommandLineParser.CommandLineParser parser =  new CommandLineParser.CommandLineParser();

            ValueArgument<string> userArgument = new ValueArgument<string>('u', "user", "ptc username");
            ValueArgument<string> passwordArgument = new ValueArgument<string>('p', "password", "ptc password");

            SwitchArgument teleportArgument = new SwitchArgument('t', "teleport", "use teleport", false);

            parser.Arguments.Add(userArgument);
            parser.Arguments.Add(passwordArgument);
            parser.Arguments.Add(teleportArgument);

            try
            {
                /* parse command line arguments */
                parser.ParseCommandLine(args);
                /* this prints results to the console */
                //parser.ShowParsedArguments();
            }
            catch (CommandLineException e)
            {
                System.Console.WriteLine(e.Message);
                parser.ShowUsage();
                return;
            }

            var setting = new Settings();

            setting.PtcUsername = userArgument.Value;
            setting.PtcPassword = passwordArgument.Value;
            setting.UseTeleportInsteadOfWalking = teleportArgument.Value;

            string path = Path.Combine(Directory.GetCurrentDirectory(), setting.PtcUsername);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }


            ServicePointManager.ServerCertificateValidationCallback = Validator;
            Logger.SetLogger();

            Task.Run(() =>
            {
                try
                {
                    new Logic.Logic(setting).Execute().Wait();
                }
                catch (PtcOfflineException)
                {
                    Logger.Write("PTC Servers are probably down OR your credentials are wrong. Try google", LogLevel.Error);
                    Logger.Write("Trying again in 60 seconds...");
                    Thread.Sleep(60000);
                    new Logic.Logic(new Settings()).Execute().Wait();
                }
                catch (AccountNotVerifiedException)
                {
                    Logger.Write("Account not verified. - Exiting");
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Logger.Write($"Unhandled exception: {ex}", LogLevel.Error);
                    new Logic.Logic(new Settings()).Execute().Wait();
                }
            });
             System.Console.ReadLine();
        }

        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
    }
}