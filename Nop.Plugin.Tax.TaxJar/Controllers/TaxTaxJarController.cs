using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Tax.TaxJar.Models;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Taxjar;

namespace Nop.Plugin.Tax.TaxJar.Controllers
{

    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class TaxTaxJarController : BasePluginController
    {
        #region Fields

        private readonly ICountryService _countryService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly TaxJarSettings _taxJarSettings;
        private readonly IPermissionService _permissionService;
        private readonly IStateProvinceService _stateProvinceService;

        #endregion

        #region Ctor

        public TaxTaxJarController(ICountryService countryService,
            ILocalizationService localizationService,
            ISettingService settingService,
            TaxJarSettings taxJarSettings,
            IPermissionService permissionService,
            IStateProvinceService stateProvinceService)
        {
            this._countryService = countryService;
            this._localizationService = localizationService;
            this._settingService = settingService;
            this._taxJarSettings = taxJarSettings;
            this._permissionService = permissionService;
            this._stateProvinceService = stateProvinceService;
        }

        #endregion

        #region Utilities

        [NonAction]
        protected IList<SelectListItem> GetAvailableCountries()
        {
            var availableCountries = _countryService.GetAllCountries(showHidden: true).Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }).ToList();
            availableCountries.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Address.SelectCountry"), Value = "0" });

            return availableCountries;
        }

        [NonAction]
        private void PrepareAvailableStates(TaxTaxJarModel model)
        {
            var selectedCountry = _countryService.GetCountryById(_taxJarSettings.FromCountry);
            var selectedState = _stateProvinceService.GetStateProvinceById(_taxJarSettings.FromState);
            var states = selectedCountry != null
                ? _stateProvinceService.GetStateProvincesByCountryId(selectedCountry.Id, showHidden: true).ToList()
                : new List<StateProvince>();
            model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem
                {
                    Text = s.Name,
                    Value = s.Id.ToString(),
                    Selected = selectedState != null && s.Id == selectedState.Id
                });
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            var model = new TaxTaxJarModel
            {
                ApiToken = _taxJarSettings.ApiToken,
                TestAddress = {AvailableCountries = GetAvailableCountries()},

                FromCountry = _taxJarSettings.FromCountry,
                FromZip = _taxJarSettings.FromZip,
                FromState = _taxJarSettings.FromState,
                UseExtendedMethod = _taxJarSettings.UseExtendedMethod,
                UseStandartRate = _taxJarSettings.UseStandartRate,
                AvailableCountries = GetAvailableCountries()
            };

            //states
            PrepareAvailableStates(model);

            return View("~/Plugins/Tax.TaxJar/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        public IActionResult Configure(TaxTaxJarModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            _taxJarSettings.ApiToken = model.ApiToken;

            _taxJarSettings.FromCountry = model.FromCountry;
            _taxJarSettings.FromZip = model.FromZip;
            _taxJarSettings.FromState = model.FromState;
            _taxJarSettings.UseExtendedMethod = model.UseExtendedMethod;
            _taxJarSettings.UseStandartRate = model.UseStandartRate;
            model.AvailableCountries = GetAvailableCountries();

            //states
            PrepareAvailableStates(model);

            _settingService.SaveSetting(_taxJarSettings);

            model.TestAddress.AvailableCountries = GetAvailableCountries();
            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return View("~/Plugins/Tax.TaxJar/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("test")]
        public IActionResult TestRequest(TaxTaxJarModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            var testResult = new StringBuilder();

            var taxJarManager = new TaxJarManager { Api = _taxJarSettings.ApiToken, CountryService = _countryService, StateProvinceService = _stateProvinceService};
            try
            {
                var result = taxJarManager.GetTestTaxRate(_taxJarSettings, model.TestAddress);

                if (string.IsNullOrEmpty(result.Country) || result.Country.Equals("CA", StringComparison.InvariantCultureIgnoreCase))
                {
                    testResult.AppendFormat("State: {0}<br />", result.State);
                    testResult.AppendFormat("County: {0}<br />", result.County);
                    testResult.AppendFormat("City: {0}<br />", result.City);
                    testResult.AppendFormat("State rate: {0}<br />", result.StateRate);
                    testResult.AppendFormat("County rate: {0}<br />", result.CountyRate);
                    testResult.AppendFormat("City rate: {0}<br />", result.CityRate);
                    testResult.AppendFormat("Combined district rate: {0}<br />", result.CombinedDistrictRate);
                    testResult.AppendFormat("<b>Total rate: {0}<b/>", result.CombinedRate);
                }
                else
                {
                    testResult.AppendFormat("Country: {0}<br />", result.Name);
                    testResult.AppendFormat("Reduced rate: {0}<br />", result.ReducedRate);
                    testResult.AppendFormat("Super reduced rate: {0}<br />", result.SuperReducedRate);
                    testResult.AppendFormat("Parking rate: {0}<br />", result.ParkingRate);
                    testResult.AppendFormat("<b>Standard rate: {0}<b/>", result.StandardRate);
                }
            }
            catch (TaxjarException e)
            {
                testResult.Append(e.Message);
            }
            catch (Newtonsoft.Json.JsonSerializationException e)
            {
                testResult.Append(e.Message);
            }

            model.TestingResult = testResult.ToString();
            model.TestAddress.AvailableCountries = GetAvailableCountries();
            model.AvailableCountries = GetAvailableCountries();

            //states
            PrepareAvailableStates(model);

            return View("~/Plugins/Tax.TaxJar/Views/Configure.cshtml", model);
        }

        #endregion
    }
}