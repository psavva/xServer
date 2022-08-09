﻿using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using x42.Controllers.Results;
using x42.Controllers.Requests;
using x42.Feature.Database.Tables;
using x42.Server;
using x42.Server.Results;
using x42.Feature.Profile;
using x42.Utilities.JsonErrors;
using System.Net;
using x42.Feature.PriceLock;
using System.Linq;
using System.Collections.Generic;
using System;
using x42.Feature.PowerDns;
using x42.Feature.WordPressPreview.Models;
using x42.Feature.Metrics;
using x42.Feature.Metrics.Models;

namespace x42.Controllers.Public
{
    /// <inheritdoc />
    /// <summary>
    ///     Controller providing Public Methods for the server.
    /// </summary>
    [ApiController]
    [Route("")]
    public class PublicController : Controller
    {
        private readonly XServer _xServer;
        private readonly ProfileFeature _profileFeature;
        private readonly PriceFeature _priceFeature;
        private readonly PowerDnsFeature _powerDnsFeature;
        private readonly WordPressPreviewFeature _wordPressPreviewFeature;

        public PublicController(
            XServer xServer, 
            ProfileFeature profileFeature, 
            PriceFeature priceFeature, 
            PowerDnsFeature powerDnsFeature, 
            WordPressPreviewFeature wordPressPreviewFeature)
        private readonly XServer xServer;
        private readonly ProfileFeature profileFeature;
        private readonly PriceFeature priceFeature;
        private readonly MetricsFeature _metricsFeature;
        public PublicController(XServer xServer, ProfileFeature profileFeature, PriceFeature priceFeature, MetricsFeature metricsFeature)
        {
            _xServer = xServer;
            _profileFeature = profileFeature;
            _priceFeature = priceFeature;
            _powerDnsFeature = powerDnsFeature;
            _wordPressPreviewFeature = wordPressPreviewFeature;
            this.xServer = xServer;
            this.profileFeature = profileFeature;
            this.priceFeature = priceFeature;
            _metricsFeature = metricsFeature;
        }

        /// <summary>
        ///     Returns simple information about the xServer.
        /// </summary>
        /// <returns>A JSON object containing the xServer information.</returns>
        [HttpGet]
        [Route("ping")]
        public IActionResult Ping()
        {
            _xServer.Stats.IncrementPublicRequest();
            PingResult pingResult = new PingResult()
            {
                Version = _xServer.Version.ToString(),
                BestBlockHeight = _xServer.AddressIndexerHeight,
                Tier = (int)_xServer.Stats.TierLevel
            };
            return Json(pingResult);
        }

        /// <summary>
        ///     Returns the top xServers available.
        /// </summary>
        /// <param name="top">The number of top xServers to return.</param>
        /// <returns>A JSON object containing the top xServers available.</returns>
        [HttpGet]
        [Route("gettop")]
        public IActionResult GetTop([FromQuery] int top = 10)
        {
            _xServer.Stats.IncrementPublicRequest();
            TopResult topResult = _xServer.GetTopXServers(top);
            return Json(topResult);
        }

        [HttpGet]
        [Route("addprofile")]
        public async Task<IActionResult> AddTestProfileAsync()
        {
           
            await _profileFeature.AddTestProfile();
            return Ok();
        }


        [HttpGet]
        [Route("wordpresspreviewdomains")]
        public async Task<IActionResult> ReserveWordpressPreviewDNS()
        {

            var result = await _wordPressPreviewFeature.GetWordPressPreviewDomains();
            return Ok(result);
        }
        [HttpPost]
        [Route("reservewordpresspreviewDNS")]
        public async Task<IActionResult> ReserveProfile([FromBody] WordPressReserveRequest reserveRequest)
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Two)
            {
                var reserveResult = await _wordPressPreviewFeature.ReserveWordpressPreviewDNS(reserveRequest);
                return Json(reserveResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 2 requirement not meet", "The node you requested is not a tier 2 node.");
            }
        }

        /// <summary>
        ///     Registers a servernode to the network.
        /// </summary>
        /// <param name="registerRequest">The object with all of the nessesary data to register a xServer.</param>
        /// <returns>A <see cref="RegisterResult" /> with registration result.</returns>
        [HttpPost]
        [Route("registerserver")]
        public async Task<IActionResult> RegisterServerAsync([FromBody] ServerRegisterRequest registerRequest)
        {
            _xServer.Stats.IncrementPublicRequest();
            ServerNodeData serverNode = new ServerNodeData()
            {
                ProfileName = registerRequest.ProfileName,
                NetworkAddress = registerRequest.NetworkAddress,
                NetworkPort = registerRequest.NetworkPort,
                KeyAddress = registerRequest.KeyAddress,
                SignAddress = registerRequest.SignAddress,
                FeeAddress = registerRequest.FeeAddress,
                Signature = registerRequest.Signature,
                Tier = registerRequest.Tier,
                NetworkProtocol = registerRequest.NetworkProtocol
            };

            RegisterResult registerResult = await _xServer.Register(serverNode);
            return Json(registerResult);
        }

