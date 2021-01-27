using System;
using System.Collections.Generic;

namespace NetSuiteAccess.Models
{
	public class NetSuiteSalesOrderFulfillment
	{
		/// <summary>
		///	Internal id
		/// </summary>
		public string Id { get; set; }
		/// <summary>
		///	Ref. no.
		/// </summary>
		public string TransactionId { get; set; }
		public DateTime CreatedDateUtc { get; set; }
		public DateTime LastModifiedDateUtc { get; set; }
		/// <summary>
		///	Picked, packed or shipped
		/// </summary>
		public NetSuiteSalesOrderFulfillmentStatusEnum Status { get; set; }
		public NetSuiteSalesOrderFulfillmentCarrierEnum ShipmentCarrier { get; set; }
		public string ShipmentClass { get; set; }
		public decimal ShippingCost { get; set; }
		/// <summary>
		///	Memo
		/// </summary>
		public string Note { get; set; }
		public IEnumerable< NetSuiteSalesOrderFulfillmentItem > Items { get; set; }
		public IEnumerable< NetSuiteSalesOrderFulfillmentPackage > Packages { get; set; }

		public NetSuiteSalesOrderFulfillment()
		{
			this.Items = new List< NetSuiteSalesOrderFulfillmentItem >();
			this.Packages = new List< NetSuiteSalesOrderFulfillmentPackage >();
		}
	}

	public enum NetSuiteSalesOrderFulfillmentStatusEnum { Unknown, Picked, Packed, Shipped }
	public enum NetSuiteSalesOrderFulfillmentCarrierEnum { Other, FedEx, USPS, UPS }

	public class NetSuiteSalesOrderFulfillmentItem
	{
		public string Name { get; set; }
		public int Quantity { get; set; }
	}

	public class NetSuiteSalesOrderFulfillmentPackage
	{
		public decimal WeightLbs { get; set; }
		public string TrackingNumber { get; set; }
		public string ContentsDescription { get; set; }
		public decimal LengthInches { get; set; }
		public decimal WidthInches { get; set; }
		public decimal HeightInches { get; set; }
		public decimal InsuranceCost { get; set; }
	}

	public static class SalesOrderFulfillmentExtensions
	{
		public static Dictionary< NetSuiteSoapWS.ItemFulfillmentShipStatus, NetSuiteSalesOrderFulfillmentStatusEnum > FulfillmentStatuses { get; private set; }

		static SalesOrderFulfillmentExtensions()
		{
			FulfillmentStatuses = new Dictionary< NetSuiteSoapWS.ItemFulfillmentShipStatus, NetSuiteSalesOrderFulfillmentStatusEnum >
			{
				{ NetSuiteSoapWS.ItemFulfillmentShipStatus._picked, NetSuiteSalesOrderFulfillmentStatusEnum.Picked },
				{ NetSuiteSoapWS.ItemFulfillmentShipStatus._packed, NetSuiteSalesOrderFulfillmentStatusEnum.Packed },
				{ NetSuiteSoapWS.ItemFulfillmentShipStatus._shipped, NetSuiteSalesOrderFulfillmentStatusEnum.Shipped }
			};
		}

		public static NetSuiteSalesOrderFulfillment ToSVSalesOrderFulfillment( this NetSuiteSoapWS.ItemFulfillment orderFulfillment )
		{
			var svOrderFulfillment = new NetSuiteSalesOrderFulfillment()
			{
				Id = orderFulfillment.internalId,
				TransactionId = orderFulfillment.tranId,
				CreatedDateUtc = orderFulfillment.createdDate.ToUniversalTime(),
				LastModifiedDateUtc = orderFulfillment.lastModifiedDate.ToUniversalTime(),
				Status = ToSVSalesOrderFulfillmentStatus( orderFulfillment.shipStatus ),
				ShipmentCarrier = GetSVSalesOrderFulfillmentCarrier( orderFulfillment ),
				ShipmentClass = orderFulfillment.shipMethod?.name,
				ShippingCost = (decimal)orderFulfillment.shippingCost,
				Note = orderFulfillment.memo,
				Items = orderFulfillment.itemList.ToSVSalesOrderFulfillmentItems(),
				Packages = GetSVSalesOrderFullfillmentPackages( orderFulfillment )
			};

			return svOrderFulfillment;
		}

