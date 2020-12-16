using System;
using FluentAssertions;
using NetSuiteAccess.Services.Customers;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using NetSuiteAccess.Models;
using Netco.Logging;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class CustomerTests : BaseTest
	{
		private INetSuiteCustomersService _customersService;
		private string _individualCustomerId = "1680";
		private string _companyCustomerId = "1100";

		[ SetUp ]
		public void Init()
		{
			this._customersService = new NetSuiteCustomersService( base.Config );
		}

		[ Test ]
		public void GetIndividualCustomerInfo()
		{
			var customerInfo = this._customersService.GetCustomerInfoByIdAsync( this._individualCustomerId, CancellationToken.None ).Result;

			customerInfo.Should().NotBeNull();
			customerInfo.FirstName.Should().NotBeNullOrWhiteSpace();
			customerInfo.LastName.Should().NotBeNullOrWhiteSpace();
			customerInfo.Phone.Should().NotBeNullOrWhiteSpace();
			customerInfo.Email.Should().NotBeNullOrWhiteSpace();
		}

		[ Test ]
		public void GetCompanyCustomerInfo()
		{
			var customerInfo = this._customersService.GetCustomerInfoByIdAsync( this._companyCustomerId, CancellationToken.None ).Result;

			customerInfo.Should().NotBeNull();
			customerInfo.CompanyName.Should().NotBeNullOrWhiteSpace();
			customerInfo.FirstName.Should().BeNullOrWhiteSpace();
			customerInfo.LastName.Should().BeNullOrWhiteSpace();
			customerInfo.Phone.Should().NotBeNullOrWhiteSpace();
			customerInfo.Email.Should().NotBeNullOrWhiteSpace();
		}

		[ Test ]
		//Slow, ~1 minute
		public async Task GetCustomersInfoByIdsAsync_HandlesBatchOfOver1000()
		{
			var customersIds = new string[ 1005 ];
			var random = new Random( DateTime.Now.Millisecond );
			for ( var i = 0; i < 1005; i++)
			{
				customersIds[ i ] = random.Next( 1000 ).ToString();
			}

			var result = await this._customersService.GetCustomersInfoByIdsAsync( customersIds, CancellationToken.None );

			result.Should().NotBeNull();
		}

		[ Test ]
		public async Task CreateCustomerAsync()
		{
			var firstName = "Ivan" + new Random().Next( 1, 1000 );
			var email = "ivan.ivanov" + Guid.NewGuid().ToString().Substring( 0, 5 ) + "@skuvault.com";
			var newCustomer = new NetSuiteCustomer()
			{
				FirstName = firstName,
				LastName = "Ivanov",
				Email = email,
				Phone = "1-502-694-5210",
				CompanyName = "SkuVault",
				Address = new NetSuiteAddress()
				{
					Country = "USA",
					City = "Louisville",
					Region = "KY",
					Address1 = "2509 Plantside Drive",
					PostalCode = "40299"
				}
			};

			await this._customersService.CreateCustomerAsync( newCustomer, CancellationToken.None );

			var nsCustomerInfo = await this._customersService.GetCustomerInfoByEmailAsync( email, CancellationToken.None );
			nsCustomerInfo.FirstName.Should().Be( firstName );
			nsCustomerInfo.LastName.Should().Be( newCustomer.LastName );
			nsCustomerInfo.Email.Should().Be( email );
		}
	}
}