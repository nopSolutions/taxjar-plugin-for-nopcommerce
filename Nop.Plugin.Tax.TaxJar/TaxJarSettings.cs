using Nop.Core.Configuration;

namespace Nop.Plugin.Tax.TaxJar
{
    public class TaxJarSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a TaxJar API Token
        /// </summary>
        public string ApiToken { get; set; }
    }
}