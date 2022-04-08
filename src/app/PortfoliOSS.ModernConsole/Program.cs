using System;
using Akka.Actor;
using Serilog;
using System.IO;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Streams;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PortfoliOSS.ModernDomain;
using PortfoliOSS.ModernDomain.Actors;
using PortfoliOSS.ModernData;

namespace PortfoliOSS.ModernConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContextFactory<PortfoliOSSDBContext>();
                })
                .ConfigureLogging(x=>x.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning))
                .Build();

            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .MinimumLevel.Information()
                .CreateLogger();

            Log.Logger = logger; 
            var configText = await File.ReadAllTextAsync("Config.Hocon"); // TODO: Extract DB connection string into config/secret
            var actorSystem = ActorSystem.Create(Constants.APP_NAME, configText);
            var writer = actorSystem.ActorOf(Props.Create<CreateViewsActor>(host.Services.GetRequiredService<IDbContextFactory<PortfoliOSSDBContext>>()), "writer");

            var readJournal = PersistenceQuery.Get(actorSystem)
                .ReadJournalFor<SqlReadJournal>("akka.persistence.query.my-read-journal");
            var materializer = actorSystem.Materializer();
            
            logger.Information("Trying to stream the events");
            readJournal.CurrentAllEvents(new Sequence(1289878)).RunForeach(ev =>
            {
                writer.Tell(ev);
            }, materializer).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // TODO: add error handling here
                    logger.Error(task.Exception, "STREAM FAILED"); 
                } else if (task.IsCanceled)
                {
                    logger.Warning("Stream canceled!"); 
                }
                else
                {
                    logger.Information("Stream ended successfully");
                }
                actorSystem.Terminate();
            });

            await actorSystem.WhenTerminated;
        }
    }
}
