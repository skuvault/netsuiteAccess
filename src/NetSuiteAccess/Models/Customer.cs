using Newtonsoft.Json;

namespace NetSuiteAccess.Models
{
	public class NetSuiteCustomer
	{
		[ JsonProperty( "id" ) ]
		public string Id { get; set; }
		[ JsonProperty( "companyName" ) ]
		public string CompanyName { get; set; }
		[ JsonProperty( "firstName" ) ]
		public string FirstName { get; set; }
		[ JsonProperty( "lastName" ) ]
		public string LastName { get; set; }
		[ JsonProperty( "email" ) ]
		public string Email { get; set; }
		[ JsonProperty( "phone" ) ]
		public string Phone { get; set; }
		public NetSuiteAddress Address { get; set; } 
	}

	public class NetSuiteAddress
	{
		public string Country { get; set; }
		public string Region { get; set; }
		public string City { get; set; }
		public string Address1 { get; set; }
		public string Address2 { get; set; }
		public string PostalCode { get; set; }
	}

	public static class CustomerExtensions
	{
		public static NetSuiteCustomer ToSVCustomer( this NetSuiteSoapWS.Customer customer )
		{
			return new NetSuiteCustomer()
			{
				Id = customer.internalId,
				CompanyName = customer.companyName,
				FirstName = customer.firstName,
				LastName = customer.lastName,
				Email = customer.email,
				Phone = customer.phone
			};
		}
	}
}