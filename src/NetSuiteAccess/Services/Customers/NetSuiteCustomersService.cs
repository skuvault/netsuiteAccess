using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Netco.Logging;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Models;
using NetSuiteAccess.Services.Soap;
using NetSuiteAccess.Shared;
using NetSuiteSoapWS;

namespace NetSuiteAccess.Services.Customers
{
	public class NetSuiteCustomersService : INetSuiteCustomersService
	{
		private NetSuiteSoapService _soapService;

		public NetSuiteCustomersService( NetSuiteConfig config )
		{
			this._soapService = new NetSuiteSoapService( config );
		}

		public async Task< NetSuiteCustomer > GetCustomerInfoByEmailAsync( string email, CancellationToken token )
		{
			var customer = await this._soapService.GetCustomerByEmailAsync( email, token ).ConfigureAwait( false );

			return customer?.ToSVCustomer();
		}

		public async Task< NetSuiteCustomer > GetCustomerInfoByIdAsync( string customerId, CancellationToken token )
		{
			var customers = await this.GetCustomersInfoByIdsAsync( new [] { customerId }, token ).ConfigureAwait( false );

			return customers?.FirstOrDefault();
		}

		/// <summary>
		///	Get customers by their ids
		///	Requires Lists -> Customers role permission.
		/// </summary>
		/// <param name="customerId"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task< IEnumerable< NetSuiteCustomer > > GetCustomersInfoByIdsAsync( string[] customersIds, CancellationToken token )
		{
			var customers = new List< Customer >();
			var customerIdsBatches = customersIds.SplitToPieces( NetSuiteConfig.GetCustomersByIdsPageSize );
			foreach ( var customerIdsBatch in customerIdsBatches )
			{
				customers.AddRange( await _soapService.GetCustomersByIdsAsync( customerIdsBatch, token ).ConfigureAwait( false ) );
			}

			return customers.Select( c => c.ToSVCustomer() );
		}

		public System.Threading.Tasks.Task< NetSuiteCustomer > CreateCustomerAsync( NetSuiteCustomer customer, CancellationToken token, Mark mark = null )
		{
			return this._soapService.CreateCustomerAsync( customer, token, mark );
		}
	}
}