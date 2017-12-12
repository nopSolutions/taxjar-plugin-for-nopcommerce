using System.Collections.Generic;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Tax;
using Taxjar;

namespace Nop.Plugin.Tax.TaxJar
{
    /// <summary>
    /// TaxJar tax provider
    /// </summary>
    public class TaxJarProvider : BasePlugin, ITaxProvider
    {
        /// <summary>
        /// {0} - Zip postal code
        /// {1} - Country id
        /// {2} - City
        /// </summary>
        private const string TAXRATE_KEY = "Nop.plugins.tax.taxjar.taxratebyaddress-{0}-{1}-{2}";

        #region Fields

        private readonly ICacheManager _cacheManager;
        private readonly ISettingService _settingService;
        private readonly TaxJarSettings _taxJarSettings;
        private readonly IWebHelper _webHelper;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;

        #endregion

        #region Ctor

        public TaxJarProvider(ICacheManager cacheManager,
            ISettingService settingService,
            TaxJarSettings taxJarSettings,
            IWebHelper webHelper,
            ICountryService countryService,
            IStateProvinceService stateProvinceService)
        {           
            this._cacheManager = cacheManager;
            this._settingService = settingService;
            this._taxJarSettings = taxJarSettings;
            this._webHelper = webHelper;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets tax rate
        /// </summary>
        /// <param name="calculateTaxRequest">Tax calculation request</param>
        /// <returns>Tax</returns>
        public CalculateTaxResult GetTaxRate(CalculateTaxRequest calculateTaxRequest)
        {
            if (calculateTaxRequest.Address == null)
                return new CalculateTaxResult { Errors = new List<string> { "Address is not set" } };

            var cacheKey = string.Format(TAXRATE_KEY, 
                !string.IsNullOrEmpty(calculateTaxRequest.Address.ZipPostalCode) ? calculateTaxRequest.Address.ZipPostalCode : string.Empty,
                calculateTaxRequest.Address.Country != null ? calculateTaxRequest.Address.Country.Id : 0,
                !string.IsNullOrEmpty(calculateTaxRequest.Address.City) ? calculateTaxRequest.Address.City : string.Empty);

            // we don't use standard way _cacheManager.Get() due the need write errors to CalculateTaxResult
            if (_cacheManager.IsSet(cacheKey))
                return new CalculateTaxResult { TaxRate = _cacheManager.Get<decimal>(cacheKey) };

            var taxJarManager = new TaxJarManager { Api = _taxJarSettings.ApiToken, CountryService = _countryService, StateProvinceService = _stateProvinceService };
            try
            {
                var result = taxJarManager.GetTaxRate(_taxJarSettings, calculateTaxRequest.Price, calculateTaxRequest.Address);

                _cacheManager.Set(cacheKey, result.GetRate() * 100, 60);
                return new CalculateTaxResult { TaxRate = result.GetRate() * 100 };
            }
            catch (TaxjarException e)
            {
                return new CalculateTaxResult {Errors = new List<string> {e.Message}};
            }
            catch (Newtonsoft.Json.JsonSerializationException e)
            {
                return new CalculateTaxResult { Errors = new List<string> { e.Message } };
            }
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/TaxTaxJar/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new TaxJarSettings
            {
                UseExtendedMethod = true,
                UseStandartRate = true
            });

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.ApiToken", "API token");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.ApiToken.Hint", "Specify TaxJar API Token.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromCountry", "From country");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromCountry.Hint", "Specify the country where the order shipped from.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromState", "From state");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromState.Hint", "Specify the state where the order shipped from");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromZip", "From zip");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromZip.Hint", "Specify the postal code where the order shipped from (5-Digit ZIP or ZIP+4).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.UseExtendedMethod", "Use extended tax rate");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.UseExtendedMethod.Hint", "This method is more precise then standard method, but it requires to specify the location the order is sent from.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.UseStandartRate", "Use standard tax rate");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.UseStandartRate.Hint", "Check to return a standard tax rate if the extended method returns zero or error.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Testing", "Test tax calculation");

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<TaxJarSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.ApiToken");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.ApiToken.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromCountry");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromCountry.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromState");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromState.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromZip");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.FromZip.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.UseExtendedMethod");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.UseExtendedMethod.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.UseStandartRate");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Fields.UseStandartRate.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Testing");

            base.Uninstall();
        }

        #endregion
    }
}
