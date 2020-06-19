using MongoDB.Bson;
using MongoDB.Driver;

namespace policy_issue.Services
{
    public class MongoConnector
    {
        MongoClient _client;
        public MongoConnector(string connectionString)
        {
            _client = new MongoClient(connectionString);

        }

        public string GetCollectionData(string database, string collectionName, string queryField, string queryValue)
        {
            var db = _client.GetDatabase(database);
            var collection = db.GetCollection<BsonDocument>(collectionName);

            
            if(string.IsNullOrEmpty(queryField))
            {
                var result = collection.Find(new BsonDocument()).FirstOrDefault();
                return result.ToString();
            }
            else
            {
                var builders = Builders<BsonDocument>.Filter.Eq(queryField, queryValue);
                 var result = collection.Find(builders).FirstOrDefault();
                return result.ToString();
            }
        }
    }
}