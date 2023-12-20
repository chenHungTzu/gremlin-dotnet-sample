using System.Transactions;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;


class Program
{
     static async Task Main(string[] args)
     {

          var endpoint = "localhost";
          var gremlinServer = new GremlinServer(endpoint, 8182, enableSsl: false);

          using var gremlinClient = new GremlinClient(gremlinServer);
          var remoteConnection = new DriverRemoteConnection(gremlinClient, "g");

          await gremlinClient.SubmitAsync<dynamic>("g.V().drop()");
          var g = Traversal().WithRemote(remoteConnection);

          try
          {
               // add node  (identity , ticket , group) 
               g.AddV("Identity").Property("tenantId", "Identity1").Next();
               g.AddV("Identity").Property("tenantId", "Identity2").Next();
               g.AddV("Identity").Property("tenantId", "Identity3").Next();

               g.AddV("TicketBook").Property("id", "TicketBook1").Next();
               g.AddV("TicketBook").Property("id", "TicketBook2").Next();
               g.AddV("TicketBook").Property("id", "TicketBook3").Next();
               g.AddV("TicketBook").Property("id", "TicketBook4").Next();
               g.AddV("group").Property("id", "Identity2's group").Next();


               // add relationship (emit)
               g.V().HasLabel("Identity")
                    .Has("tenantId", "Identity1")
                    .AddE("emit")
                    .Property("time", DateTime.Now.ToString())
                    .To(
                        __.V().HasLabel("TicketBook")
                              .Has("id", "TicketBook1")
                    )
                    .Next();

               g.V().HasLabel("Identity")
                   .Has("tenantId", "Identity1")
                   .AddE("emit")
                   .Property("time", DateTime.Now.ToString())
                   .To(
                       __.V().HasLabel("TicketBook")
                             .Has("id", "TicketBook2")
                   )
                   .Next();


               g.V().HasLabel("Identity")
                    .Has("tenantId", "Identity2")
                    .AddE("emit")
                    .Property("time", DateTime.Now.ToString())
                    .To(
                        __.V().HasLabel("TicketBook")
                              .Has("id", "TicketBook3")
                    )
                    .Next();
               g.V().HasLabel("Identity")
                    .Has("tenantId", "Identity2")
                    .AddE("emit")
                    .Property("time", DateTime.Now.ToString())
                    .To(
                        __.V().HasLabel("TicketBook")
                              .Has("id", "TicketBook4")
                    )
                    .Next();


               // add relationship (tansfer) 
               g.V().HasLabel("TicketBook")
                    .Has("id", "TicketBook3")
                    .AddE("transfer")
                    .Property("time", DateTime.Now.ToString())
                    .To(
                       __.V().HasLabel("Identity")
                             .Has("tenantId", "Identity1")
                    )
                    .Next();


               // add relationship (share)       
               g.V().HasLabel("Identity")
                    .Has("tenantId", "Identity1")
                    .Out("emit")
                    .AddE("share")
                    .To(
                       __.V().HasLabel("Identity")
                             .Has("tenantId", "Identity2")
                    )
                    .Next();

               // add relationship (owner group) 
               g.V().HasLabel("Identity")
                    .Has("tenantId", "Identity2")
                    .AddE("owner")
                    .Property("time", DateTime.Now.ToString())
                    .To(
                       __.V().HasLabel("group")
                             .Has("id", "Identity2's group")
                    )
                    .Next();


               // add relationship (join group) 
               g.V().HasLabel("Identity")
                    .Has("tenantId", "Identity3")
                    .AddE("join")
                    .Property("time", DateTime.Now.ToString())
                    .To(
                      __.V().HasLabel("group")
                            .Has("id", "Identity2's group")
                    )
                    .Next();


               // query  
               var result1 = g.V().HasLabel("Identity")
                                  .Has("tenantId", "Identity1")
                                  .Union<dynamic>(

                                    __.OutE("emit").InV().ValueMap<string, Object>(),
                                    __.InE("transfer").OutV().ValueMap<string, Object>(),
                                    __.InE("share").OutV().ValueMap<string, Object>(),
                                    __.OutE("join").InV().InE("owner").OutV().OutE("emit").InV().ValueMap<string, Object>()
                                 )
                                 .ToList();

               var result2 = g.V().HasLabel("Identity")
                                  .Has("tenantId", "Identity2")
                                  .Union<dynamic>(

                                   __.OutE("emit").InV().ValueMap<string, Object>(),
                                   __.InE("transfer").OutV().ValueMap<string, Object>(),
                                   __.InE("share").OutV().ValueMap<string, Object>(),
                                   __.OutE("join").InV().InE("owner").OutV().OutE("emit").InV().ValueMap<string, Object>()
                                )
                                .ToList();

               var result3 = g.V().HasLabel("Identity")
                              .Has("tenantId", "Identity3")
                              .Union<dynamic>(

                                __.OutE("emit").InV().ValueMap<string, Object>(),          // 買入
                                __.InE("transfer").OutV().ValueMap<string, Object>(),      // 受贈
                                __.InE("share").OutV().ValueMap<string, Object>(),         // 被動共享
                                __.OutE("join").InV().InE("owner").OutV().OutE("emit").InV().ValueMap<string, Object>() // 主動共享

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