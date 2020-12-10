using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GoogleHelper
{
    public class GoogleGeoCoder
    {
        #region hk
        private string _googleGeoApiBaseUrl = "https://maps.googleapis.com/maps/api/geocode/json?address={address}";
        private string _googleGeoApiKey = "";
        private bool _googleMapApiOverLimit = false;
        private System.DateTime? _dateWentOverLimit;
        private GeoCodeResponse _geoCodeResponse;
        public Dictionary<string, GeoCodeResponse> _addresses = new Dictionary<string, GeoCodeResponse>();
        private string _addressesJson;
        private JObject _addressObject;
        private bool _cacheResults = true;
        #endregion

        #region ctor
        public GoogleGeoCoder() { }
        public GoogleGeoCoder(string key)
        {
            _googleGeoApiKey = key;
        }
        #endregion

        #region props
        public string GoogleMapAPIUrl
        {
            get
            {
                return _googleGeoApiBaseUrl;
            }

            set
            {
                _googleGeoApiBaseUrl = value;
            }
        }
        public bool CacheResults
        {
            get
            {
                return _cacheResults;
            }

            set
            {
                _cacheResults = value;
            }
        }
        public bool ExceededDailyLimit
        {
            get
            {
                if (_dateWentOverLimit != null && _dateWentOverLimit == DateTime.UtcNow.Date)
                    return _googleMapApiOverLimit;
                return false;
            }
        }
        #endregion

        #region functions
        public GeoCodeResponse ParseResultsJson(string addressesJson, string address)
        {
            _addressObject = JObject.Parse(addressesJson);
            _geoCodeResponse = null;
            try
            {
                _geoCodeResponse = JsonConvert.DeserializeObject<GeoCodeResponse>(addressesJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            if (_geoCodeResponse == null || _geoCodeResponse.status == null)
            {
                return _geoCodeResponse;
            }

            switch (_geoCodeResponse.status.ToUpper())
            {
                case "OK":
                case "ZERO_RESULTS":
                case "REQUEST_DENIED":
                case "INVALID_REQUEST":
                    {
                        if (_cacheResults)
                            _addresses.Add(address, _geoCodeResponse);
                        break;
                    }

                case "OVER_QUERY_LIMIT":
                    {
                        _dateWentOverLimit = System.DateTime.Now.Date;
                        _googleMapApiOverLimit = true;
                        break;
                    }

                case "UNKNOWN_ERROR":
                    {
                        //if (!string.IsNullOrWhiteSpace(_googleGeoApiKey))
                        //{
                        //    _addressesJson = await GetGoogleMapEncode(address);
                        //    _addressObject = JObject.Parse(_addressesJson);
                        //    _geoCodeResponse = JsonConvert.DeserializeObject<GeoCodeResponse>(_addressesJson);
                        //    if (_geoCodeResponse.status == "OVER_QUERY_LIMIT")
                        //    {
                        //        _dateWentOverLimit = System.DateTime.Now.Date;
                        //        _googleMapApiOverLimit = true;
                        //    }
                        //    else if (_cacheResults)
                        //        _addresses.Add(address, _geoCodeResponse);
                        //}

                        break;
                    }
            }

            return _geoCodeResponse;
        }
        public GeoCodeResponse ParsePlaceJson(string addressesJson, string address)
        {
            _addressObject = JObject.Parse(addressesJson);
            _geoCodeResponse = new GeoCodeResponse();
            try
            {
                var result = JsonConvert.DeserializeObject<Result>(addressesJson);
                _geoCodeResponse = new GeoCodeResponse
                {
                    results = new Result[] { result },
                    status = "OK"
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _geoCodeResponse.status = ex.Message;

            }


            switch (_geoCodeResponse.status.ToUpper())
            {
                case "OK":
                case "ZERO_RESULTS":
                case "REQUEST_DENIED":
                case "INVALID_REQUEST":
                    if (_cacheResults)
                        _addresses.Add(address, _geoCodeResponse);
                    break;

                case "OVER_QUERY_LIMIT":
                    _dateWentOverLimit = System.DateTime.Now.Date;
                    _googleMapApiOverLimit = true;
                    break;
                case "UNKNOWN_ERROR":
                    //if (!string.IsNullOrWhiteSpace(_googleGeoApiKey))
                    //{
                    //    _addressesJson = await GetGoogleMapEncode(address);
                    //    _addressObject = JObject.Parse(_addressesJson);
                    //    _geoCodeResponse = JsonConvert.DeserializeObject<GeoCodeResponse>(_addressesJson);
                    //    if (_geoCodeResponse.status == "OVER_QUERY_LIMIT")
                    //    {
                    //        _dateWentOverLimit = System.DateTime.Now.Date;
                    //        _googleMapApiOverLimit = true;
                    //    }
                    //    else if (_cacheResults)
                    //        _addresses.Add(address, _geoCodeResponse);
                    //}

                    break;
            }

            return _geoCodeResponse;
        }
        public async Task<GeoCodeResponse> ReverseLookupAsync(string address)
        {
            if (_cacheResults)
                if (_addresses.ContainsKey(address))
                {
                    _geoCodeResponse = _addresses[address];
                    return _addresses[address];
                }

            _addressesJson = await GetGoogleMapEncode(address);

            return await this.ParseResultsJsonAsync(_addressesJson, address);
            //_addressObject = JObject.Parse(_addressesJson);
            //_geoCodeResponse = JsonConvert.DeserializeObject<GeoCodeResponse>(_addressesJson);
            //switch (_geoCodeResponse.status.ToUpper())
            //{
            //    case "OK":
            //    case "ZERO_RESULTS":
            //    case "REQUEST_DENIED":
            //    case "INVALID_REQUEST":
            //        {
            //            if (_cacheResults)
            //                _addresses.Add(address, _geoCodeResponse);
            //            break;
            //        }

            //    case "OVER_QUERY_LIMIT":
            //        {
            //            _dateWentOverLimit = System.DateTime.Now.Date;
            //            _googleMapApiOverLimit = true;
            //            break;
            //        }

            //    case "UNKNOWN_ERROR":
            //        {
            //            _addressesJson = await GetGoogleMapEncode(address);
            //            _addressObject = JObject.Parse(_addressesJson);
            //            _geoCodeResponse = JsonConvert.DeserializeObject<GeoCodeResponse>(_addressesJson);
            //            if (_geoCodeResponse.status == "OVER_QUERY_LIMIT")
            //            {
            //                _dateWentOverLimit = System.DateTime.Now.Date;
            //                _googleMapApiOverLimit = true;
            //            }
            //            else if (_cacheResults)
            //                _addresses.Add(address, _geoCodeResponse);
            //            break;
            //        }
            //}

            //return _geoCodeResponse;
        }
        public async Task<GeoCodeResponse> ParseResultsJsonAsync(string addressesJson, string address)
        {
            _addressObject = JObject.Parse(addressesJson);
            _geoCodeResponse = null;
            try
            {
                _geoCodeResponse = JsonConvert.DeserializeObject<GeoCodeResponse>(addressesJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            if (_geoCodeResponse == null || _geoCodeResponse.status == null)
            {
                return _geoCodeResponse;
            }

            switch (_geoCodeResponse.status.ToUpper())
            {
                case "OK":
                case "ZERO_RESULTS":
                case "REQUEST_DENIED":
                case "INVALID_REQUEST":
                    {
                        if (_cacheResults)
                            _addresses.Add(address, _geoCodeResponse);
                        break;
                    }

                case "OVER_QUERY_LIMIT":
                    {
                        _dateWentOverLimit = System.DateTime.Now.Date;
                        _googleMapApiOverLimit = true;
                        break;
                    }

                case "UNKNOWN_ERROR":
                    {
                        if (!string.IsNullOrWhiteSpace(_googleGeoApiKey))
                        {
                            _addressesJson = await GetGoogleMapEncode(address);
                            _addressObject = JObject.Parse(_addressesJson);
                            _geoCodeResponse = JsonConvert.DeserializeObject<GeoCodeResponse>(_addressesJson);
                            if (_geoCodeResponse.status == "OVER_QUERY_LIMIT")
                            {
                                _dateWentOverLimit = System.DateTime.Now.Date;
                                _googleMapApiOverLimit = true;
                            }
                            else if (_cacheResults)
                                _addresses.Add(address, _geoCodeResponse);
                        }

                        break;
                    }
            }

            return _geoCodeResponse;
        }
        public void ClearCache()
        {
            _addresses.Clear();
        }
        #endregion

        #region helpers
        private Dictionary<string, GeoCodeResponse> AddressCache
        {
            get
            {
                return _addresses;
            }
        }
        private async Task<string> GetGoogleMapEncode(string address)
        {
            string responseString = "";

            if (!string.IsNullOrWhiteSpace(_googleGeoApiKey))
            {
                var requestUri = _googleGeoApiBaseUrl.Replace("{address}", address) + "&key=" + _googleGeoApiKey;

                var request = WebRequest.Create(requestUri);
                var response = await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    responseString = reader.ReadToEnd();
                }
            }
            return responseString;
        }
        private bool ContainsAddress(string address)
        {
            return _addresses.ContainsKey(address);
        }
        #endregion
    }

    public class AddressComponent
    {
        public string long_name
        {
            get;
            set;
        }

        public string short_name
        {
            get;
            set;
        }

        public string[] types
        {
            get;
            set;
        }
    }

    public class Northeast
    {
        public double lat
        {
            get;
            set;
        }

        public double lng
        {
            get;
            set;
        }
    }

    public class Southwest
    {
        public double lat
        {
            get;
            set;
        }

        public double lng
        {
            get;
            set;
        }
    }

    public class Bounds
    {
        public Northeast northeast
        {
            get;
            set;
        }

        public Southwest southwest
        {
            get;
            set;
        }
    }

    public class Location
    {
        public double lat
        {
            get;
            set;
        }

        public double lng
        {
            get;
            set;
        }
    }

    public class Viewport
    {
        public Northeast northeast
        {
            get;
            set;
        }

        public Southwest southwest
        {
            get;
            set;
        }
    }

    public class Geometry
    {
        public Bounds bounds
        {
            get;
            set;
        }

        public Location location
        {
            get;
            set;
        }

        public string location_type
        {
            get;
            set;
        }

        public Viewport viewport
        {
            get;
            set;
        }
    }

    public class Result
    {
        public AddressComponent[] address_components
        {
            get;
            set;
        }

        public string formatted_address
        {
            get;
            set;
        }

        public Geometry geometry
        {
            get;
            set;
        }

        public bool partial_match
        {
            get;
            set;
        }

        public string place_id
        {
            get;
            set;
        }

        public string[] types
        {
            get;
            set;
        }
    }

    public class GeoCodeResponse
    {
        private Dictionary<string, string> _dataDict = null /* TODO Change to default(_) if this is not a reference type */;
        public GeoCodeResponse()
        {
        }

        private void InitializeDictionaries()
        {
            _dataDict = new Dictionary<string, string>
            {
                { "StreetNumber", "street_number" },
                { "Street", "route" },
                { "Premise", "premise" },
                { "SubPremise", "subpremise" },
                { "Neighborhood", "neighborhood" },
                { "City", "locality" },
                { "County", "administrative_area_level_2" },
                { "State", "administrative_area_level_1" },
                { "Zip", "postal_code" },
                { "Zip4", "postal_code_suffix" },
                { "Country", "country" }
            };
        }

        public Result[] results
        {
            get;
            set;
        }

        public string status
        {
            get;
            set;
        }

        public string Address
        {
            get
            {
                if (this.status != "OK")
                    return "";
                return ParseGeoGoogleResponse(this.results[0], "Address");
            }
        }
        public string StreetNumber
        {
            get
            {
                if (this.status != "OK")
                    return "";
                return ParseGeoGoogleResponse(this.results[0], "StreetNumber");
            }
        }
        public string Address1
        {
            get
            {
                if (this.status != "OK")
                    return "";
                return ParseGeoGoogleResponse(this.results[0], "Address1");
            }
        }
        public string Address2
        {
            get
            {
                if (this.status != "OK")
                    return "";
                return ParseGeoGoogleResponse(this.results[0], "Address2");
            }
        }
        public string Neighborhood
        {
            get
            {
                if (this.status != "OK")
                    return "";
                return ParseGeoGoogleResponse(this.results[0], "Neighborhood");
            }
        }
        public string City
        {
            get
            {
                if (this.status != "OK")
                    return "";
                return ParseGeoGoogleResponse(this.results[0], "City");
            }
        }
        public string County
        {
            get
            {
                if (this.status != "OK")
                    return "";
                return ParseGeoGoogleResponse(this.results[0], "County");
            }
        }
        public string State
        {
            get
            {
                if (this.status != "OK")
                    return "";
                return ParseGeoGoogleResponse(this.results[0], "State");
            }
        }
        public string Zip
        {
            get
            {
                if (this.status != "OK")
                    return "";
                return ParseGeoGoogleResponse(this.results[0], "ZipFull");
            }
        }
        public string Country
        {
            get
            {
                if (this.status != "OK")
                    return "";
                return ParseGeoGoogleResponse(this.results[0], "Country");
            }
        }
        public string LatLon
        {
            get
            {
                if (this.status != "OK")
                    return "";

                return this.results[0].geometry.location.lat.ToString() + "," + this.results[0].geometry.location.lng.ToString();
            }
        }

        private string ParseGeoGoogleResponse(Result result, string addressPiece)
        {
            string parsedResponse = result.formatted_address;
            string subPremise;
            if (_dataDict == null)
                this.InitializeDictionaries();
            switch (addressPiece)
            {
                case "Address":
                    {
                        parsedResponse = GetGoogleGeoAddressPart(result, "StreetNumber") + " " + GetGoogleGeoAddressPart(result, "Street");
                        if (!string.IsNullOrWhiteSpace(GetGoogleGeoAddressPart(result, "Premise")))
                        {
                            if (!string.IsNullOrWhiteSpace(parsedResponse))
                                parsedResponse += ", ";
                            parsedResponse += GetGoogleGeoAddressPart(result, "Premise");
                        }

                        if (!string.IsNullOrWhiteSpace(GetGoogleGeoAddressPart(result, "SubPremise")))
                        {
                            if (!string.IsNullOrWhiteSpace(parsedResponse))
                                parsedResponse += ", ";
                            subPremise = GetGoogleGeoAddressPart(result, "SubPremise");
                            if (subPremise.Length < 4)
                                subPremise = subPremise.ToUpper();
                            parsedResponse += "#" + subPremise;
                        }

                        break;
                    }

                case "Address1":
                    {
                        parsedResponse = GetGoogleGeoAddressPart(result, "StreetNumber") + " " + GetGoogleGeoAddressPart(result, "Street");
                        break;
                    }

                case "Address2":
                    {
                        parsedResponse = "";
                        if (!string.IsNullOrWhiteSpace(GetGoogleGeoAddressPart(result, "Premise")))
                            parsedResponse = GetGoogleGeoAddressPart(result, "Premise");
                        if (!string.IsNullOrWhiteSpace(GetGoogleGeoAddressPart(result, "SubPremise")))
                        {
                            if (!string.IsNullOrWhiteSpace(parsedResponse))
                                parsedResponse += ", ";
                            subPremise = GetGoogleGeoAddressPart(result, "SubPremise");
                            if (subPremise.Length < 4)
                                subPremise = subPremise.ToUpper();
                            parsedResponse += "#" + subPremise;
                        }

                        break;
                    }

                case "StreetNumber":
                case "StreetName":
                case "Neighborhood":
                case "Vicinity":
                case "City":
                case "County":
                case "State":
                case "Zip":
                case "Zip4":
                case "Country":
                    {
                        parsedResponse = GetGoogleGeoAddressPart(result, addressPiece);
                        break;
                    }

                case "ZipFull":
                    {
                        parsedResponse = GetGoogleGeoAddressPart(result, "Zip") + "-" + GetGoogleGeoAddressPart(result, "Zip4");
                        if (parsedResponse.LastIndexOf("-") == parsedResponse.Trim().Length - 1)
                            parsedResponse = parsedResponse.Replace("-", "");
                        break;
                    }
            }

            return parsedResponse;
        }

        private string GetGoogleGeoAddressPart(Result result, string partName)
        {
            foreach (AddressComponent addressComponent in result.address_components)
                if (addressComponent.types[0] == _dataDict[partName])
                    return addressComponent.short_name;
            return "";
        }
    }
}
