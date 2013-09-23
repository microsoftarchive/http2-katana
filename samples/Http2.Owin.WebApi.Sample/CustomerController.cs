using System;
using System.Web.Http;

namespace Server.WebApi
{
    public class CustomerController : ApiController
    {
        public CustomerController()
        {
        }

        // Gets
        [HttpGet]
        public Customer Get(string id)
        {
            return new Customer()
            {
                ID = Int32.Parse(id),
                LastName = "Smith",
                FirstName = "Mary",
                HouseNumber = "333",
                Street = "Main Street NE",
                City = "Redmond",
                State = "WA",
                ZipCode = "98053"
            };
        }
    }
}
