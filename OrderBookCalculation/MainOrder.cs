using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookCalculation
{
	class MainOrder
	{
		public Order receivedOrder { get; set; }
		public BUYORSELL useCase { get; set; }
		public int statusOfOrder { get; set; }     // 0 if successfull, 1 in progress, 2 denied, 3 not enough goods, 4 wrong price

		public MainOrder(Order orderObject, BUYORSELL buy, int status = 1)
		{
			receivedOrder = orderObject;
			useCase = buy;
			statusOfOrder = status;
		}

		public MainOrder()
		{
			receivedOrder = new Order();
		}
	}

	public enum BUYORSELL { Buy, Sell }
}