        /// <summary>
        ///     Returns the active xServer count.
        /// </summary>
        /// <returns>A JSON object containing a count of active xServers.</returns>
        [HttpGet]
        [Route("getactivecount")]
        public IActionResult GetActiveCount()
        {
            _xServer.Stats.IncrementPublicRequest();
            CountResult topResult = new CountResult()
            {
                Count = _xServer.GetActiveServerCount()
            };
            return Json(topResult);
        }

        /// <summary>
        ///     Returns the active xServers from Id.
        /// </summary>
        /// <returns>A JSON object containing a list of active xServers.</returns>
        [HttpGet]
        [Route("getactivexservers")]
        public IActionResult GetActiveXServers(int fromId)
        {
            _xServer.Stats.IncrementPublicRequest();
            var allServers = _xServer.GetActiveXServers(fromId);
            return Json(allServers);
        }

        /// <summary>
        ///     Searches for the xServer by profile name or sign address.
        /// </summary>
        /// <returns>A JSON object containing the xServer search result.</returns>
        [HttpGet]
        [Route("searchforxserver")]
        public IActionResult SearchForXServer(string profileName = "", string signAddress = "")
        {
            _xServer.Stats.IncrementPublicRequest();
            var foundxServer = _xServer.SearchForXServer(profileName, signAddress);
            return Json(foundxServer);
        }

