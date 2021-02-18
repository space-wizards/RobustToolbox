namespace Robust.Server.Maps
{
    public sealed class MapLoadOptions
    {
        /// <summary>
        ///     If true, UID components will be created for loaded entities
        ///     to maintain consistency upon subsequent savings.
        /// </summary>
        public bool StoreMapUids { get; set; }
    }
}
