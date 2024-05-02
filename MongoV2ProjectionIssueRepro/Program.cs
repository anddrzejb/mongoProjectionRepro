using System.Security.Authentication;
using System.Text.Json;
using EphemeralMongo;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

ConventionRegistry.Register("camelCaseConvention",
    new ConventionPack
    {
        new CamelCaseElementNameConvention(),
        new IgnoreExtraElementsConvention(true),
    },
    t => true);

using (var runner = MongoRunner.Run())
{
    //var mongoSettings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://localhost:27017"));
    var mongoSettings = MongoClientSettings.FromUrl(new MongoUrl(runner.ConnectionString));
    mongoSettings.LinqProvider = LinqProvider.V2;
    mongoSettings.SslSettings = new SslSettings { EnabledSslProtocols = SslProtocols.None };
    var client = new MongoClient(mongoSettings);
    var collection = client.GetDatabase("test").GetCollection<MyDocument>("test");

    var document = new MyDocument
    {
        Id = Guid.NewGuid(),
        Name = $"Test{DateTime.Now.Ticks.ToString()[^1]}",
        ActiveSince = DateTime.UtcNow,
        IsActive = true
    };
    collection.InsertOne(document);

    var projectionTyped = Builders<MyDocument>.Projection.Expression(x => new MyDocument { Id = x.Id, Name = x.Name });
    var queryTyped = collection.Aggregate().Project(projectionTyped).Limit(1);
    Console.WriteLine("Query with projection to typed: " + queryTyped);
    var resultTyped = await queryTyped.FirstOrDefaultAsync();
    Console.WriteLine($"Result with projection to {resultTyped.GetType().Name}: {JsonSerializer.Serialize(resultTyped)}");

    var projectionAnonymous = Builders<MyDocument>.Projection.Expression(x => new { x.Id, x.Name });
    var queryAnonymous = collection.Aggregate().Project(projectionAnonymous).Limit(1);
    Console.WriteLine("Query with projection to anonymous: " + queryAnonymous);
    var resultAnonymous = await queryAnonymous.FirstOrDefaultAsync();
    Console.WriteLine($"Result with projection to {resultAnonymous.GetType().Name}: {JsonSerializer.Serialize(resultAnonymous)}");

}

record MyDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime ActiveSince { get; set; }
    public bool IsActive { get; set; }
}