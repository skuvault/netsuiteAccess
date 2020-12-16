using NetSuiteAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services.Orders
{
	public interface INetSuiteOrdersService
	{
		Task< IEnumerable< NetSuiteSalesOrder > > GetSalesOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token, bool includeFulfillments = false );
		Task CreateSalesOrderAsync( NetSuiteSalesOrder order, string locationName, CancellationToken token, bool createCustomer = false );
		Task UpdateSalesOrderAsync( NetSuiteSalesOrder order, string locationName, CancellationToken token );
		Task< IEnumerable< NetSuitePurchaseOrder > > GetPurchaseOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token );
		Task< IEnumerable< NetSuitePurchaseOrder > > GetAllPurchaseOrdersAsync( CancellationToken token );
		Task CreatePurchaseOrderAsync( NetSuitePurchaseOrder order, string locationName, CancellationToken token );
		Task UpdatePurchaseOrderAsync( NetSuitePurchaseOrder order, CancellationToken none );
	}
}