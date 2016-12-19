using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookCalculation
{
    public static class dbConnectionStocks
    {
        private static String connectionString = "mongodb://ec2-35-164-218-97.us-west-2.compute.amazonaws.com";
        private static MongoClient Client = new MongoDB.Driver.MongoClient(connectionString);
        private static IMongoDatabase dbtemp = Client.GetDatabase("boerse");
        public static IMongoCollection<BsonDocument> _db = dbtemp.GetCollection<BsonDocument>("stocks");
    }

	public static class dbConnectionOrders
	{
		private static String connectionString = "mongodb://ec2-35-164-218-97.us-west-2.compute.amazonaws.com";
		private static MongoClient Client = new MongoDB.Driver.MongoClient(connectionString);
		private static IMongoDatabase dbtemp = Client.GetDatabase("boerse");
		public static IMongoCollection<BsonDocument> _db = dbtemp.GetCollection<BsonDocument>("orders");
	}

	public static class dbConnectionAktienverlauf
	{
		private static String connectionString = "mongodb://ec2-35-164-218-97.us-west-2.compute.amazonaws.com";
		private static MongoClient Client = new MongoDB.Driver.MongoClient(connectionString);
		private static IMongoDatabase dbtemp = Client.GetDatabase("boerse");
		public static IMongoCollection<BsonDocument> _db = dbtemp.GetCollection<BsonDocument>("aktienverlauf");
	}
}
