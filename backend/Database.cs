using MongoDB.Driver;
using MongoDB.Bson;

namespace Database
{

    class User
    {
        public ObjectId _id { get; set; }
        public string username { get; set; }

        public User(string username)
        {
            this.username = username;
        }
    }

    class Ranking
    {
        public User user { get; set; }
        public int played { get; set; }
        public int won { get; set; }

        public Ranking(User user, int played, int won)
        {

            this.user = user;
            this.played = played;
            this.won = won;
        }
    }

    class DB
    {
        private MongoClient client;
        private IMongoDatabase db;
        private MongoDB.Driver.IMongoCollection<User> users;
        private MongoDB.Driver.IMongoCollection<Ranking> rankings;
        public DB()
        {
            client = new MongoClient(Environment.GetEnvironmentVariable("MONGO_URI"));
            db = client.GetDatabase("Pong");
            users = db.GetCollection<User>("Users");
            rankings = db.GetCollection<Ranking>("Rankings");
        }

        public async Task<User> FindOrCreateUser(string username)
        {
            User newUser = new User(username);
            await users.InsertOneAsync(newUser);
            var dbUser = await users.Find<User>(x => x.username == username).Limit(1).FirstAsync();
            return dbUser;
        }

        private async void CreateUser()
        {
            User user = new User("Test");
            await users.InsertOneAsync(user);

            var created = await users.FindAsync<User>(new BsonDocument());
            Console.WriteLine(created.First().username);

            Ranking doc = new Ranking(user, 4, 2);
            rankings.InsertOne(doc);

        }
    }
}