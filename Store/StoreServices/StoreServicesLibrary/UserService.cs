using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using DTOLibraries.UserDTO;

namespace StoreServicesLibrary
{
	// NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "UserService" in both code and config file together.
	public class UserService : IUserService
	{
		public bool Register(User user)
		{
			return true;
		}

		public bool SignIn(User user)
		{
			return true;
		}
	}
}
