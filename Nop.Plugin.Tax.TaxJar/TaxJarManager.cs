using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json;

namespace Nop.Plugin.Tax.TaxJar
{
    /// <summary>
    /// The TaxJar manager
    /// </summary>
    public class TaxJarManager
    {
        private const string API_URL = "https://api.taxjar.com/v2/";

        /// <summary>
        /// API token
        /// </summary>
        public string Api { get; set; }

        /// <summary>
        /// Get tax rate from TaxJar API
        /// </summary>
        /// <param name="country">Two-letter ISO country code</param>
        /// <param name="city">City</param>
        /// <param name="street">Address</param>
        /// <param name="zip">Zip postal code</param>
        /// <returns>Response from API</returns>
        public TaxJarResponse GetTaxRate(string country, string city, string street, string zip)
        {
            var parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters.Add("country", country);
            parameters.Add("city", city);
            parameters.Add("street", street);

            var request = (HttpWebRequest)WebRequest.Create(string.Format("{0}rates/{1}?{2}", API_URL, zip, parameters));
            request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Bearer {0}", Api));
            request.Method = "GET";
            request.UserAgent = "nopCommerce";

            try
            {
                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<TaxJarResponse>(streamReader.ReadToEnd());
                }
            }
            catch (WebException e)
            {
                using (var streamReader = new StreamReader(e.Response.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<TaxJarResponse>(streamReader.ReadToEnd());
                }
            }
        }
    }

    /// <summary>
    /// Response from API
    /// </summary>
    public class TaxJarResponse
    {
        [JsonProperty(PropertyName = "rate")]
        public TaxJarRate Rate { get; set; }

        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }

        [JsonProperty(PropertyName = "detail")]
        public string ErrorDetails { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string ErrorStatus { get; set; }

        /// <summary>
        /// Returns true when a request is successful
        /// </summary>
        public bool IsSuccess
        {
            get { return string.IsNullOrEmpty(Error); }
        }

        /// <summary>
        /// Error summary message
        /// </summary>
        public string ErrorMessage
        {
            get { return IsSuccess ? string.Empty : string.Format("{0} - {1} ({2})", ErrorStatus, Error, ErrorDetails); }
        }
    }

    /// <summary>
    /// Rate from API
    /// </summary>
    public class TaxJarRate
    {
        #region International attributes

        [JsonProperty(PropertyName = "country")]
        public string CountryCode { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string CountryName { get; set; }

        [JsonProperty(PropertyName = "standard_rate")]
        public string StandardRate { get; set; }

        [JsonProperty(PropertyName = "reduced_rate")]
        public string ReducedRate { get; set; }

        [JsonProperty(PropertyName = "super_reduced_rate")]
        public string SuperReducedRate { get; set; }

        [JsonProperty(PropertyName = "parking_rate")]
        public string ParkingRate { get; set; }

        [JsonProperty(PropertyName = "distance_sale_threshold")]
        public string DistanceSaleThreshold { get; set; }

        [JsonProperty(PropertyName = "freight_taxable")]
        public bool FreightTaxable { get; set; }
        
        #endregion

        #region US/Canada attributes

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "county")]
        public string County { get; set; }

        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        [JsonProperty(PropertyName = "zip")]
        public string ZipCode { get; set; }

        [JsonProperty(PropertyName = "state_rate")]
        public string StateRate { get; set; }

        [JsonProperty(PropertyName = "county_rate")]
        public string CountyRate { get; set; }

        [JsonProperty(PropertyName = "city_rate")]
        public string CityRate { get; set; }

        [JsonProperty(PropertyName = "combined_district_rate")]
        public string CombinedDistrictRate { get; set; }

        [JsonProperty(PropertyName = "combined_rate")]
        public string CombinedRate { get; set; }

        #endregion

        /// <summary>
        /// Returns true for USA or Canada rates
        /// </summary>
        public bool IsUsCanada
        {
            get { return string.IsNullOrEmpty(CountryName); }
        }

        /// <summary>
        /// Tax rate
        /// </summary>
        public decimal TaxRate
        {
            get
            {
                decimal rate;
                if (IsUsCanada)
                    decimal.TryParse(CombinedRate, out rate);
                else
                    decimal.TryParse(StandardRate, out rate);

                return rate;
            }
        }
    }
}
