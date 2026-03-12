namespace EnvAutoUpdater.src.Models
{
    public class Config
    {
        /// <summary>
        /// Gets or sets the collection of services that are scheduled to be updated.
        /// </summary>
        /// <remarks>If the property is null, no services are currently marked for update. Modifying this
        /// collection affects which services will be processed during the update operation.</remarks>
        public List<ServiceToUpdate>? ServicesToUpdate { get; set; }
        /// <summary>
        /// Gets or sets the interval, in seconds, at which checks are performed.
        /// </summary>
        public int? CheckInterval { get; set; }

        public override string ToString()
        {
            string servicesInfo = ServicesToUpdate != null
                ? string.Join("\n", ServicesToUpdate.Select(s => s.ServiceName))
                : "No services configured";
            return $"Config: ServicesToUpdate=[{servicesInfo}]\nCheckInterval={CheckInterval}";
        }
    }
}
