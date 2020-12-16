using Netco.Logging;
using NetSuiteAccess.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services.Customers
{
	public interface INetSuiteCustomersService
	{
		Task< IEnumerable< NetSuiteCustomer > > GetCustomersInfoByIdsAsync( string[] customersIds, CancellationToken token );
		Task< NetSuiteCustomer > GetCustomerInfoByIdAsync( string customerId, CancellationToken token );
		Task< NetSuiteCustomer > GetCustomerInfoByEmailAsync( string email, CancellationToken token );
		Task< NetSuiteCustomer > CreateCustomerAsync( NetSuiteCustomer customer, CancellationToken token, Mark mark = null );
	}
}