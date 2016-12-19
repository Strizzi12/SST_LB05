using Boerse;
using Grapevine;
using Grapevine.Interfaces.Server;
using Grapevine.Server.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDBConnection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 
/// </summary>
[RestResource]
public class BoersenResource
{

	#region ### REST INTERFACES ###

	/// <summary>
	/// This interface should return an JSON array with all the stocks in our market.
	/// "aktienID": "GUID",
	/// "name":"String",
	/// "course": "double",
	/// "amount": "int"
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	[RestRoute(HttpMethod = Grapevine.Shared.HttpMethod.GET, PathInfo = "/boerse/listCourses")]
	public IHttpContext ListCourses(IHttpContext context)
	{
		string stockListAsJsonString = getStockListFromDb();
		context.Response.ContentType = Grapevine.Shared.ContentType.JSON;
		context.Response.ContentEncoding = Encoding.UTF8;
		context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.Ok, stockListAsJsonString);
		return context;
	}

	/// <summary>
	/// This interface should gets an JSON array with the order from a customer
	/// "orderID": "GUID",
	/// "aktienID": "GUID",
	/// "amount": int,
	/// “limit”: double // max Amount Customer wants to pay
	/// "timestamp": "int", // unix timestamp
	/// "hash": "String" // can be null
	///
	/// RESPONSE:
	/// 404: aktienID not found
	/// 200: OK, but check again with /check in x minutes
	/// 500: unknown error
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	[RestRoute(HttpMethod = Grapevine.Shared.HttpMethod.POST, PathInfo = "/boerse/buy")]
	public IHttpContext Buy(IHttpContext context)
	{
		return storeInDatabase(context, BUYORSELL.Buy);
	}

	/// <summary>
	/// This interface should gets an JSON array with the order from a customer
	/// "orderID": "GUID",
	/// "aktienID": "GUID",
	/// "amount": "int",
	/// “limit”: double		// min Amount Customer wants to have for the stock
	/// "timestamp": int,	// unix timestamp
	/// "hash": "String"	// can be null
	///
	/// RESPONSE:
	/// 200: OK, but check again with / check in x minutes
	/// 404: aktienID not found
	/// 500: unknown error
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	[RestRoute(HttpMethod = Grapevine.Shared.HttpMethod.POST, PathInfo = "/boerse/sell")]
	public IHttpContext Sell(IHttpContext context)
	{
		return storeInDatabase(context, BUYORSELL.Sell);
	}

	/// <summary>
	/// This interface should check the order from a customer
	/// "orderID": "GUID",
	///	"hash": "String" // can be null
	///
	/// RESPONSE:	The Response is a JSON array if OK
	/// 200 OK:
	///
	///	"orderID": "GUID",
	///	"price": double // price per good
	///	"amount":int
	///	"status":int // 0 if successfull, 1 in progress, 2 denied, 3 not enough goods, 4 wrong price
	///	"hash": "String" // can be null
	///
	///	404: orderID not found
	///	500: unknown internal error
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	[RestRoute(HttpMethod = Grapevine.Shared.HttpMethod.POST, PathInfo = "/boerse/check")]
	public IHttpContext Check(IHttpContext context)
	{
		string payload = context.Request.Payload.ToString();        //Liefert einen JSON String mit escaped zeichen zurück
		JToken token = JToken.Parse(payload);
		JObject json = JObject.Parse(token.ToString());
		if(payload == null || payload.Equals(""))
		{
			context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.InternalServerError, "Oops, something went wrong!");
			return context;
		}
		CheckOrder orderObject = JsonConvert.DeserializeObject<CheckOrder>(json.ToString());
		try
		{
			bool foundOrder = false;
			var dbOrders = dbConnectionOrders._db;
			var entrys = dbOrders.Find(new BsonDocument()).ToList();
			foreach(var item in entrys)
			{
				//Getting the order from the database
				Guid deserializedOrderID = new Guid(item.GetElement("orderID").Value.ToString());
				Guid deserializedAktienID = new Guid(item.GetElement("aktienID").Value.ToString());
				int deserializedAmount = Int32.Parse(item.GetElement("amount").Value.ToString(), System.Globalization.NumberStyles.Any);
				double deserializedLimit = Double.Parse(item.GetElement("limit").Value.ToString(), System.Globalization.NumberStyles.Any);
				int deserializedTimestamp = Int32.Parse(item.GetElement("timestamp").Value.ToString(), System.Globalization.NumberStyles.Any);
				string deserializedHash = item.GetElement("hash").Value.ToString();
				int deserializedUseCase = Int32.Parse(item.GetElement("useCase").Value.ToString(), System.Globalization.NumberStyles.Any);
				int deserializedStatusOfOrder = Int32.Parse(item.GetElement("statusOfOrder").Value.ToString(), System.Globalization.NumberStyles.Any);
				
				if(orderObject.orderID.Equals(deserializedOrderID))
				{
					foundOrder = true;
					//Find the current price of the stock
					double course = 0L;
					var dbStocks = dbConnectionStocks._db;
					var stockEntrys = dbStocks.Find(new BsonDocument()).ToList();
					foreach(var stock in stockEntrys)
					{
						Guid aktienID = new Guid(stock.GetElement("aktienID").Value.ToString());
						if(aktienID.Equals(deserializedAktienID))
						{
							course = double.Parse(stock.GetElement("course").Value.ToString(), System.Globalization.NumberStyles.Any);
							break;	//Since the aktienID should be uniquely, no need to try the remaining stocks in the list
						}
					}
					
					//Create a Response Order
					ResponseOrder responseOrder = new ResponseOrder();
					responseOrder.orderID = deserializedOrderID;
					responseOrder.status = deserializedStatusOfOrder;
					responseOrder.amount = deserializedAmount;
					responseOrder.price = deserializedLimit;
					string response = JsonConvert.SerializeObject(responseOrder);

					response.Replace("state", "status");
					//Send back a response
					context.Response.ContentType = Grapevine.Shared.ContentType.JSON;
					context.Response.ContentEncoding = Encoding.UTF8;
					context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.Ok, response);
					break;
				}
			}
			if(!foundOrder)
				context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.NotFound, "OrderID not found!");
		}
		catch(Exception ex)
		{
			Debug.WriteLine(ex);
		}
		return context;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	[RestRoute]
	public IHttpContext Info(IHttpContext context)
	{
		context.Response.SendResponse("This is an info! :)");
		return context;
	}
	#endregion

	#region ### HELPER FUNCTIONS ###

	/// <summary>
	/// This function is used to simplify the 2 REST interfaces if changes are needed!
	/// </summary>
	/// <param name="context"></param>
	/// <param name="whatToDo"></param>
	/// <returns></returns>
	private IHttpContext storeInDatabase(IHttpContext context, BUYORSELL whatToDo)
	{
		string payload = context.Request.Payload.ToString();        //Liefert einen JSON String mit escaped zeichen zurück
		JToken token = JToken.Parse(payload);
		JObject json = JObject.Parse(token.ToString());
		if(payload == null || payload.Equals(""))
		{
			context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.InternalServerError, "Oops, something went wrong!");
			return context;
		}
		Order orderObject = JsonConvert.DeserializeObject<Order>(json.ToString());
		bool isPresent = checkIfStockExists(orderObject);
		if(isPresent)
		{
			//Die Order ins Orderbuch eintragen.
			try
			{
				var dbOrders = dbConnectionOrders._db;
				MainOrder mainOrder = new MainOrder(orderObject, whatToDo);
				var test = new BsonDocument()
				{
					{"orderID", mainOrder.receivedOrder.orderID.ToString()},
					{"aktienID", mainOrder.receivedOrder.aktienID.ToString()},
					{"amount", mainOrder.receivedOrder.amount},
					{"limit", mainOrder.receivedOrder.limit},
					{"timestamp", mainOrder.receivedOrder.timestamp},
					{"hash", mainOrder.receivedOrder.hash ?? ""},
					{"useCase", mainOrder.useCase},
					{"statusOfOrder", mainOrder.statusOfOrder}
				};
				dbOrders.InsertOne(test);
			}
			catch(Exception ex)
			{
				Debug.WriteLine(ex);
				context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.InternalServerError, "Oops, something went wrong!");
				return context;
			}

			context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.Ok, "Check again with /check");
			return context;
		}
		else
		{
			context.Response.SendResponse(Grapevine.Shared.HttpStatusCode.NotFound, "aktienID not found");
			return context;
		}
	}

	/// <summary>
	/// This functions checks if the given aktienID is present in our market.
	/// </summary>
	/// <param name="order"></param>
	/// <returns></returns>
	private bool checkIfStockExists(Order order)
	{
		try
		{
			var dbStocks = dbConnectionStocks._db;
			var entrys = dbStocks.Find(new BsonDocument()).ToList();
			foreach(var item in entrys)
			{
				Guid aktienID = new Guid(item.GetElement("aktienID").Value.ToString());
				if(order.aktienID.Equals(aktienID))
					return true;
			}		
		}
		catch(Exception ex)
		{
			Debug.Write(ex);
		}
		return false;
	}

	/// <summary>
	/// This function is used to get a list from all current stocks from the market.
	/// </summary>
	/// <returns></returns>
	private string getStockListFromDb()
	{
		string result = string.Empty;
		List<Stock> stockList = new List<Stock>();
		
		var dbStocks = dbConnectionStocks._db;
		try
		{
			var entrys = dbStocks.Find(new BsonDocument()).ToList();
			foreach(var item in entrys)
			{
				Stock stock = new Stock();
				stock.aktienID = new Guid(item.GetElement("aktienID").Value.ToString());
				stock.name = item.GetElement("name").Value.ToString();
				stock.course = double.Parse(item.GetElement("course").Value.ToString(), System.Globalization.NumberStyles.Any);
				stock.amount = Int32.Parse(item.GetElement("amount").Value.ToString(), System.Globalization.NumberStyles.Any);
				stockList.Add(stock);
			}
			result = JsonConvert.SerializeObject(stockList);
		}
		catch(Exception ex)
		{
			Debug.WriteLine(ex);
		}
		return result;
	}

	#endregion
}

/* Valid JSON format
{
"orderID" : "1bebfc80-dbd8-4d9d-a49d-762a1949d1e7",
"aktienID" : "1bebfc80-dbd8-4d9d-a49d-762a1949d1e9",
"amount" : "10",
"limit" : "50.0",
"timestamp" : "1480348961", 
"hash" : ""
}
*/
