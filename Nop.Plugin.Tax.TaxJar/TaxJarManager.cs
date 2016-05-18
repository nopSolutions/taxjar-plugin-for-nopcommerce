using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace Nop.Plugin.Tax.TaxJar
{
    /// <summary>
    /// The TaxJar manager
    /// </summary>
    public class TaxJarManager
    {
        /// <summary>
        /// API token
        /// </summary>
        public string API { get; set; }

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

            var request = (HttpWebRequest)WebRequest.Create(string.Format("https://api.taxjar.com/v2/rates/{0}?{1}", zip, parameters.ToString()));
            request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Bearer {0}", API));
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
        public TaxJarRate Rate { get; set; }
        public string Error { get; set; }
        public string Detail { get; set; }
        public string Status { get; set; }

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
        public string Message
        {
            get { return IsSuccess ? string.Empty : string.Format("{0} - {1} ({2})", Status, Error, Detail); }
        }
    }

    /// <summary>
    /// Rate from API
    /// </summary>
    public class TaxJarRate
    {
        // international attributes
        public string Country { get; set; }
        public string Name { get; set; }
        public string Standard_Rate { get; set; }
        public string Reduced_Rate { get; set; }
        public string Super_Reduced_Rate { get; set; }
        public string Parking_Rate { get; set; }
        public string Distance_Sale_Threshold { get; set; }
        public bool Freight_Taxable { get; set; }

        // US/Canada attributes
        public string State { get; set; }
        public string County { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public string State_Rate { get; set; }
        public string County_Rate { get; set; }
        public string City_Rate { get; set; }
        public string Combined_District_rate { get; set; }
        public string Combined_Rate { get; set; }

        /// <summary>
        /// Returns true for USA or Canada rates
        /// </summary>
        public bool IsUsCanada
        {
            get { return string.IsNullOrEmpty(Name); }
        }

        /// <summary>
        /// Tax rate
        /// </summary>
        public decimal TaxRate
        {
            get
            {
                decimal rate = 0;
                if (IsUsCanada)
                    decimal.TryParse(Combined_Rate, out rate);
                else
                    decimal.TryParse(Standard_Rate, out rate);
                return rate;
            }
        }
    }
}
