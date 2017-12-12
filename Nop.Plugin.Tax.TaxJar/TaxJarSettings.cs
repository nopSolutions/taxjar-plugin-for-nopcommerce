using Nop.Core.Configuration;

namespace Nop.Plugin.Tax.TaxJar
{
    public class TaxJarSettings : ISettings
    {
        /// <summary>
        /// Gets or sets TaxJar API Token
        /// </summary>
        public string ApiToken { get; set; }

        /// <summary>
        /// Specifies whether to return a standard tax rate if the extended method one returns zero or has been executed with an error
        /// </summary>
        public bool UseStandartRate { get; set; }

        /// <summary>
        /// Specifies whether to use extended method of get tax rate
        /// </summary>
        public bool UseExtendedMethod { get; set; }

        /// <summary>
        /// ID of the country where the order shipped from
        /// </summary>
        public int FromCountry { get; set; }

        /// <summary>
        /// ID of the state where the order shipped from
        /// </summary>
        public int FromState { get; set; }

        /// <summary>
        /// Postal code where the order shipped from (5-Digit ZIP or ZIP+4)
        /// </summary>
        public string FromZip { get; set; }
    }
}