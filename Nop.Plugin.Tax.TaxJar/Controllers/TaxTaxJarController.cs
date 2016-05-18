using System.Linq;
using System.Text;
using System.Web.Mvc;
using Nop.Plugin.Tax.TaxJar.Models;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Tax.TaxJar.Controllers
{
    [AdminAuthorize]
    public class TaxTaxJarController : BasePluginController
    {
        private readonly ICountryService _countryService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly TaxJarSettings _taxJarSettings;

        public TaxTaxJarController(ICountryService countryService,
            ILocalizationService localizationService,
            ISettingService settingService,
            TaxJarSettings taxJarSettings)
        {
            this._countryService = countryService;
            this._localizationService = localizationService;
            this._settingService = settingService;
            this._taxJarSettings = taxJarSettings;
        }

        [NonAction]
        protected void PrepareAddress(TestAddressModel model)
        {
            model.AvailableCountries = _countryService.GetAllCountries(showHidden: true)
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }).ToList();
            model.AvailableCountries.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Address.SelectCountry"), Value = "0" });
        }

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new TaxTaxJarModel { ApiToken = _taxJarSettings.ApiToken};
            PrepareAddress(model.TestAddress);

            return View("~/Plugins/Tax.TaxJar/Views/TaxTaxJar/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        public ActionResult Configure(TaxTaxJarModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            _taxJarSettings.ApiToken = model.ApiToken;
            _settingService.SaveSetting(_taxJarSettings);

            PrepareAddress(model.TestAddress);
            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return View("~/Plugins/Tax.TaxJar/Views/TaxTaxJar/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("test")]
        public ActionResult TestRequest(TaxTaxJarModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            var testResult = new StringBuilder();
            var country = _countryService.GetCountryById(model.TestAddress.CountryId);
            var taxJarManager = new TaxJarManager { API = _taxJarSettings.ApiToken };
            var result = taxJarManager.GetTaxRate(
                country != null ? country.TwoLetterIsoCode : null, 
                model.TestAddress.City, 
                null, 
                model.TestAddress.Zip);
            if (result.IsSuccess)
            {
                if (result.Rate.IsUsCanada)
                {
                    testResult.AppendFormat("State: {0}<br />", result.Rate.State);
                    testResult.AppendFormat("County: {0}<br />", result.Rate.County);
                    testResult.AppendFormat("City: {0}<br />", result.Rate.City);
                    testResult.AppendFormat("State rate: {0}<br />", result.Rate.State_Rate);
                    testResult.AppendFormat("County rate: {0}<br />", result.Rate.County_Rate);
                    testResult.AppendFormat("City rate: {0}<br />", result.Rate.City_Rate);
                    testResult.AppendFormat("Combined district rate: {0}<br />", result.Rate.Combined_District_rate);
                    testResult.AppendFormat("<b>Total rate: {0}<b/>", result.Rate.Combined_Rate);
                }
                else
                {
                    testResult.AppendFormat("Country: {0}<br />", result.Rate.Name);
                    testResult.AppendFormat("Reduced rate: {0}<br />", result.Rate.Reduced_Rate);
                    testResult.AppendFormat("Super reduced rate: {0}<br />", result.Rate.Super_Reduced_Rate);
                    testResult.AppendFormat("Parking rate: {0}<br />", result.Rate.Parking_Rate);
                    testResult.AppendFormat("<b>Standard rate: {0}<b/>", result.Rate.Standard_Rate);
                }
            }
            else
                testResult.Append(result.Message);

            model.TestingResult = testResult.ToString();
            PrepareAddress(model.TestAddress);

            return View("~/Plugins/Tax.TaxJar/Views/TaxTaxJar/Configure.cshtml", model);
        }
    }
}