		private static NetSuiteSalesOrderFulfillmentStatusEnum ToSVSalesOrderFulfillmentStatus( NetSuiteSoapWS.ItemFulfillmentShipStatus status )
		{
			if ( !FulfillmentStatuses.TryGetValue( status, out NetSuiteSalesOrderFulfillmentStatusEnum salesOrderFulfillmentStatus ) )
			{
				return NetSuiteSalesOrderFulfillmentStatusEnum.Unknown;
			}

			return salesOrderFulfillmentStatus;
		}

		private static IEnumerable< NetSuiteSalesOrderFulfillmentItem > ToSVSalesOrderFulfillmentItems( this NetSuiteSoapWS.ItemFulfillmentItemList itemList )
		{
			var svSalesOrderFulfillmentItems = new List< NetSuiteSalesOrderFulfillmentItem >();

			if ( itemList?.item == null || itemList.item.Length == 0 )
				return svSalesOrderFulfillmentItems;

			foreach( var fulfillmentItem in itemList.item )
			{
				svSalesOrderFulfillmentItems.Add( new NetSuiteSalesOrderFulfillmentItem()
				{
					Name = fulfillmentItem.itemName,
					Quantity = (int)fulfillmentItem.quantity
				} );
			}

			return svSalesOrderFulfillmentItems;
		}

		private static IEnumerable< NetSuiteSalesOrderFulfillmentPackage > GetSVSalesOrderFullfillmentPackages( NetSuiteSoapWS.ItemFulfillment orderFulfillment )
		{
			if ( orderFulfillment.packageFedExList != null )
				return orderFulfillment.packageFedExList.ToSVSalesOrderFulfillmentPackages();

			if ( orderFulfillment.packageUspsList != null )
				return orderFulfillment.packageUspsList.ToSVSalesOrderFulfillmentPackages();

			if ( orderFulfillment.packageUpsList != null )
				return orderFulfillment.packageUpsList.ToSVSalesOrderFulfillmentPackages();

			return orderFulfillment.packageList.ToSVSalesOrderFulfillmentPackages();
		}

		public static NetSuiteSalesOrderFulfillmentCarrierEnum GetSVSalesOrderFulfillmentCarrier( NetSuiteSoapWS.ItemFulfillment orderFulfillment )
		{
			if ( orderFulfillment.packageFedExList != null )
				return NetSuiteSalesOrderFulfillmentCarrierEnum.FedEx;

			if ( orderFulfillment.packageUspsList != null )
				return NetSuiteSalesOrderFulfillmentCarrierEnum.USPS;

			if ( orderFulfillment.packageUpsList != null )
				return NetSuiteSalesOrderFulfillmentCarrierEnum.UPS;

			return NetSuiteSalesOrderFulfillmentCarrierEnum.Other;
		}

		private static IEnumerable< NetSuiteSalesOrderFulfillmentPackage > ToSVSalesOrderFulfillmentPackages( this NetSuiteSoapWS.ItemFulfillmentPackageList packagesList )
		{
			var svSalesOrderFulfillmentPackages = new List< NetSuiteSalesOrderFulfillmentPackage >();
			if ( packagesList?.package == null || packagesList.package.Length == 0 )
				return svSalesOrderFulfillmentPackages;

			foreach( var package in packagesList.package )
			{
				svSalesOrderFulfillmentPackages.Add( new NetSuiteSalesOrderFulfillmentPackage()
				{
					TrackingNumber = package.packageTrackingNumber,
					WeightLbs = (decimal)package.packageWeight,
					ContentsDescription = package.packageDescr
				} );
			}

			return svSalesOrderFulfillmentPackages;
		}

