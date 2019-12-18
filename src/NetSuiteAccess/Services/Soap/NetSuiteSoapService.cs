﻿using CuttingEdge.Conditions;
using Netco.Extensions;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Exceptions;
using NetSuiteAccess.Models;
using NetSuiteAccess.Shared;
using NetSuiteAccess.Throttling;
using NetSuiteSoapWS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services.Soap
{
	public class NetSuiteSoapService
	{
		private Throttler _throttler;
		private Func< string > _additionalLogInfo;
		/// <summary>
		///	Extra logging information
		/// </summary>
		public Func< string > AdditionalLogInfo
		{
			get { return this._additionalLogInfo ?? ( () => string.Empty ); }
			set => _additionalLogInfo = value;
		}

		private NetSuiteConfig _config; 
		private NetSuitePortTypeClient _service;

		private TokenPassport _passport
		{
			get
			{
				return this.CreateTokenPassport();
			}
		}

		public NetSuiteSoapService( NetSuiteConfig config )
		{
			Condition.Requires( config, "config" ).IsNotNull();

			this._config = config;
			this._service = new NetSuitePortTypeClient( NetSuitePortTypeClient.EndpointConfiguration.NetSuitePort );
			this.ConfigureClient( this._service );

			this._throttler = new Throttler( config.ThrottlingOptions.MaxRequestsPerTimeInterval, config.ThrottlingOptions.TimeIntervalInSec, config.ThrottlingOptions.MaxRetryAttempts );
		}

		private void ConfigureClient( NetSuitePortTypeClient client )
		{
			client.Endpoint.Binding.SendTimeout = new TimeSpan( 0, 0, 0, 0, this._config.NetworkOptions.RequestTimeoutMs );

			string subdomain = this._config.Credentials.CustomerId.ToLowerInvariant().Replace("_", "-");
			client.Endpoint.Address = this.GetDataCenterEndpoint( client, $"https://{ subdomain }.suitetalk.api.netsuite.com" );
		}

		private EndpointAddress GetDataCenterEndpoint( NetSuitePortTypeClient client, string dataCenter)
		{
			var endpoint = client.Endpoint.Address;
			var relativeWsPath = endpoint.Uri.LocalPath;

			if ( !dataCenter.EndsWith( "/" ) )
			{
				return new EndpointAddress( dataCenter + relativeWsPath );
			}
			else
			{
				return new EndpointAddress( string.Concat( dataCenter.Substring( 0, dataCenter.Length - 1), relativeWsPath ) );
			}
		}

		public async Task< InventoryItem > GetItemByIdAsync( string sku, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var searchRecord = new ItemSearch()
			{
				basic = new ItemSearchBasic()
				{
					displayName = new SearchStringField()
					{
						@operator = SearchStringFieldOperator.@is,
						operatorSpecified = true,
						searchValue = sku
					}
				}
			};
			
			var searchResponse = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.searchAsync( null, this._passport, null, null, null, searchRecord );
			}, searchRecord.ToJson(), cancellationToken ).ConfigureAwait( false );

			if ( searchResponse.searchResult.status.isSuccess )
			{
				return searchResponse.searchResult.recordList.FirstOrDefault() as InventoryItem;
			}

			return null;
		}

		public async Task< ItemAvailability[] > GetItemInventoryAsync( InventoryItem item, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var filter = new ItemAvailabilityFilter()
			{
				item = new RecordRef[] { new RecordRef() { internalId = item.internalId } }
			};

			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.getItemAvailabilityAsync( null, this._passport, null, null, null, filter );
			}, item.ToJson(), cancellationToken ).ConfigureAwait( false );

			if ( response.getItemAvailabilityResult.status.isSuccess )
			{
				return response.getItemAvailabilityResult.itemAvailabilityList;
			}

			return null;
		}

		public async System.Threading.Tasks.Task AdjustInventory( int accountId, InventoryAdjustmentInventory[] inventory, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var adjustment = new InventoryAdjustment()
			{
				 account = new RecordRef()
				 {
					 internalId = accountId.ToString()
				 },
				 inventoryList = new InventoryAdjustmentInventoryList()
				 {
					 inventory = inventory
				 }, 
			};

			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.addAsync( null, this._passport, null, null, null, adjustment );
			}, adjustment.ToJson(), cancellationToken );

			if ( !response.writeResponse.status.isSuccess )
			{
				throw new NetSuiteException( response.writeResponse.status.statusDetail[0].message );
			}
		}

		public async Task< IEnumerable< NetSuiteAccount > > ListAccounts( CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var accountsSearch = new AccountSearch();
			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.searchAsync( null, this._passport, null, null, null, accountsSearch );
			}, accountsSearch.ToJson(), cancellationToken ).ConfigureAwait( false );

			if ( response.searchResult.status.isSuccess )
			{
				return response.searchResult.recordList
					.Select( r => r as Account )
					.Select( r => new NetSuiteAccount() { Id = int.Parse( r.internalId ), Name = r.acctName, Number = r.acctNumber } ).ToArray();
			}

			return null;
		}

		private Task< T > ThrottleRequestAsync< T >( Mark mark, Func< CancellationToken, Task< T > > processor, string payload, CancellationToken token )
		{
			return this._throttler.ExecuteAsync( () =>
			{
				return new ActionPolicy( this._config.NetworkOptions.RetryAttempts, this._config.NetworkOptions.DelayBetweenFailedRequestsInSec, this._config.NetworkOptions.DelayFailRequestRate )
					.ExecuteAsync( async () =>
					{
						using( var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource( token ) )
						{
							NetSuiteLogger.LogStarted( this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo(), payload: payload ) );
							linkedTokenSource.CancelAfter( this._config.NetworkOptions.RequestTimeoutMs );

							var result = await processor( linkedTokenSource.Token ).ConfigureAwait( false );

							NetSuiteLogger.LogEnd( this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo(), result: result.ToJson() ) );

							return result;
						}
					}, 
					( exception, timeSpan, retryCount ) =>
					{
						string retryDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo(), errors: exception.Message );
						NetSuiteLogger.LogTraceRetryStarted( timeSpan.Seconds, retryCount, retryDetails );
					},
					() => CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() ),
					NetSuiteLogger.LogTraceException );
			} );
		}

		private string CreateMethodCallInfo( Mark mark = null, string errors = "", string result = "", string additionalInfo = "", string payload = "", [ CallerMemberName ] string memberName = "" )
		{
			var str = string.Format(
				"{{MethodName: {0}, Mark: '{1}', ServiceEndPoint: '{2}', {3} {4}{5}{6}}}",
				memberName,
				mark ?? Mark.Blank(),
				this._service.Endpoint.Address.Uri,
				string.IsNullOrWhiteSpace( errors ) ? string.Empty : ", Errors:" + errors,
				string.IsNullOrWhiteSpace( result ) ? string.Empty : ", Result:" + result,
				string.IsNullOrWhiteSpace( additionalInfo ) ? string.Empty : ", " + additionalInfo,
				string.IsNullOrWhiteSpace( payload ) ? string.Empty : ", Payload:" + payload
			);
			return str;
		}

		private TokenPassport CreateTokenPassport()
		{
			var tokenPassport = new TokenPassport()
			{
				account = this._config.Credentials.CustomerId.ToUpper(),
				consumerKey = this._config.Credentials.ConsumerKey,
				token = this._config.Credentials.TokenId,
				nonce = GetRandomSessionNonce(),
				timestamp = GetUtcEpochTime()
			};
			return this.SignPassport( tokenPassport );
		}

		private string GetRandomSessionNonce()
		{
			return Guid.NewGuid().ToString().Replace( "-", "" ).Substring( 0, 11 ).ToUpper();
		}

		private long GetUtcEpochTime()
		{
			return (int)( DateTime.UtcNow - new DateTime( 1970, 1, 1 ) ).TotalSeconds;
		}

		private TokenPassport SignPassport( TokenPassport passport )
		{
			string baseString = passport.account + "&" + passport.consumerKey + "&" + passport.token + "&" + passport.nonce + "&" + passport.timestamp;
			string key = this._config.Credentials.ConsumerSecret + "&" + this._config.Credentials.TokenSecret;
			string signature = string.Empty;
			
			var encoding = new ASCIIEncoding();
			byte[] keyBytes = encoding.GetBytes( key );
			byte[] baseStringBytes = encoding.GetBytes( baseString );
			using ( var hmacSha1 = new HMACSHA1( keyBytes ) )
			{
				byte[] hashBaseString = hmacSha1.ComputeHash( baseStringBytes );
				signature = Convert.ToBase64String( hashBaseString );
			}
			
			passport.signature = new TokenPassportSignature()
			{
				algorithm = "HMAC-SHA1",
				Value = signature
			};
			
			return passport;
		}
	}
}