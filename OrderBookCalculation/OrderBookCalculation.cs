using Boerse;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrderBookCalculation
{
	class OrderBookCalculation
	{
		static void Main(string[] args)
		{
			//New Thread needs to be started, which calculates the market prizes and handles the orders.
			Thread myThread = new Thread(new ThreadStart(orderBookOperations));
			myThread.Start();
		}

		static void orderBookOperations()
		{
			while(true)
			{
				int minutes = 1;
				Thread.Sleep(1000 * 60 * minutes);
				//Do work here
				Console.WriteLine("Starting to process the orders!");
				//order the limit
				//the limit with most verkäufe to given käufe is taken
				//the one with the lower timestamp gets their full amount, rest is depending on next timestamp and so on.
				try
				{
					//The different MongoDB Tables and their connection.
					var dbStocks = dbConnectionStocks._db;
					var stockEntrys = dbStocks.Find(new BsonDocument()).ToList();

					var dbOrders = dbConnectionOrders._db;
					var OrderEntrys = dbOrders.Find(new BsonDocument()).ToList();

					var dbAktienverlauf = dbConnectionAktienverlauf._db;

					//Do work for each aktienID
					foreach(var stock in stockEntrys)
					{
						Guid aktienID = new Guid(stock.GetElement("aktienID").Value.ToString());
						//List for all orders with the current aktienID
						List<MainOrder> orderList = new List<MainOrder>();
						foreach(var order in OrderEntrys)
						{
							//Check if AktienID is equivalent to current AktienID which is processed.
							Guid deserializedAktienID = new Guid(order.GetElement("aktienID").Value.ToString());
							if(!aktienID.Equals(deserializedAktienID))
								continue;

							//Get all entrys in Order-Table and check if Order is "in progress"
							int deserializedStatusOfOrder = Int32.Parse(order.GetElement("statusOfOrder").Value.ToString(), System.Globalization.NumberStyles.Any);
							if(deserializedStatusOfOrder != 1)
								continue;

							//Getting the order from the database
							Guid deserializedOrderID = new Guid(order.GetElement("orderID").Value.ToString());
							int deserializedAmount = Int32.Parse(order.GetElement("amount").Value.ToString(), System.Globalization.NumberStyles.Any);
							double deserializedLimit = Double.Parse(order.GetElement("limit").Value.ToString(), System.Globalization.NumberStyles.Any);
							int deserializedTimestamp = Int32.Parse(order.GetElement("timestamp").Value.ToString(), System.Globalization.NumberStyles.Any);
							int deserializedUseCase = Int32.Parse(order.GetElement("useCase").Value.ToString(), System.Globalization.NumberStyles.Any);

							//Store them in an object for further processing
							MainOrder mainOrder = new MainOrder();
							mainOrder.receivedOrder.orderID = deserializedOrderID;
							mainOrder.receivedOrder.aktienID = deserializedAktienID;
							mainOrder.receivedOrder.amount = deserializedAmount;
							mainOrder.receivedOrder.limit = deserializedLimit;
							mainOrder.receivedOrder.timestamp = deserializedTimestamp;
							mainOrder.useCase = deserializedUseCase == 0 ? BUYORSELL.Buy : BUYORSELL.Sell;
							mainOrder.statusOfOrder = deserializedStatusOfOrder;

							orderList.Add(mainOrder);
						}
						//If the orderlist is empty, nothing should be done!
						if(orderList.Count == 0)
							continue;

						//Look at each order, differentiate each limit (if buy or sell) an add up their amount from the different order.
						List<KeyVal<double, int, int>> limitList = new List<KeyVal<double, int, int>>();
						bool found = false;
						foreach(MainOrder order in orderList)
						{
							double limit = order.receivedOrder.limit;
							int amount = order.receivedOrder.amount;
							foreach(KeyVal<double, int, int> item in limitList)
							{
								//Buy
								if(order.useCase == 0)
								{
									if(item.limit == limit)
									{
										item.amountBuy += order.receivedOrder.amount;
										found = true;
										break;
									}
								}
								//Sell
								else
								{
									if(item.limit == limit)
									{
										item.amountSell += order.receivedOrder.amount;
										found = true;
										break;
									}
								}
							}
							if(!found)
							{
								//Buy
								if(order.useCase == 0)
								{
									KeyVal<double, int, int> newKeyVal = new KeyVal<double, int, int>(limit, order.receivedOrder.amount, 0);
									limitList.Add(newKeyVal);
								}
								//Sell
								else
								{
									KeyVal<double, int, int> newKeyVal = new KeyVal<double, int, int>(limit, 0, order.receivedOrder.amount);
									limitList.Add(newKeyVal);
								}
							}
							found = false;
						}
						found = false;

						//The market also wants to sell their remaining stocks so this also needs to be taken into account
						int amountInBoerse = Int32.Parse(stock.GetElement("amount").Value.ToString(), System.Globalization.NumberStyles.Any);
						double courseInBoerse = Double.Parse(stock.GetElement("course").Value.ToString(), System.Globalization.NumberStyles.Any);
						foreach(KeyVal<double, int, int> item in limitList)
						{
							if(item.limit == courseInBoerse)
							{
								item.amountSell += amountInBoerse;
								found = true;
							}
						}
						if(!found)
						{
							KeyVal<double, int, int> newKeyVal = new KeyVal<double, int, int>(courseInBoerse, 0, amountInBoerse);
							limitList.Add(newKeyVal);
						}

						//Now compare the best amount of buy/sell
						List<KeyVal<double, int, int>> sortedLimitList = limitList.OrderByDescending(o => o.limit).ToList();

						//For Memory purposes^^
						//limitList.Clear();

						//Hier sollen alle Buy und Sell Order von unten bzw. von oben aufgelistet werden.!
						List<KeyVal<double, int, int>> tempList = new List<KeyVal<double, int, int>>();
						int count = 1;
						foreach(var item in sortedLimitList)
						{
							KeyVal<double, int, int> tempKeyVal = new KeyVal<double, int, int>(0, 0, 0);
							for(int i = 0; i < count; i++)  //The last one should add himself up
							{
								tempKeyVal.amountBuy += sortedLimitList[i].amountBuy;
							}
							tempKeyVal.limit = item.limit;
							tempList.Add(tempKeyVal);
							count++;
						}

						int maxCount = sortedLimitList.Count;
						count = 0;
						bool limitFound = false;
						foreach(var item in sortedLimitList)
						{
							KeyVal<double, int, int> tempKeyVal = new KeyVal<double, int, int>(0, 0, 0);
							for(int i = count; i < maxCount; i++)
							{
								tempKeyVal.amountSell += sortedLimitList[i].amountSell;
							}
							foreach(var temp in tempList)
							{
								if(item.limit == temp.limit)
								{
									temp.amountSell = tempKeyVal.amountSell;
									limitFound = true;
									break;
								}
							}
							if(!limitFound)
							{
								tempKeyVal.limit = item.limit;
								tempList.Add(tempKeyVal);
							}
							count++;
						}

						sortedLimitList = tempList;

						//Kursfeststellung - here are buy order smaller than sell orders
						KeyVal<double, int, int> best = new KeyVal<double, int, int>(0, 0, 0);
						int bestMin = 0;
						foreach(var item in sortedLimitList)
						{
							//Feststellung hier muss amount buy größer als die von best sein
							int min = Math.Min(item.amountBuy, item.amountSell);
							if(min > bestMin)
							{
								bestMin = min;
								best = item;
							}

							/*
							//Feststellung hier muss amount buy größer als die von best sein
							if(item.amountSell >= item.amountBuy && best.amountBuy <= item.amountBuy && best.limit <= item.limit)
							{
								//if(item.amountBuy != 0 && item.amountSell > best.amountSell)
									best = item;
							}*/
						}
						int bestMax = 0;
						if(bestMin == 0)
						{
							foreach(var item in sortedLimitList)
							{
								//Feststellung hier muss amount buy größer als die von best sein
								int max = Math.Max(item.amountBuy, item.amountSell);
								if(max > bestMax)
								{
									bestMax = max;
									best = item;
								}
							}
						}

						//For Memory purposes^^
						//sortedLimitListBuy.Clear();

						//Setting the price for the specific aktienID
						//Get the _id of the MongoDB document
						//var _id = stock.GetElement("_id").Value.ToString();
						var filter = Builders<BsonDocument>.Filter.Eq("aktienID", aktienID.ToString());
						var update = Builders<BsonDocument>.Update.Set("course", best.limit);
						var result = dbStocks.UpdateOne(filter, update);

						//Check all the orders and update them
						//Sort them per timestamp because -> First Come, First Serve
						List<MainOrder> timestampSortedOrderList = new List<MainOrder>();
						timestampSortedOrderList = orderList.OrderBy(o => o.receivedOrder.timestamp).ToList();
						int differenceBetweenBuyAndSell = best.amountSell - best.amountBuy;
						foreach(var sortedOrder in timestampSortedOrderList)
						{
							var quantity = sortedOrder.receivedOrder.amount;
							//Buy
							if(sortedOrder.useCase.Equals(BUYORSELL.Buy) && sortedOrder.receivedOrder.limit >= best.limit)
							{
								if(best.amountSell > 0)
								{
									//Order bearbeiten
									if(quantity <= best.amountSell)      //All requested goods can be bought                  
									{
										sortedOrder.statusOfOrder = 0;  //Successfull
										best.amountSell -= quantity;     //Remaining amount calculation
									}
									else//Not all requested goods can be bought
									{
										sortedOrder.statusOfOrder = 3;  //Not enough goods
										sortedOrder.receivedOrder.amount = best.amountSell;
										best.amountSell = 0;
									}
								}
								else
								{
									sortedOrder.statusOfOrder = 2;  //Denied, because no goods are left to buy
									sortedOrder.receivedOrder.amount = 0;
								}
							}
							//Sell
							else if(sortedOrder.useCase.Equals(BUYORSELL.Sell) && sortedOrder.receivedOrder.limit <= best.limit)
							{
								sortedOrder.statusOfOrder = 0;      //Successfull	
																	/*
																	if(quantity <= best.amountSell)
																	{
																		//Order bearbeiten
																		//sortedOrder.statusOfOrder = 0;      //Successfull							
																		//best.amountSell -= quantity;        //Remaining amount calculation
																	}
																	else
																	{   //Hier dürfte er beim verkaufen theoretisch nie reinkommen.
																		sortedOrder.statusOfOrder = 3;      //Not enough goods
																		sortedOrder.receivedOrder.amount = 0;
																	}*/
							}
							else if((sortedOrder.useCase.Equals(BUYORSELL.Buy) && sortedOrder.receivedOrder.limit <= best.limit)
								|| (sortedOrder.useCase.Equals(BUYORSELL.Sell) && sortedOrder.receivedOrder.limit >= best.limit))
							{
								sortedOrder.statusOfOrder = 4;          //Wrong price
								sortedOrder.receivedOrder.amount = 0;   //Nothings was sold
							}
							else
							{
								sortedOrder.statusOfOrder = 2;      //Denied
							}
						}

						//Update the remaining amount in the stock database
						update = Builders<BsonDocument>.Update.Set("amount", best.amountSell);
						result = dbStocks.UpdateOne(filter, update);

						//Update the orders in the database with the updated sorted orderList
						foreach(var order in OrderEntrys)
						{
							Guid orderID = new Guid(order.GetElement("orderID").Value.ToString());
							foreach(var item in timestampSortedOrderList)
							{
								Guid itemOrderID = item.receivedOrder.orderID;
								if(itemOrderID.Equals(orderID))
								{
									//var order_id = order.GetElement("_id").Value.ToString();
									var orderFilter = Builders<BsonDocument>.Filter.Eq("orderID", item.receivedOrder.orderID.ToString());
									var orderUpdate = Builders<BsonDocument>.Update.Set("amount", item.receivedOrder.amount).Set("statusOfOrder", item.statusOfOrder).Set("limit", best.limit);
									var orderResult = dbOrders.UpdateOne(orderFilter, orderUpdate);
									break;
								}
							}
						}

						//Insert new value of the current aktienID into database, where the course balancing is stored.
						var aktie = new BsonDocument()
						{
							{"aktienID", aktienID},
							{"course", best.limit},
							{"timestamp", ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString()}
						};
						dbAktienverlauf.InsertOne(aktie);
					}
					Console.WriteLine("Finished to process the orders!");
				}
				catch(Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}
		}

	}
}
