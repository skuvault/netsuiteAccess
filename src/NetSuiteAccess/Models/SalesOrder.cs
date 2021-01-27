using System;
using System.Collections.Generic;

namespace NetSuiteAccess.Models
{
	public class NetSuiteSalesOrder : NetSuiteOrder
	{
		public NetSuiteSalesOrderSource Source { get; set; }
		public NetSuiteSalesOrderStatus Status { get; set; }
		public NetSuiteCustomer Customer { get; set; }
		public NetSuiteSalesOrderItem[] Items { get; set; }
		public decimal DiscountTotal { get; set; }
		public decimal TaxTotal { get; set; }
		public string DiscountName { get; set; }
		public NetSuiteDiscountTypeEnum DiscountType { get; set; }
		public IEnumerable< NetSuiteSalesOrderFulfillment > Fulfillments { get; set; }

		public NetSuiteSalesOrder()
		{
			this.Fulfillments = new List< NetSuiteSalesOrderFulfillment >();
		}
	}

	public class NetSuiteSalesOrderItem
	{
		public string Sku { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal TaxAmount { get; set; }	//Always returns 0, even if the item is taxable, has a tax code in the order and the order itself shows tax
		public decimal TaxRate { get; set; }
	}

	public enum NetSuiteSalesOrderStatus
	{
		Unknown,
		PendingApproval,
		PendingBilling,
		PendingBillingPartFulfilled,
		PartiallyFulfilled,
		Billed,
		PendingFulfillment,
		Cancelled,
		Closed
	}

	public enum NetSuiteSalesOrderSource
	{
		NetSuite,
		External
	}

	public static class OrderExtensions
	{
		public static Dictionary< string, NetSuiteSalesOrderStatus > SalesOrderStatuses { get; private set; }

		static OrderExtensions()
		{
			SalesOrderStatuses = new Dictionary< string, NetSuiteSalesOrderStatus >
			{
				{ "Pending Approval", NetSuiteSalesOrderStatus.PendingApproval },
				{ "Pending Billing", NetSuiteSalesOrderStatus.PendingBilling },
				{ "Pending BillingPart Fulfilled", NetSuiteSalesOrderStatus.PendingBillingPartFulfilled },
				{ "Partially Fulfilled", NetSuiteSalesOrderStatus.PartiallyFulfilled },
				{ "Billed", NetSuiteSalesOrderStatus.Billed },
				{ "Pending Fulfillment", NetSuiteSalesOrderStatus.PendingFulfillment },
				{ "Cancelled", NetSuiteSalesOrderStatus.Cancelled },
				{ "Closed", NetSuiteSalesOrderStatus.Closed }
			};
		}

		public static NetSuiteSalesOrder ToSVSalesOrder( this NetSuiteSoapWS.SalesOrder order )
		{
			var svOrder = new NetSuiteSalesOrder
			{
				Id = order.internalId,
				DocNumber = order.tranId,
				CreatedDateUtc = order.createdDate.ToUniversalTime(),
				ModifiedDateUtc = order.lastModifiedDate.ToUniversalTime(),
				Status = GetSalesOrderStatus( order.status ),
				Total = (decimal)order.total,
				DiscountName = order.discountItem?.name,
				DiscountTotal = ( decimal )order.discountTotal,
				DiscountType = order.discountRate.ToDiscountType(),
				TaxTotal = ( decimal )order.taxTotal
			};

			if ( !string.IsNullOrWhiteSpace( order.source ) 
					&& order.source.Equals( "Web Services" ) )
			{
				svOrder.Source = NetSuiteSalesOrderSource.External;
			}

			svOrder.ShippingInfo = new NetSuiteShippingInfo()
			{
				Cost = (decimal)order.shippingCost
			};

			if ( order.shippingAddress != null )
			{
				svOrder.ShippingInfo.Address = new NetSuiteShippingAddress()
				{
					Line1 = order.shippingAddress.addr1,
					Line2 = order.shippingAddress.addr2,
					City = order.shippingAddress.city,
					PostalCode = order.shippingAddress.zip,
					CountryCode = order.shippingAddress.country.ToString(),
					State = order.shippingAddress.state
				};
			}

			if ( order.shipMethod != null )
			{
				svOrder.ShippingInfo.Carrier = order.shipMethod.name;
			}

			var items = new List< NetSuiteSalesOrderItem >();
			if ( order.itemList != null )
			{
				foreach( var itemInfo in order.itemList.item )
				{
					items.Add( new NetSuiteSalesOrderItem
					{
						Quantity = (int)Math.Floor( itemInfo.quantity ),
						Sku = itemInfo.item != null ? itemInfo.item.name : string.Empty,
						UnitPrice = GetOrderLineItemUnitPrice( itemInfo ),
						TaxRate = (decimal)itemInfo.taxRate1,
						TaxAmount = (decimal)itemInfo.taxAmount
					} );
				}
			}
			svOrder.Items = items.ToArray();

			svOrder.Customer = new NetSuiteCustomer()
			{
				Id = int.Parse( order.entity.internalId )
			};

			return svOrder;
		}

		public static NetSuiteDiscountTypeEnum ToDiscountType( this string discountRate )
		{
			if ( string.IsNullOrWhiteSpace( discountRate ) )
				return NetSuiteDiscountTypeEnum.Undefined;
			return discountRate.Contains( "%" ) ? NetSuiteDiscountTypeEnum.Percentage : NetSuiteDiscountTypeEnum.FixedAmount;
		}

		private static decimal GetOrderLineItemUnitPrice( NetSuiteSoapWS.SalesOrderItem saleOrderItem )
		{
			decimal unitPrice = 0;

			if ( saleOrderItem.amountSpecified && saleOrderItem.quantity > 0 )
			{
				unitPrice = (decimal)Math.Round( saleOrderItem.amount / saleOrderItem.quantity, 2 );
			}

			return unitPrice;
		}

		private static NetSuiteSalesOrderStatus GetSalesOrderStatus( string status )
		{
			if ( string.IsNullOrWhiteSpace( status ) )
				return NetSuiteSalesOrderStatus.Unknown;

			if ( !SalesOrderStatuses.TryGetValue( status, out NetSuiteSalesOrderStatus salesOrderStatus ) )
			{
				return NetSuiteSalesOrderStatus.Unknown;
			}

			return salesOrderStatus;
		}
	}
}