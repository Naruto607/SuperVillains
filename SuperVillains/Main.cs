using GTA;
using LCPD_First_Response.Engine;
using LCPD_First_Response.Engine.Input;
using LCPD_First_Response.Engine.Scripting.Plugins;
using LCPD_First_Response.LCPDFR.API;
using SuperVillains.Callouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperVillains
{
    /// <summary>
    /// Supervillains Redux plugin: Main script
    /// </summary>
    [PluginInfo("Supervillains",false,true)]
    public class Main:Plugin
    {
        /// <summary>
        /// Called when the plugin has been created successfully.
        /// </summary>
        public override void Initialize()
        {
            base.RegisterConsoleCommands();
            Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;
            Log.Info("Started", this);
        }

        /// <summary>
        /// Called when player changed the on duty state.
        /// </summary>
        /// <param name="onDuty">The new on duty state.</param>
        private void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            // Allow Episodic Checking
            if (onDuty && Game.CurrentEpisode == GameEpisode.TBOGT)
            {
                Functions.RegisterCallout(typeof(SVNiko));
                Functions.RegisterCallout(typeof(SVLuis_TBOGT));
                Functions.RegisterCallout(typeof(SVJohnny));
            }
            else if (onDuty && Game.CurrentEpisode == GameEpisode.TLAD)
            {
                Functions.RegisterCallout(typeof(SVJohnny_TLAD));
                Functions.RegisterCallout(typeof(SVLuis));
                Functions.RegisterCallout(typeof(SVNiko));
            }
            else if (onDuty && Game.CurrentEpisode == GameEpisode.GTAIV)
            {
                Functions.RegisterCallout(typeof(SVLuis));
                Functions.RegisterCallout(typeof(SVJohnny));
            }

            Log.Info("Using platform: " + Game.CurrentEpisode.ToString(), this);
            Log.Info("Callouts have been assigned properly", this);
        }

        /// <summary>
        /// Called every tick to process all plugin logic.
        /// </summary>
        public override void Process()
        {
        }

        /// <summary>
        /// Called when the plugin is being disposed, e.g. because an unhandled exception occured in Process. Free all resources here!
        /// </summary>
        public override void Finally()
        {
        }

        [ConsoleCommand("StartCallout", false)]
        private void StartCallout(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count > 0)
            {
                string name = parameterCollection[0];
                Functions.StartCallout(name);
            }
            else
            {
                Game.Console.Print("StartCallout: No argument given.");
            }
        }
    }
}
