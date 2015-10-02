using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBLayoutLibrary.DTO
{
	public class User
	{
		public string Name { get; set; }
		public string Email { get; set; }
		public Guid Id { get; set; }
	}
}