		private static IEnumerable< NetSuiteSalesOrderFulfillmentPackage > ToSVSalesOrderFulfillmentPackages( this NetSuiteSoapWS.ItemFulfillmentPackageFedExList fedExPackagesList )
		{
			var svSalesOrderFulfillmentPackages = new List< NetSuiteSalesOrderFulfillmentPackage >();
			if ( fedExPackagesList?.packageFedEx == null || fedExPackagesList.packageFedEx.Length == 0 )
				return svSalesOrderFulfillmentPackages;

			foreach( var fedExPackage in fedExPackagesList.packageFedEx )
			{
				svSalesOrderFulfillmentPackages.Add( new NetSuiteSalesOrderFulfillmentPackage()
				{
					TrackingNumber = fedExPackage.packageTrackingNumberFedEx,
					WeightLbs = (decimal)fedExPackage.packageWeightFedEx,
					LengthInches = (decimal)fedExPackage.packageLengthFedEx,
					WidthInches = (decimal)fedExPackage.packageWidthFedEx,
					HeightInches = (decimal)fedExPackage.packageHeightFedEx,
					InsuranceCost = (decimal)fedExPackage.insuredValueFedEx
				} );
			}

			return svSalesOrderFulfillmentPackages;
		}

		private static IEnumerable< NetSuiteSalesOrderFulfillmentPackage > ToSVSalesOrderFulfillmentPackages( this NetSuiteSoapWS.ItemFulfillmentPackageUspsList uspsPackagesList )
		{
			var svSalesOrderFulfillmentPackages = new List< NetSuiteSalesOrderFulfillmentPackage >();
			if ( uspsPackagesList?.packageUsps == null || uspsPackagesList.packageUsps.Length == 0 )
				return svSalesOrderFulfillmentPackages;

			foreach( var uspsPackage in uspsPackagesList.packageUsps )
			{
				svSalesOrderFulfillmentPackages.Add( new NetSuiteSalesOrderFulfillmentPackage()
				{
					TrackingNumber = uspsPackage.packageTrackingNumberUsps,
					WeightLbs = (decimal)uspsPackage.packageWeightUsps,
					LengthInches = (decimal)uspsPackage.packageLengthUsps,
					WidthInches = (decimal)uspsPackage.packageWidthUsps,
					HeightInches = (decimal)uspsPackage.packageHeightUsps,
					ContentsDescription = uspsPackage.packageDescrUsps,
					InsuranceCost = (decimal)uspsPackage.insuredValueUsps
				} );
			}

			return svSalesOrderFulfillmentPackages;
		}

		private static IEnumerable< NetSuiteSalesOrderFulfillmentPackage > ToSVSalesOrderFulfillmentPackages( this NetSuiteSoapWS.ItemFulfillmentPackageUpsList upsPackagesList )
		{
			var svSalesOrderFulfillmentPackages = new List< NetSuiteSalesOrderFulfillmentPackage >();
			if ( upsPackagesList?.packageUps == null || upsPackagesList.packageUps.Length == 0 )
				return svSalesOrderFulfillmentPackages;

			foreach( var upsPackage in upsPackagesList.packageUps )
			{
				svSalesOrderFulfillmentPackages.Add( new NetSuiteSalesOrderFulfillmentPackage()
				{
					TrackingNumber = upsPackage.packageTrackingNumberUps,
					WeightLbs = (decimal)upsPackage.packageWeightUps,
					LengthInches = (decimal)upsPackage.packageLengthUps,
					WidthInches = (decimal)upsPackage.packageWidthUps,
					HeightInches = (decimal)upsPackage.packageHeightUps,
					ContentsDescription = upsPackage.packageDescrUps,
					InsuranceCost = (decimal)upsPackage.insuredValueUps
				} );
			}

			return svSalesOrderFulfillmentPackages;
		}
	}
}
