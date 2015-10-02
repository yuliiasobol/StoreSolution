using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace DTOLibraries.UserDTO
{
	[KnownType(typeof(RegisteredUser))]//указываем что RegisteredUser тоже нужно сериализовать 
	[DataContract]
	public class User
	{
		[DataMember(Order = 2)] //по умолчанию идет по алфавиту
		public string Name { get; set; }
		public string Email { get; set; }
		public Guid Id { get; set; }
	}
}
