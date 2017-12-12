using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Tax.TaxJar.Models
{
    public class TaxTaxJarModel
    {
        public TaxTaxJarModel()
        {
            TestAddress = new TestAddressModel();
            AvailableStates = new List<SelectListItem>();
            AvailableCountries = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Tax.TaxJar.Fields.ApiToken")]
        public string ApiToken { get; set; }

        public TestAddressModel TestAddress { get; set; }
        public string TestingResult { get; set; }

        [NopResourceDisplayName("Plugins.Tax.TaxJar.Fields.UseStandartRate")]
        public bool UseStandartRate { get; set; }

        [NopResourceDisplayName("Plugins.Tax.TaxJar.Fields.UseExtendedMethod")]
        public bool UseExtendedMethod { get; set; }

        [NopResourceDisplayName("Plugins.Tax.TaxJar.Fields.FromCountry")]
        public int FromCountry { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }

        [NopResourceDisplayName("Plugins.Tax.TaxJar.Fields.FromState")]
        public int FromState { get; set; }
        public IList<SelectListItem> AvailableStates { get; set; }
       
        [NopResourceDisplayName("Plugins.Tax.TaxJar.Fields.FromZip")]
        public string FromZip { get; set; }
    }

    public class TestAddressModel
    {
        public TestAddressModel()
        {
            AvailableCountries = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Admin.Address.Fields.Country")]
        public int CountryId { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }

        [NopResourceDisplayName("Admin.Address.Fields.City")]
        public string City { get; set; }

        [NopResourceDisplayName("Admin.Address.Fields.ZipPostalCode")]
        public string Zip { get; set; }
    }
}