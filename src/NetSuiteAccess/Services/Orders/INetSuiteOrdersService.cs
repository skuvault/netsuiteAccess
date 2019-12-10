﻿using NetSuiteAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services.Orders
{
	public interface INetSuiteOrdersService
	{
		Task< IEnumerable< NetSuiteSalesOrder > > GetSalesOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token );
		Task< IEnumerable< NetSuitePurchaseOrder > > GetPurchaseOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token );
	}
}