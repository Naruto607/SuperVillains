namespace SuperVillains
{
    /// <summary>
    /// Class that handles object validity check
    /// </summary>
    internal static class ValidityCheck
    {
        /// <summary>
        /// Checks if the current object is still exist
        /// </summary>
        /// <param name="ped">GTA Ped</param>
        /// <returns>True if exist, otherwise false</returns>
        internal static bool isObjectValid(this LCPD_First_Response.LCPDFR.API.LPed ped)
        {
            return ped != null && ped.Exists();
        }

        /// <summary>
        /// Checks if the current object is still exist
        /// </summary>
        /// <param name="ped">LCPDFR Ped</param>
        /// <returns>True if exist, otherwise false</returns>
        internal static bool isObjectValid(this GTA.Ped ped)
        {
            return ped != null && ped.Exists();
        }

        /// <summary>
        /// Checks if the current object is still exist
        /// </summary>
        /// <param name="veh">LCPDFR Vehicle</param>
        /// <returns>True if exist, otherwise false</returns>
        internal static bool isObjectValid(this LCPD_First_Response.LCPDFR.API.LVehicle veh)
        {
            return veh != null && veh.Exists();
        }

        /// <summary>
        /// Checks if the current object is still exist
        /// </summary>
        /// <param name="veh">GTA Vehicle</param>
        /// <returns>True if exist, otherwise false</returns>
        internal static bool isObjectValid(this GTA.Vehicle veh)
        {
            return veh != null && veh.Exists();
        }

        /// <summary>
        /// Checks if the current object is still exist
        /// </summary>
        /// <param name="obj">GTA Object</param>
        /// <returns>True if exist, otherwise false</returns>
        internal static bool isObjectValid(this GTA.@base.Object obj)
        {
            return obj != null && obj.Exists();
        }

        /// <summary>
        /// Checks if the current object is still exist
        /// </summary>
        /// <param name="obj">System Object?</param>
        /// <returns>True if exist, otherwise false</returns>
        internal static bool isObjectValid(this System.Object obj)
        {
            return obj != null;
        }

        /// <summary>
        /// Checks if the current object is still exist
        /// </summary>
        /// <param name="blip">GTA Blip</param>
        /// <returns>True if exist, otherwise false</returns>
        internal static bool isObjectValid(this GTA.Blip blip)
        {
            return blip != null && blip.Exists();
        }
    }
}
