using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boerse
{
	class Order
	{
		public Guid orderID { get; set; }
		public Guid aktienID { get; set; }
		public int amount { get; set; }
		public double limit { get; set; }
		public int timestamp { get; set; }
		public string hash { get; set; }
	}
}
