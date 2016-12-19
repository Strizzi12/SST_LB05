using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boerse
{
	class Stock
	{
		public Guid aktienID { get; set; }
		public string name { get; set; }
		public double course { get; set; }
		public int amount { get; set; }
	}
}
