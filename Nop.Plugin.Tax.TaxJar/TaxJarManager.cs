using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Plugin.Tax.TaxJar.Models;
using Nop.Services.Directory;
using Taxjar;

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
        public string Api { get; set; }

        /// <summary>
        /// Country service
        /// </summary>
        public ICountryService CountryService { get; set; }

        /// <summary>
        /// Country service
        /// </summary>
        public IStateProvinceService StateProvinceService { get; set; }

        /// <summary>
        /// Get test tax rate from TaxJar API
        /// </summary>
        /// <param name="settings">TaxJar settings</param>
        /// <param name="model">TestAddressModel</param>
        /// <returns></returns>
        public Rate GetTestTaxRate(TaxJarSettings settings, TestAddressModel model)
        {
            var address=new Address
            {
                ZipPostalCode = model.Zip,
                Country = CountryService.GetCountryById(model.CountryId),
                City = model.City
            };

            return GetTaxRate(settings, 100, address);
        }

        /// <summary>
        /// Get tax rate from TaxJar API
        /// </summary>
        /// <param name="settings">TaxJar settings</param>
        /// <param name="price">Price</param>
        /// <param name="address">Address where the order shipped to</param>
        /// <returns>Response from API</returns>
        public Rate GetTaxRate(TaxJarSettings settings, decimal price, Address address)
        {
            var client = new TaxjarApi(Api);
            var rez = new Rate
            {
                StandardRate = 0,
                CombinedRate = 0,
                City = address.City,
                Country = address.Country?.TwoLetterIsoCode??string.Empty,
                State = address.StateProvince?.Abbreviation ?? string.Empty,
                Zip = address.ZipPostalCode
            };

            if (settings.UseExtendedMethod)
            {
                var countryTwoLetterIsoCode = CountryService?.GetCountryById(settings.FromCountry)?.TwoLetterIsoCode;
                var stateTwoLetterIsoCode = CommonHelper.EnsureMaximumLength(StateProvinceService?.GetStateProvinceById(settings.FromState)?.Abbreviation, 2);

                try
                {
                    var tax = client.TaxForOrder(new
                    {
                        from_country = countryTwoLetterIsoCode,
                        from_zip = settings.FromZip,
                        from_state = stateTwoLetterIsoCode,
                        to_country = address.Country?.TwoLetterIsoCode ?? string.Empty,
                        to_zip = address.ZipPostalCode,
                        to_state = address.StateProvince?.Abbreviation ?? string.Empty,
                        amount = price,
                        shipping = 0
                    });

                    rez.CombinedRate = rez.StandardRate = tax.Rate;
                }
                catch (TaxjarException)
                {
                    if (!settings.UseStandartRate)
                        throw;
                }
            }

            if(rez.CombinedRate == 0 && (!settings.UseExtendedMethod || settings.UseStandartRate))
                rez = client.RatesForLocation(address.ZipPostalCode ?? string.Empty, new
                {
                    city = address.City ?? string.Empty,
                    country = address.Country.TwoLetterIsoCode ?? string.Empty
                });

            return rez;
        }
    }

    internal static class RateExt
    {
        public static decimal GetRate(this Rate rate)
        {
            return rate.CombinedRate.HasValue && rate.CountryRate != 0
                ? rate.CombinedRate.Value
                : (rate.StandardRate ?? 0);
        }
    }
}
