using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace policy_issue.Services
{
    public class MongoConnector
    {
        MongoClient _client;
        IMongoDatabase _database;

        public MongoConnector(string connectionString, string database = "mongodb")
        {
            _client = new MongoClient(connectionString);


            _database = _client.GetDatabase(database);

        }

        public string GetCollectionData(string database, string collectionName, string queryField, string queryValue)
        {
            var db = _client.GetDatabase(database);
            var collection = db.GetCollection<BsonDocument>(collectionName);


            if (string.IsNullOrEmpty(queryField))
            {
                var result = collection.Find(new BsonDocument()).Limit(10).ToList();
                return result.ToJson();
            }
            else
            {
                var builders = Builders<BsonDocument>.Filter.Eq(queryField, queryValue);
                var result = collection.Find(builders).FirstOrDefault();
                return result.ToJson();
            }
        }

        public void InsertData(string collectionName, JObject content)
        {
            if(!_database.ListCollectionNames().ToList().Any(x=>x == collectionName))
            {
                _database.CreateCollection(collectionName);
            }
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            collection.InsertOne(BsonDocument.Parse(content.ToString()));
        }

        public JObject GetPolicyObject(string quoteId)
        {
            return new JObject(new JProperty("vehicles", GetVehicle(quoteId)),
            new JProperty("drivers", GetDrivers(quoteId)),
            new JProperty("customer", GetCustomer(quoteId).FirstOrDefault() ?? new JArray()),
            new JProperty("coverages",  GetRate(quoteId)));
        }

        public JArray GetCustomer(string quoteId)
        {
            return GetData("col_lrqi_customers", quoteId, "customer");
        }

        public JArray GetVehicle(string quoteId)
        {
            return GetData("col_lrqi_vehicles", quoteId, "vehicles");
        }

        public JArray GetDrivers(string quoteId)
        {
            return GetData("col_lrqi_drivers", quoteId, "drivers");
        }

        public JArray GetRate(string quoteId)
        {
            return GetData("col_lrqi_rate_issue", quoteId, "coverages");
        }

        public JArray GetIncidents(string quoteId)
        {
            return GetData("col_lrqi_incidents", quoteId, "incidents");
        }

        private JArray GetData(string collectionName, string quoteId, string objectName)
        {
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            var builders = Builders<BsonDocument>.Filter.Eq("quoteId", quoteId);
            var result = collection.Find(builders).ToList();

            RemoveIdObject(result);
            return new JArray { result.Select(x=> JObject.Parse(x.ToJson())).ToArray() };
            //return new JObject(new JProperty(objectName,new JArray { result.Select(x=> new JValue(x.ToJson())).ToArray() } ));

        }

        private static void RemoveIdObject(List<BsonDocument> response)
        {
            foreach(BsonDocument doc in response)
            {
                BsonElement bsonElement;
                if (doc.TryGetElement("_id", out bsonElement))
                    doc.RemoveElement(bsonElement);
                if (doc.TryGetElement("quoteId", out bsonElement))
                    doc.RemoveElement(bsonElement);
                
            }
        }
    }
}
