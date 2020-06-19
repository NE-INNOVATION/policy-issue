using System;
using System.Collections.Generic;
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

        public JObject GetPolicyObject(string quoteId)
        {
            return new JObject(
                new JProperty("vehicles", GetVehicle(quoteId)),
                new JProperty("drivers", GetDrivers(quoteId)),
                new JProperty("Customer", GetCustomer(quoteId)),
                new JProperty("coverages", GetRate(quoteId))
                
                );
        }

        public JObject GetCustomer(string quoteId)
        {
            return GetData("col_lrqi_customers", quoteId);
        }

        public JObject GetVehicle(string quoteId)
        {
            return GetData("col_lrqi_vehicles", quoteId);
        }

        public JObject GetDrivers(string quoteId)
        {
            return GetData("col_lrqi_drivers", quoteId);
        }

        public JObject GetRate(string quoteId)
        {
            return GetData("col_lrqi_rate_issue", quoteId);
        }

        public JObject GetIncidents(string quoteId)
        {
            return GetData("col_lrqi_incidents", quoteId);
        }

        private JObject GetData(string collectionName, string quoteId)
        {
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            var builders = Builders<BsonDocument>.Filter.Eq("quoteId", quoteId);
            var result = collection.Find(builders).ToList();

            if (result.Count > 0) 
            {
                RemoveIdObject(result);
                return JObject.Parse(result.ToJson());

            }
            return new JObject(new JProperty("Message", "No data found"));
        }

        private static void RemoveIdObject(List<BsonDocument> response)
        {
            foreach(BsonDocument doc in response)
            {
                BsonElement bsonElement;
                if (doc.TryGetElement("_id", out bsonElement))
                    doc.RemoveElement(bsonElement);
                
            }
        }
    }
}