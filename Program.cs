using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure.IO.GraphBinary;
using Gremlin.Net.Structure.IO.GraphSON;
using JanusGraph.Net.IO.GraphBinary;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var endpoint = "localhost";
        var gremlinServer = new GremlinServer(endpoint, 8182, enableSsl: false);
        var graphSONMessageSerializer = new GraphSON3MessageSerializer();

        using var gremlinClient = new GremlinClient(gremlinServer, new GraphBinaryMessageSerializer(JanusGraphTypeSerializerRegistry.Instance));

        var remoteConnection = new DriverRemoteConnection(gremlinClient, "g");

        await gremlinClient.SubmitAsync<dynamic>("g.V().drop()");
        var g = Traversal().WithRemote(remoteConnection);

        try
        {
            // add node  (identity , ticket , group)

            g.AddV("Identity").Property(T.Id, "Identity1").Next();
            g.AddV("Identity").Property(T.Id, "Identity2").Next();
            g.AddV("Identity").Property(T.Id, "Identity3").Next();

            //  add node  (transaction)
            var transaction = g.Tx();
            var batch = transaction.Begin();
            batch.AddV("TicketBook").Property(T.Id, "TicketBook1").Next();
            batch.AddV("TicketBook").Property(T.Id, "TicketBook2").Next();
            batch.AddV("TicketBook").Property(T.Id, "TicketBook3").Next();
            batch.AddV("TicketBook").Property(T.Id, "TicketBook4").Next();
            batch.AddV("group").Property(T.Id, "Identity2's group").Next();
            await transaction.CommitAsync();

            // add relationship (emit)
            g.V().HasLabel("Identity")
                 .Has(T.Id, "Identity1")
                 .AddE("emit")
                 .Property("time", DateTime.Now.ToString())
                 .To(
                     __.V().HasLabel("TicketBook")
                           .Has(T.Id, "TicketBook1")
                 )
                 .Next();

            g.V().HasLabel("Identity")
                .Has(T.Id, "Identity1")
                .AddE("emit")
                .Property("time", DateTime.Now.ToString())
                .To(
                    __.V().HasLabel("TicketBook")
                          .Has(T.Id, "TicketBook2")
                )
                .Next();

            g.V().HasLabel("Identity")
                 .Has(T.Id, "Identity2")
                 .AddE("emit")
                 .Property("time", DateTime.Now.ToString())
                 .To(
                     __.V().HasLabel("TicketBook")
                           .Has(T.Id, "TicketBook3")
                 )
                 .Next();
            g.V().HasLabel("Identity")
                 .Has(T.Id, "Identity2")
                 .AddE("emit")
                 .Property("time", DateTime.Now.ToString())
                 .To(
                     __.V().HasLabel("TicketBook")
                           .Has(T.Id, "TicketBook4")
                 )
                 .Next();

            // add relationship (tansfer)
            g.V().HasLabel("TicketBook")
                 .Has(T.Id, "TicketBook3")
                 .AddE("transfer")
                 .Property("time", DateTime.Now.ToString())
                 .To(
                    __.V().HasLabel("Identity")
                          .Has(T.Id, "Identity1")
                 )
                 .Next();

            // add relationship (share)
            g.V().HasLabel("Identity")
                 .Has(T.Id, "Identity1")
                 .Out("emit")
                 .AddE("share")
                 .To(
                    __.V().HasLabel("Identity")
                          .Has(T.Id, "Identity2")
                 )
                 .Next();

            // add relationship (owner group)
            g.V().HasLabel("Identity")
                 .Has(T.Id, "Identity2")
                 .AddE("owner")
                 .Property("time", DateTime.Now.ToString())
                 .To(
                    __.V().HasLabel("group")
                          .Has(T.Id, "Identity2's group")
                 )
                 .Next();

            // add relationship (join group)
            g.V().HasLabel("Identity")
                 .Has(T.Id, "Identity3")
                 .AddE("join")
                 .Property("time", DateTime.Now.ToString())
                 .To(
                   __.V().HasLabel("group")
                         .Has(T.Id, "Identity2's group")
                 )
                 .Next();

            // query
            var result1 = g.V().HasLabel("Identity")
                               .Has(T.Id, "Identity1")
                               .Union<dynamic>(

                                 __.OutE("emit").InV().ValueMap<string, object>(),
                                 __.InE("transfer").OutV().ValueMap<string, object>(),
                                 __.InE("share").OutV().ValueMap<string, object>(),
                                 __.OutE("join").InV().InE("owner").OutV().OutE("emit").InV().ValueMap<string, object>()
                              )
                              .ToList();

            var result2 = g.V().HasLabel("Identity")
                               .Has(T.Id, "Identity2")
                               .Union<dynamic>(

                                __.OutE("emit").InV().ValueMap<string, object>(),
                                __.InE("transfer").OutV().ValueMap<string, object>(),
                                __.InE("share").OutV().ValueMap<string, object>(),
                                __.OutE("join").InV().InE("owner").OutV().OutE("emit").InV().ValueMap<string, object>()
                             )
                             .ToList();

            var result3 = g.V().HasLabel("Identity")
                           .Has(T.Id, "Identity3")
                           .Union<dynamic>(

                             __.OutE("emit").InV().ValueMap<string, object>(),          // 買入
                             __.InE("transfer").OutV().ValueMap<string, object>(),      // 受贈
                             __.InE("share").OutV().ValueMap<string, object>(),         // 被動共享
                             __.OutE("join").InV().InE("owner").OutV().OutE("emit").InV().ValueMap<string, object>() // 主動共享

                          )
                          .ToList();
        }
        catch (Exception e)
        {
            Console.WriteLine("{0}", e);
        }

        Console.ReadLine();
    }
}