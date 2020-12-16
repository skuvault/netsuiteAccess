using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Netco.Logging;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Exceptions;
using NetSuiteAccess.Models;
using NetSuiteAccess.Services.Common;
using NetSuiteAccess.Services.Customers;
using NetSuiteAccess.Services.Soap;
using NetSuiteAccess.Shared.Logging;

namespace NetSuiteAccess.Services.Orders
{
	public sealed class NetSuiteOrdersService : INetSuiteOrdersService
	{
		private INetSuiteCustomersService _customersService;
		private INetSuiteCommonService _commonService;
		private NetSuiteSoapService _soapService;

		public NetSuiteOrdersService( NetSuiteConfig config )
		{
			this._customersService = new NetSuiteCustomersService( config );
			this._commonService = new NetSuiteCommonService( config );
			this._soapService = new NetSuiteSoapService( config );
		}

		/// <summary>
		///	Create purchase order.
		///	Requires Transactions -> Purchase Order role permission. Level - Create or Full.
		/// </summary>
		/// <param name="order"></param>
		/// <param name="locationName"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task CreatePurchaseOrderAsync( NetSuitePurchaseOrder order, string locationName, CancellationToken token )
		{
			var location = await this.GetLocationByNameAsync( locationName, token ).ConfigureAwait( false );

			if ( location == null )
				throw new NetSuiteException( string.Format( "Location with name {0} is not found in NetSuite!", locationName ) );

			await this._soapService.CreatePurchaseOrderAsync( order, location.Id, token ).ConfigureAwait( false );
		}

		/// <summary>
		///	Update purchase order.
		///	Requires Transactions -> Purchase Order role permission. Level - Edit or Full.
		/// </summary>
		/// <param name="order"></param>
		/// <param name="none"></param>
		/// <returns></returns>
		public Task UpdatePurchaseOrderAsync( NetSuitePurchaseOrder order, CancellationToken token )
		{
			return this._soapService.UpdatePurchaseOrderAsync( order, token );
		}

		/// <summary>
		///	Get all purchase orders
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public Task< IEnumerable< NetSuitePurchaseOrder > > GetAllPurchaseOrdersAsync( CancellationToken token )
		{
			return this._soapService.GetAllPurchaseOrdersAsync( token );
		}

		/// <summary>
		///	Lists purchase orders that were changed
		/// </summary>
		/// <param name="startDateUtc"></param>
		/// <param name="endDateUtc"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public Task< IEnumerable< NetSuitePurchaseOrder > > GetPurchaseOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token )
		{
			return this._soapService.GetModifiedPurchaseOrdersAsync( startDateUtc, endDateUtc, token );
		}

		/// <summary>
		///  Lists sales orders that were changed.
		///  Requires Transactions -> Sales Order permission.
		/// </summary>
		/// <param name="startDateUtc"></param>
		/// <param name="endDateUtc"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task< IEnumerable< NetSuiteSalesOrder > > GetSalesOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token, bool includeFulfillments = false )
		{
			var modifiedOrders = ( await _soapService.GetModifiedSalesOrdersAsync( startDateUtc, endDateUtc, token, includeFulfillments ).ConfigureAwait( false ) ).ToArray();
			var customers = ( await this._customersService.GetCustomersInfoByIdsAsync( modifiedOrders.Select( c => c.Customer.Id.ToString() ).Distinct().ToArray(), token ).ConfigureAwait( false ) ).ToList();
			foreach( var order in modifiedOrders )
			{
				order.Customer = customers.FirstOrDefault( c => c.Id == order.Customer.Id );
			}

			return modifiedOrders.ToArray();
		}

		/// <summary>
		///	Creates sales order in NetSuite
		/// </summary>
		/// <param name="order">Sales order</param>
		/// <param name="token">Cancellation token</param>
		/// <returns></returns>
		public async Task CreateSalesOrderAsync( NetSuiteSalesOrder order, string locationName, CancellationToken token, bool createCustomer = false )
		{
			var mark = Mark.CreateNew();
			var location = await this.GetLocationByNameAsync( locationName, token ).ConfigureAwait( false );

			if ( location == null )
			{
				throw new NetSuiteException( string.Format( "Location with name {0} is not found in NetSuite!", locationName ) );
			}

			var customer = await this._customersService.GetCustomerInfoByEmailAsync( order.Customer.Email, token ).ConfigureAwait( false );

			if ( customer == null )
			{
				if ( !createCustomer )
				{
					NetSuiteLogger.LogTrace( string.Format( "Can't create sales order in NetSuite! Customer with email {0} was not found!", order.Customer.Email ) );
					return;
				}

				if ( string.IsNullOrWhiteSpace( order.Customer.FirstName )
					|| string.IsNullOrWhiteSpace( order.Customer.LastName ) )
				{
					NetSuiteLogger.LogTrace( "Can't create sales order in NetSuite! Customer's first name or last name aren't specified!" );
					return;
				}

				customer = await this._soapService.CreateCustomerAsync( order.Customer, token, mark ).ConfigureAwait( false );
			}

			await this._soapService.CreateSalesOrderAsync( order, location.Id, customer.Id, token, mark ).ConfigureAwait( false );
		}

		/// <summary>
		///	Updates existing sales order in NetSuite
		/// </summary>
		/// <param name="order">Sales order</param>
		/// <param name="">Cancellation token</param>
		/// <returns></returns>
		public async Task UpdateSalesOrderAsync( NetSuiteSalesOrder order, string locationName, CancellationToken token )
		{
			var location = await this.GetLocationByNameAsync( locationName, token ).ConfigureAwait( false );

			if ( location == null )
			{
				throw new NetSuiteException( string.Format( "Location with name {0} is not found in NetSuite!", locationName ) );
			}

			var customer = await this._customersService.GetCustomerInfoByEmailAsync( order.Customer.Email, token ).ConfigureAwait( false );

			if ( customer == null )
			{
				NetSuiteLogger.LogTrace( string.Format( "Can't update sales order in NetSuite! Customer with email {0} was not found!", order.Customer.Email ) );
				return;
			}

			await this._soapService.UpdateSalesOrderAsync( order, location.Id, customer.Id, token ).ConfigureAwait( false );
		}

		private async Task< NetSuiteLocation > GetLocationByNameAsync( string locationName, CancellationToken token )
		{
			var locations = await this._commonService.GetLocationsAsync( token ).ConfigureAwait( false );
			return locations.FirstOrDefault( l => l.Name.ToLower().Equals( locationName.ToLower() ) );
		}
	}
}