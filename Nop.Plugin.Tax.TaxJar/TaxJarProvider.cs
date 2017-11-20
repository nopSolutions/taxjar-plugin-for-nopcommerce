using System.Collections.Generic;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Tax;

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

        #endregion

        #region Ctor

        public TaxJarProvider(ICacheManager cacheManager,
            ISettingService settingService,
            TaxJarSettings taxJarSettings,
            IWebHelper webHelper)
        {           
            this._cacheManager = cacheManager;
            this._settingService = settingService;
            this._taxJarSettings = taxJarSettings;
            this._webHelper = webHelper;
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

            var taxJarManager = new TaxJarManager { Api = _taxJarSettings.ApiToken };
            var result = taxJarManager.GetTaxRate(
                calculateTaxRequest.Address.Country != null ? calculateTaxRequest.Address.Country.TwoLetterIsoCode : null,
                calculateTaxRequest.Address.City, 
                calculateTaxRequest.Address.Address1, 
                calculateTaxRequest.Address.ZipPostalCode);
            if (!result.IsSuccess)
                return new CalculateTaxResult { Errors = new List<string> { result.ErrorMessage } };

            _cacheManager.Set(cacheKey, result.Rate.TaxRate * 100, 60);
            return new CalculateTaxResult { TaxRate = result.Rate.TaxRate * 100 };
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
            _settingService.SaveSetting(new TaxJarSettings());

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.ApiToken", "API token");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.TaxJar.Fields.ApiToken.Hint", "Specify TaxJar API Token.");
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
            this.DeletePluginLocaleResource("Plugins.Tax.TaxJar.Testing");

            base.Uninstall();
        }

        #endregion
    }
}
