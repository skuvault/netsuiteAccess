using FluentAssertions;
using NetSuiteAccess.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class OrderFulfillmentMapperTests
	{
		[ Test ]
		public void WhenToSVSalesOrderFulfillment_ThenFieldsAreMapped()
		{
			var nsOrderFulfillment = new NetSuiteSoapWS.ItemFulfillment()
			{
				internalId = "1",
				tranId = "100",
				createdDate = DateTime.UtcNow.AddDays( -1 ),
				lastModifiedDate = DateTime.UtcNow,
				shipStatus = NetSuiteSoapWS.ItemFulfillmentShipStatus._shipped,
				shipMethod = new NetSuiteSoapWS.RecordRef()
				{
					name = "UPS 2nd Day Air"
				},
				shippingCost = 1.24,
				memo = "Some note here",
				itemList = new NetSuiteSoapWS.ItemFulfillmentItemList()
				{
					item = new NetSuiteSoapWS.ItemFulfillmentItem[]
					{
						new NetSuiteSoapWS.ItemFulfillmentItem()
						{
							itemName = "NS-testsku1",
							quantity = 2
						},
						new NetSuiteSoapWS.ItemFulfillmentItem()
						{
							itemName = "NS-testsku2",
							quantity = 4
						}
					}
				},
				packageUpsList = new NetSuiteSoapWS.ItemFulfillmentPackageUpsList()
				{
					packageUps = new NetSuiteSoapWS.ItemFulfillmentPackageUps[]
					{
						new NetSuiteSoapWS.ItemFulfillmentPackageUps()
						{
							packageTrackingNumberUps = "123456789ABCD",
							packageWeightUps = 4.12,
							packageDescrUps = "Fragile content",
							packageLengthUps = 2,
							packageHeightUps = 2,
							packageWidthUps = 1,
							insuredValueUps = 1.5
						}
					}
				}
			};

			var svSalesOrderFulfillment = nsOrderFulfillment.ToSVSalesOrderFulfillment();

			svSalesOrderFulfillment.Id.Should().Be( nsOrderFulfillment.internalId );
			svSalesOrderFulfillment.TransactionId.Should().Be( nsOrderFulfillment.tranId );
			svSalesOrderFulfillment.CreatedDateUtc.Should().Be( nsOrderFulfillment.createdDate.ToUniversalTime() );
			svSalesOrderFulfillment.LastModifiedDateUtc.Should().Be( nsOrderFulfillment.lastModifiedDate.ToUniversalTime() );
			svSalesOrderFulfillment.Note.Should().Be( nsOrderFulfillment.memo );
			svSalesOrderFulfillment.Status.Should().Be( NetSuiteSalesOrderFulfillmentStatusEnum.Shipped );
			svSalesOrderFulfillment.ShipmentCarrier.Should().Be( NetSuiteSalesOrderFulfillmentCarrierEnum.UPS );
			svSalesOrderFulfillment.ShipmentClass.Should().Be( nsOrderFulfillment.shipMethod.name );
			svSalesOrderFulfillment.ShippingCost.Should().Be( (decimal)nsOrderFulfillment.shippingCost );

			svSalesOrderFulfillment.Items.Count().Should().Be( nsOrderFulfillment.itemList.item.Count() );
			svSalesOrderFulfillment.Items.First().Name.Should().Be( nsOrderFulfillment.itemList.item.First().itemName );
			svSalesOrderFulfillment.Items.First().Quantity.Should().Be( (int)nsOrderFulfillment.itemList.item.First().quantity );
			svSalesOrderFulfillment.Items.Last().Name.Should().Be( nsOrderFulfillment.itemList.item.Last().itemName );
			svSalesOrderFulfillment.Items.Last().Quantity.Should().Be( (int)nsOrderFulfillment.itemList.item.Last().quantity );

			svSalesOrderFulfillment.Packages.Count().Should().Be( nsOrderFulfillment.packageUpsList.packageUps.Count() );
			var svSalesOrderFulfillmentPackage = svSalesOrderFulfillment.Packages.First();
			var nsOrderFulfillmentPackage = nsOrderFulfillment.packageUpsList.packageUps.First();
			svSalesOrderFulfillmentPackage.TrackingNumber.Should().Be( nsOrderFulfillmentPackage.packageTrackingNumberUps );
			svSalesOrderFulfillmentPackage.WeightLbs.Should().Be( (decimal)nsOrderFulfillmentPackage.packageWeightUps );
			svSalesOrderFulfillmentPackage.WidthInches.Should().Be( nsOrderFulfillmentPackage.packageWidthUps );
			svSalesOrderFulfillmentPackage.LengthInches.Should().Be( nsOrderFulfillmentPackage.packageLengthUps );
			svSalesOrderFulfillmentPackage.HeightInches.Should().Be( nsOrderFulfillmentPackage.packageHeightUps );
			svSalesOrderFulfillmentPackage.InsuranceCost.Should().Be( (decimal)nsOrderFulfillmentPackage.insuredValueUps );
		}

		[ Test ]
		public void GivenNSOrderFulfillmentWithStatusPicked_WhenToSVSalesOrderFulfillmentIsCalled_ThenStatusIsMappedCorrectly()
		{
			var nsOrderFulfillment = GetNsOrder();
			nsOrderFulfillment.shipStatus = NetSuiteSoapWS.ItemFulfillmentShipStatus._picked;

			var svOrderFulfillment = nsOrderFulfillment.ToSVSalesOrderFulfillment();

			svOrderFulfillment.Status.Should().Be( NetSuiteSalesOrderFulfillmentStatusEnum.Picked );
		}

		[ Test ]
		public void GivenNsOrderFulfillmentWithStatusPacked_WhenToSVSalesOrderFulfillmentIsCalled_ThenStatusIsMappedCorrectly()
		{
			var nsOrderFulfillment = GetNsOrder();
			nsOrderFulfillment.shipStatus = NetSuiteSoapWS.ItemFulfillmentShipStatus._packed;

			var svOrderFulfillment = nsOrderFulfillment.ToSVSalesOrderFulfillment();

			svOrderFulfillment.Status.Should().Be( NetSuiteSalesOrderFulfillmentStatusEnum.Packed );
		}

		[ Test ]
		public void GivenNsOrderFulfillmentWithStatusShipped_WhenToSVSalesOrderFulfillmentIsCalled_ThenStatusIsMappedCorrectly()
		{
			var nsOrderFulfillment = GetNsOrder();
			nsOrderFulfillment.shipStatus = NetSuiteSoapWS.ItemFulfillmentShipStatus._shipped;

			var svOrderFulfillment = nsOrderFulfillment.ToSVSalesOrderFulfillment();

			svOrderFulfillment.Status.Should().Be( NetSuiteSalesOrderFulfillmentStatusEnum.Shipped );
		}

		[ Test ]
		public void GivenNsOrderFulfillmentWithUnknownCarrier_WhenToSVSalesOrderFulfillmentIsCalled_ThenOtherShipmentCarrierWithPackagesIsReturned()
		{
			var nsOrderFulfillment = GetNsOrder();
			nsOrderFulfillment.packageList = new NetSuiteSoapWS.ItemFulfillmentPackageList()
			{
				package = new NetSuiteSoapWS.ItemFulfillmentPackage[]
				{
					new NetSuiteSoapWS.ItemFulfillmentPackage()
					{
						packageTrackingNumber = "ABCD12345",
						packageWeight = 5.2,
						packageDescr = "Fragile content"
					}
				}
			};

			var svOrderFulfillment = nsOrderFulfillment.ToSVSalesOrderFulfillment();

			svOrderFulfillment.ShipmentCarrier.Should().Be( NetSuiteSalesOrderFulfillmentCarrierEnum.Other );
			svOrderFulfillment.Packages.First().TrackingNumber.Should().Be( nsOrderFulfillment.packageList.package.First().packageTrackingNumber );
			svOrderFulfillment.Packages.First().WeightLbs.Should().Be( (decimal)nsOrderFulfillment.packageList.package.First().packageWeight );
			svOrderFulfillment.Packages.First().ContentsDescription.Should().Be( nsOrderFulfillment.packageList.package.First().packageDescr );
		}

		[ Test ]
		public void GivenNsOrderFulfillmentWithFedExCarrier_WhenToSVSalesOrderFulfillmentIsCalled_ThenFedExShipmentCarrierWithPackagesIsReturned()
		{
			var nsOrderFulfillment = GetNsOrder();
			nsOrderFulfillment.packageFedExList = new NetSuiteSoapWS.ItemFulfillmentPackageFedExList()
			{
				packageFedEx = new NetSuiteSoapWS.ItemFulfillmentPackageFedEx[]
				{
					new NetSuiteSoapWS.ItemFulfillmentPackageFedEx()
					{
						packageTrackingNumberFedEx = "XYZ12345",
						packageWeightFedEx = 3.72,
						packageLengthFedEx = 1,
						packageWidthFedEx = 1,
						packageHeightFedEx = 2,
						insuredValueFedEx = 2.4
					}
				}
			};

			var svOrderFulfillment = nsOrderFulfillment.ToSVSalesOrderFulfillment();
			svOrderFulfillment.ShipmentCarrier.Should().Be( NetSuiteSalesOrderFulfillmentCarrierEnum.FedEx );
			svOrderFulfillment.Packages.First().TrackingNumber.Should().Be( nsOrderFulfillment.packageFedExList.packageFedEx.First().packageTrackingNumberFedEx );
			svOrderFulfillment.Packages.First().WeightLbs.Should().Be( (decimal)nsOrderFulfillment.packageFedExList.packageFedEx.First().packageWeightFedEx );
			svOrderFulfillment.Packages.First().LengthInches.Should().Be( nsOrderFulfillment.packageFedExList.packageFedEx.First().packageLengthFedEx );
			svOrderFulfillment.Packages.First().WidthInches.Should().Be( nsOrderFulfillment.packageFedExList.packageFedEx.First().packageWidthFedEx );
			svOrderFulfillment.Packages.First().HeightInches.Should().Be( nsOrderFulfillment.packageFedExList.packageFedEx.First().packageHeightFedEx );
			svOrderFulfillment.Packages.First().InsuranceCost.Should().Be( (decimal)nsOrderFulfillment.packageFedExList.packageFedEx.First().insuredValueFedEx );
		}

		[ Test ]
		public void GivenNsOrderFulfillmentWithUSPSCarrier_WhenToSVSalesOrderFulfillmentIsCalled_ThenUSPSShipmentCarrierWithPackagesIsReturned()
		{
			var nsOrderFulfillment = GetNsOrder();
			nsOrderFulfillment.packageUspsList = new NetSuiteSoapWS.ItemFulfillmentPackageUspsList()
			{
				packageUsps = new NetSuiteSoapWS.ItemFulfillmentPackageUsps[]
				{
					new NetSuiteSoapWS.ItemFulfillmentPackageUsps()
					{
						packageTrackingNumberUsps = "TUWOQP12354",
						packageDescrUsps = "Fragile",
						packageWeightUsps = 3.14,
						packageHeightUsps = 3,
						packageWidthUsps = 2,
						packageLengthUsps = 2,
						insuredValueUsps = 4.48
					}
				}
			};

			var svOrderFulfillment = nsOrderFulfillment.ToSVSalesOrderFulfillment();

			svOrderFulfillment.ShipmentCarrier.Should().Be( NetSuiteSalesOrderFulfillmentCarrierEnum.USPS );
			svOrderFulfillment.Packages.First().TrackingNumber.Should().Be( nsOrderFulfillment.packageUspsList.packageUsps.First().packageTrackingNumberUsps );
			svOrderFulfillment.Packages.First().ContentsDescription.Should().Be( nsOrderFulfillment.packageUspsList.packageUsps.First().packageDescrUsps );
			svOrderFulfillment.Packages.First().WeightLbs.Should().Be( (decimal)nsOrderFulfillment.packageUspsList.packageUsps.First().packageWeightUsps );
			svOrderFulfillment.Packages.First().HeightInches.Should().Be( nsOrderFulfillment.packageUspsList.packageUsps.First().packageHeightUsps );
			svOrderFulfillment.Packages.First().WidthInches.Should().Be( nsOrderFulfillment.packageUspsList.packageUsps.First().packageWidthUsps );
			svOrderFulfillment.Packages.First().LengthInches.Should().Be( nsOrderFulfillment.packageUspsList.packageUsps.First().packageLengthUsps );
			svOrderFulfillment.Packages.First().InsuranceCost.Should().Be( (decimal)nsOrderFulfillment.packageUspsList.packageUsps.First().insuredValueUsps );
		}

		private NetSuiteSoapWS.ItemFulfillment GetNsOrder()
		{
			return new NetSuiteSoapWS.ItemFulfillment()
			{
				internalId = "1",
				tranId = "100",
				createdDate = DateTime.UtcNow.AddDays( -1 ),
				lastModifiedDate = DateTime.UtcNow
			};
		}
	}
}
