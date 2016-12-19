using Grapevine.Interfaces.Server;
using Grapevine.Server;
using Grapevine.Server.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDBConnection;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Collections;

namespace Boerse
{
	class BoersenApplication
	{
		static void Main(string[] args)
		{
			Debug.WriteLine("Started!");
			ServerSettings settings = new ServerSettings();
			settings.Host = "ec2-35-164-218-97.us-west-2.compute.amazonaws.com";    //93.82.35.63 
																					//settings.Host = "localhost";    //78.104.199.75
			settings.Port = "8080";
			insertInitialData();
			try
			{
				using(var server = new RestServer(settings))
				{
					server.LogToConsole().Start();
					Console.ReadLine();
					server.Stop();
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine("Error: " + ex);
				Console.ReadLine();
			}
		}

		/// <summary>
		/// This function should insert data into the database
		/// </summary>
		private static void insertInitialData()
		{
			string result = string.Empty;
			Stock stock = new Stock();
			var dbStocks = dbConnectionStocks._db;
			var entrys = dbStocks.Find(new BsonDocument()).ToList();
			foreach(var item in entrys)
			{
				stock.aktienID = new Guid(item.GetElement("aktienID").Value.ToString());
				stock.name = item.GetElement("name").Value.ToString();
				stock.course = double.Parse(item.GetElement("course").Value.ToString(), System.Globalization.NumberStyles.Any);
				stock.amount = Int32.Parse(item.GetElement("amount").Value.ToString(), System.Globalization.NumberStyles.Any);

				result += JsonConvert.SerializeObject(stock);
			}
			if(entrys.Count == 7)
				createMoreStocks();
			if(!result.Equals(string.Empty))
				return;

			var entry = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Super Firma von Andi"},
				{"course", 100},
				{"amount", 1000}
			};
			dbConnectionStocks._db.InsertOne(entry);

			var entry2 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Super Firma von Mike"},
				{"course", 100},
				{"amount", 1000}
			};
			dbConnectionStocks._db.InsertOne(entry2);

			var entry3 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Super Firma von Bugsdehude"},
				{"course", 50},
				{"amount", 1000}
			};
			dbConnectionStocks._db.InsertOne(entry3);

			var entry4 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Super Firma von Blablub"},
				{"course", 70},
				{"amount", 1000}
			};
			dbConnectionStocks._db.InsertOne(entry4);

			var entry5 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Super Firma von Cyka"},
				{"course", 10},
				{"amount", 100}
			};
			dbConnectionStocks._db.InsertOne(entry5);

			var entry6 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Super Firma von Blyat"},
				{"course", 15},
				{"amount", 150}
			};
			dbConnectionStocks._db.InsertOne(entry6);

			var entry7 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Super Firma von RUSH B"},
				{"course", 100},
				{"amount", 1000}
			};
			dbConnectionStocks._db.InsertOne(entry7);
		}

		private static void createMoreStocks()
		{
			var entry = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Facebook"},
				{"course", 100},
				{"amount", 1000000}
			};
			dbConnectionStocks._db.InsertOne(entry);

			var entry2 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Google"},
				{"course", 70},
				{"amount", 1000000}
			};
			dbConnectionStocks._db.InsertOne(entry2);

			var entry3 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Nintendo"},
				{"course", 50},
				{"amount", 1000000}
			};
			dbConnectionStocks._db.InsertOne(entry3);

			var entry4 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "CSGO"},
				{"course", 70},
				{"amount", 9999999}
			};
			dbConnectionStocks._db.InsertOne(entry4);

			var entry5 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Russia"},
				{"course", 10},
				{"amount", 100000}
			};
			dbConnectionStocks._db.InsertOne(entry5);

			var entry6 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Cobblestone"},
				{"course", 15},
				{"amount", 15123410}
			};
			dbConnectionStocks._db.InsertOne(entry6);

			var entry7 = new BsonDocument
			{
				{"aktienID", Guid.NewGuid().ToString() },
				{"name", "Dust 2"},
				{"course", 100},
				{"amount", 987654321}
			};
			dbConnectionStocks._db.InsertOne(entry7);
		}
	}

	public class KeyVal<Key, Buy, Sell>
	{
		public Key limit { get; set; }
		public Buy amountBuy { get; set; }
		public Sell amountSell { get; set; }

		public KeyVal() { }

		public KeyVal(Key key, Buy buy, Sell sell)
		{
			limit = key;
			amountBuy = buy;
			amountSell = sell;
		}
	}
}


/*
 private static async void updateAktie(BsonDocument stock, KeyVal<double, int, int> best)
		{
			//Setting the price for the specific aktienID
			//Get the _id of the MongoDB document
			var _id = stock.GetElement("_id").Value.ToString();
			var filter = Builders<BsonDocument>.Filter.Eq("_id", _id);
			var update = Builders<BsonDocument>.Update.Set("course", best.limit).Set("amount", best.amountSell);
			var result = await dbConnectionStocks._db.UpdateOneAsync(filter, update);
		}

		private static async void updateOrder(BsonDocument order, MainOrder item)
		{
			var order_id = order.GetElement("_id").Value.ToString();
			var orderFilter = Builders<BsonDocument>.Filter.Eq("_id", order_id);
			var orderUpdate = Builders<BsonDocument>.Update.Set("amount", item.receivedOrder.amount).Set("statusOfOrder", item.statusOfOrder);
			var orderResult = await dbConnectionOrders._db.UpdateOneAsync(orderFilter, orderUpdate);
		}
	 
	 */
