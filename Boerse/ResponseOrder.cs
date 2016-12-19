using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boerse
{
	class ResponseOrder
	{
		public Guid orderID { get; set; }
		public double price { get; set; }
		public int amount { get; set; }
		public int status { get; set; }     // 0 if successfull, 1 in progress, 2 denied, 3 not enough goods, 4 wrong price
		public string hash { get; }
	}
}
