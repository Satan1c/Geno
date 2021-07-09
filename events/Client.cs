using DSharpPlus;
using DSharpPlus.EventArgs;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Geno.events
{
    internal class Client
    {
        public static async Task OnReady(DiscordClient client, ReadyEventArgs args)
        {
            Bot.reportChannel = await client.GetChannelAsync(686099575840178201);
            var newGuilds = new List<models.Server>();
            var coll = Bot.mongo.GetDatabase("servers").GetCollection<models.Server>("settings");
            var raw = await coll.FindAsync(x => x._id != "");
            var db = raw.ToList();

            foreach (var i in client.Guilds)
            {
                if (!db.Any((x) => x._id == i.Key.ToString()))
                    newGuilds.Add(new models.Server(i.Value));
            }

            if (newGuilds.Count >= 1)
            {
                await coll.InsertManyAsync(newGuilds);
            }

            Console.WriteLine($"\n{client.CurrentUser.Username} is ready\n");
            GC.Collect();
        }

        public static Task OnError(DiscordClient client, ClientErrorEventArgs args)
        {
            GC.Collect();
            //Console.WriteLine($"{args.EventName}\n{args.Exception}\n{args.Exception.Message}\n{args.Exception.StackTrace}");
            return Task.CompletedTask;
        }
    }
}