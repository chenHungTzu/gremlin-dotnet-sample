using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Structure;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;
using static Gremlin.Net.Process.Traversal.T;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var endpoint = "localhost";
            var gremlinServer = new GremlinServer(endpoint, 8182, enableSsl: false);

            var gremlinClient = new GremlinClient(gremlinServer);
          
            var remoteConnection = new DriverRemoteConnection(gremlinClient, "g");
            var g = Traversal().WithRemote(remoteConnection);
            g.AddV("Person").Property("id", "1").Next();
            g.AddV("Person").Property("id", "2").Next();
            g.AddV("Person").Property("id", "3").Next();
            var output = g.V().Limit<Vertex>(3).ToList();
            foreach (var item in output)
            {
                Console.WriteLine(item);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("{0}", e);
        }
    }
}