        /// <summary>
        ///     Will lookup the profile, and return the profile data.
        /// </summary>
        /// <returns>A JSON object containing the profile requested.</returns>
        [HttpGet]
        [Route("getprofile")]
        public IActionResult GetProfile(string name = "", string keyAddress = "")
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Two)
            {
                var profileRequest = new ProfileRequest()
                {
                    Name = name,
                    KeyAddress = keyAddress
                };
                var profile = _profileFeature.GetProfile(profileRequest);
                return Json(profile);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 2 requirement not meet", "The node you requested is not a tier 2 node.");
            }
        }

        /// <summary>
        ///     Reserves a profile to the network.
        /// </summary>
        /// <param name="reserveRequest">The object with all of the nessesary data to reserve a profile.</param>
        /// <returns>A <see cref="ReserveProfileResult" /> with reservation result.</returns>
        [HttpPost]
        [Route("reserveprofile")]
        public async Task<IActionResult> ReserveProfile([FromBody] ProfileReserveRequest reserveRequest)
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Two)
            {
                var reserveResult = await _profileFeature.ReserveProfile(reserveRequest);
                return Json(reserveResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 2 requirement not meet", "The node you requested is not a tier 2 node.");
            }
        }

        /// <summary>
        ///     Used for syncing the profile reservations.
        /// </summary>
        /// <param name="receiveProfileReserveRequest">The object with all of the nessesary data to sync a profile reservation.</param>
        /// <returns>A <see cref="bool" /> with reservation result.</returns>
        [HttpPost]
        [Route("receiveprofilereservation")]
        public async Task<IActionResult> ReceiveProfileReservation([FromBody] ReceiveProfileReserveRequest receiveProfileReserveRequest)
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Two)
            {
                var reserveResult = await _profileFeature.ReceiveProfileReservation(receiveProfileReserveRequest);
                return Json(reserveResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 2 requirement not meet", "The node you requested is not a tier 2 node.");
            }
        }

        /// <summary>
        ///     Will get next 10 profiles from the last confirmed block requested.
        /// </summary>
        /// <param name="fromBlock">A number to specificy what block to get the list of profiles.</param>
        /// <returns>A <see cref="List{ProfilesResult}" /> with list of profiles from specified block.</returns>
        [HttpGet]
        [Route("getnextprofiles")]
        public IActionResult GetNextProfiles(int fromBlock)
        {
            _xServer.Stats.IncrementPublicRequest();
            var reserveResult = _profileFeature.GetProfiles(fromBlock);
            return Json(reserveResult);
        }

        /// <summary>
        ///     Get my average price.
        /// </summary>
        /// <returns>A JSON object containing price information.</returns>
        [HttpGet]
        [Route("getprice")]
        public IActionResult GetPrice(int fiatPairId)
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var fiatPair = _priceFeature.FiatPairs.Where(f => (int)f.Currency == fiatPairId).FirstOrDefault();
                if (fiatPair != null)
                {
                    PriceResult priceResult = new PriceResult()
                    {
                        Price = fiatPair.GetMytPrice()
                    };
                    return Json(priceResult);
                }
                else
                {
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Invalid Pair", "The pair supplied does not exist.");
                }
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Get my average price list.
        /// </summary>
        /// <returns>A JSON object containing price information.</returns>
        [HttpGet]
        [Route("getprices")]
        public IActionResult GetPrices()
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var priceResults = new List<PriceResult>();
                foreach (var pair in _priceFeature.FiatPairs)
                {
                    PriceResult priceResult = new PriceResult()
                    {
                        Price = pair.GetMytPrice(),
                        Pair = (int)pair.Currency
                    };
                    priceResults.Add(priceResult);
                }
                return Json(priceResults);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Create a price lock.
        /// </summary>
        /// <param name="priceLockRequest">The object with all of the nessesary data to create a price lock.</param>
        /// <returns>A <see cref="PriceLockResult" /> with price lock results.</returns>
        [HttpPost]
        [Route("createpricelock")]
        public async Task<IActionResult> CreatePriceLock([FromBody] CreatePriceLockRequest priceLockRequest)
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var priceLockResult = await _priceFeature.CreatePriceLock(priceLockRequest);
                return Json(priceLockResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }


        /// <summary>
        ///     Create a price lock.
        /// </summary>
        /// <param name="priceLockRequest">The object with all of the nessesary data to create a price lock.</param>
        /// <returns>A <see cref="PriceLockResult" /> with price lock results.</returns>
        [HttpGet]
        [Route("zones")]
        public async Task<IActionResult> GetAllZones()
        {
 
                var priceLockResult = await _powerDnsFeature.GetAllZones();
                return Json(priceLockResult);
      
        }


        /// <summary>
        ///     Update a price lock.
        /// </summary>
        /// <param name="priceLockData">The object with all of the nessesary data to update a price lock.</param>
        /// <returns>A <see cref="bool" /> with the result.</returns>
        [HttpPost]
        [Route("updatepricelock")]
        public async Task<IActionResult> UpdatePriceLock([FromBody] PriceLockResult priceLockData)
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var priceLockResult = await _priceFeature.UpdatePriceLock(priceLockData);
                return Json(priceLockResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Get available price lock pairs
        /// </summary>
        /// <returns>A list with all of the available pairs for a price lock.</returns>
        [HttpGet]
        [Route("getavailablepairs")]
        public IActionResult GetAvailablePairs()
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var pairList = _priceFeature.GetPairList();
                return Json(pairList);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Get a price lock.
        /// </summary>
        /// <param name="priceLockId">The ID of the price lock.</param>
        /// <returns>A <see cref="PriceLockResult" /> with price lock information.</returns>
        [HttpGet]
        [Route("getpricelock")]
        public IActionResult GetPriceLock(string priceLockId)
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                if (Guid.TryParse(priceLockId, out Guid validPriceLockId))
                {
                    var priceLockResult = _priceFeature.GetPriceLock(validPriceLockId);
                    return Json(priceLockResult);
                }
                else
                {
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Invalid pricelock id", "The price lock id is not a valid Guid");
                }
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Submit the payment for a price lock.
        /// </summary>
        /// <param name="submitPaymentRequest">The object with all of the nessesary data to submit payment.</param>
        /// <returns>A <see cref="SubmitPaymentResult" /> with submission results.</returns>
        [HttpPost]
        [Route("submitpayment")]
        public async Task<IActionResult> SubmitPayment([FromBody] SubmitPaymentRequest submitPaymentRequest)
        {
            _xServer.Stats.IncrementPublicRequest();
            if (_xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var submitPaymentResult = await _priceFeature.SubmitPayment(submitPaymentRequest);
                return Json(submitPaymentResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }


        /// <summary>
        ///     Get Hardware Metrics
        /// </summary>
        /// <param name="hardwaremetrics">Gets Hardware Metrics of the Host</param>
        /// <returns>A <see cref="ContainerStatsModel" /> with hardware metrics.</returns>
        [HttpGet]
        [Route("hardwaremetrics")]
        public ActionResult<HostStatsModel> HardwareMetricsAsync()
        {
            var response = _metricsFeature.getHardwareMetricsAsync();
            return Json(response);
        }

    }
}