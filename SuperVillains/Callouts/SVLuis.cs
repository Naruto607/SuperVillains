using GTA;
using GTA.Native;
using LCPD_First_Response.Engine;
using LCPD_First_Response.Engine.Scripting.Entities;
using LCPD_First_Response.LCPDFR.API;
using LCPD_First_Response.LCPDFR.Callouts;
using SuperVillains.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperVillains.Callouts
{
    /// <summary>
    /// Callout that calls the TBoGT protagonist: Luis F. Lopez, causing civil disturbance.
    /// </summary>
    [CalloutInfo("SVLuis", ECalloutProbability.Low)]
    internal class SVLuis : Callout
    {
        /// <summary>
        /// Vehicle models that can be used.
        /// </summary>
        private string[] vehicleModels = new string[] { "ADMIRAL", "BANSHEE", "BLISTA", "FUTO", "INGOT", "FACTION", "LANDSTALKER", "ORACLE", "SENTINEL", "PCJ", "FELTZER" };

        /// <summary>
        /// The pursuit.
        /// </summary>
        private LHandle pursuit;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private LVehicle vehicle;

        /// <summary>
        /// The position at which the vehicles are spawned
        /// </summary>
        private Vector3 spawnPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="SVLuis"/> class.
        /// </summary>
        public SVLuis()
        {
            // Get a good position
            this.spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(400.0f));

            while (this.spawnPosition.DistanceTo(LPlayer.LocalPlayer.Ped.Position) < 100.0f)
            {
                this.spawnPosition = World.GetNextPositionOnStreet(LPlayer.LocalPlayer.Ped.Position.Around(400.0f));
            }

            if (this.spawnPosition == Vector3.Zero)
            {
                // It obviously failed, set the position to be the player's position and the distance check will catch it.
                this.spawnPosition = LPlayer.LocalPlayer.Ped.Position;
            }

            // Show user where the pursuit is about to happen
            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPosition, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPosition);

            // Set up message
            this.CalloutMessage = string.Format(Resources.CALLOUT_SV_LUIS_MESSAGE, Functions.GetAreaStringFromPosition(this.spawnPosition));
            int rand = Common.GetRandomValue(0, 3);
            switch (rand)
            {
                case 0:
                    Functions.PlaySoundUsingPosition("INS_THIS_IS_CONTROL_I_NEED_ASSISTANCE_FOR CRIM_A_CIVILIAN_CAUSING_TROUBLE IN_OR_ON_POSITION", this.spawnPosition); break;
                case 1:
                    Functions.PlaySoundUsingPosition("ALL_UNITS_ALL_UNITS ASSISTANCE_REQUIRED IN_OR_ON_POSITION FOR CRIM_A_CIVIL_DISTURBANCE", this.spawnPosition); break;
                case 2:
                    string audioMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Officers);
                    string crimeMessage = "CRIM_A_CRIMINAL_IN_A_STOLEN_VEHICLE";
                    if (Common.GetRandomBool(0, 2, 1))
                    {
                        crimeMessage = "CRIM_A_CRIMINAL_RESISTING_ARREST";
                    }

                    Functions.PlaySoundUsingPosition(audioMessage + crimeMessage + " IN_OR_ON_POSITION", this.spawnPosition); break;
            }
        }

        /// <summary>
        /// The Luis ped
        /// </summary>
        private LPed luis;

        /// <summary>
        /// Called when the callout has been accepted. Call base to set state to Running.
        /// </summary>
        /// <returns>
        /// True if callout was setup properly, false if it failed. Calls <see cref="End"/> when failed.
        /// </returns>
        public override bool OnCalloutAccepted()
        {
            bool isReady = base.OnCalloutAccepted();

            // Create pursuit instance
            this.pursuit = Functions.CreatePursuit();

            // Create vehicle
            try
            {
                this.vehicle = new LVehicle(World.GetNextPositionOnStreet(this.spawnPosition), Common.GetRandomCollectionValue<string>(this.vehicleModels));
                if (ValidityCheck.isObjectValid(this.vehicle))
                {
                    // Ensure vehicle is freed on end
                    Functions.AddToScriptDeletionList(this.vehicle, this);
                    this.vehicle.PlaceOnNextStreetProperly();

                    // Create Luis
                    luis = vehicle.CreatePedOnSeat(VehicleSeat.Driver, new CModel(new Model("IG_LUIS")), RelationshipGroup.Criminal);
                    luis.PersonaData = new PersonaData(new DateTime(1983, 8, 17, 0, 25, 0, DateTimeKind.Utc), 13, "Luis Fernando", "Lopez", true, 8, true);
                    luis.Money = 500;

                    // Make ignore all events
                    luis.BlockPermanentEvents = true;
                    luis.Task.AlwaysKeepTask = true;

                    // Give specified weapons
                    luis.Weapons.RemoveAll();
                    luis.Weapons.DesertEagle.Ammo = 999;
                    luis.DefaultWeapon = Weapon.Handgun_DesertEagle;
                    luis.Weapons.AssaultRifle_M4.Ammo = 300;
                    luis.Weapons.AssaultRifle_M4.Select();
                    luis.ComplianceChance = Common.GetRandomValue(40, 80);

                    // Make default of the ped's component variation
                    Function.Call("SET_CHAR_DEFAULT_COMPONENT_VARIATION", new Parameter[] { luis.GPed });

                    // Add to deletion list and to pursuit
                    Functions.AddToScriptDeletionList(luis, this);
                    Functions.AddPedToPursuit(this.pursuit, luis);
                }

                // Create FIB Squad
                LVehicle fibCar = new LVehicle(World.GetNextPositionOnStreet(this.vehicle.Position), "FBI");
                LPed[] fibPersonnel = new LPed[4]
                {
                    fibCar.CreatePedOnSeat(VehicleSeat.Driver, new CModel((UInt32)3295460374), RelationshipGroup.Cop),
                    fibCar.CreatePedOnSeat(VehicleSeat.RightFront, new CModel((UInt32)3295460374), RelationshipGroup.Cop),
                    fibCar.CreatePedOnSeat(VehicleSeat.LeftRear, new CModel((UInt32)3295460374), RelationshipGroup.Cop),
                    fibCar.CreatePedOnSeat(VehicleSeat.RightRear, new CModel((UInt32)3295460374), RelationshipGroup.Cop),
                };

                if (ValidityCheck.isObjectValid(fibCar))
                {
                    Functions.AddToScriptDeletionList(fibCar, this);
                    fibCar.PlaceOnNextStreetProperly();
                    fibCar.SirenActive = true;

                    foreach (LPed fib in fibPersonnel)
                    {
                        if (fib.isObjectValid()) // derived from ValidityCheck - greetings to LtFlash
                        {
                            Functions.AddToScriptDeletionList(fib, this);
                            fib.Weapons.RemoveAll();
                            fib.Weapons.FromType(Weapon.Handgun_Glock).Ammo = 999;
                            fib.Weapons.FromType(Weapon.SMG_MP5).Ammo = 999;
                            fib.Weapons.FromType(Weapon.Rifle_M4).Ammo = 999;
                            fib.Weapons.Select(Weapon.Rifle_M4);
                            fib.DefaultWeapon = Weapon.Rifle_M4;
                        }
                    }
                }

                // Since we want other cops to join, set as called in already and also active it for player
                Functions.SetPursuitCalledIn(this.pursuit, true);
                Functions.SetPursuitIsActiveForPlayer(this.pursuit, true);

                // Show message to the player
                Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_ROBBERY_CATCH_UP"), 25000);
                isReady = true;
            }
            catch (Exception ex) { Log.Error("OnCalloutAccepted: Cannot create Pursuit instance: " + ex, this); isReady = false; }

            return isReady;
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (ValidityCheck.isObjectValid(luis) && luis.HasBeenArrested)
            {
                this.SetCalloutFinished(true, true, true);
                this.End();
            }

            // End this script is pursuit is no longer running, e.g. because all suspects are dead
            if (!Functions.IsPursuitStillRunning(this.pursuit))
            {
                this.SetCalloutFinished(true, true, true);
                this.End();
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the calloutmanager to shutdown the callout or can be called by the 
        /// callout itself to execute the cleanup code. Call base to set state to None.
        /// </summary>
        public override void End()
        {
            base.End();
            int salary = 2500;

            // End pursuit if still running
            if (this.pursuit != null)
            {
                Functions.ForceEndPursuit(this.pursuit);
            }

            Functions.PrintText(string.Format(Resources.CALLOUT_SV_BONUS_SALARY, salary), 8000);
            LPlayer.LocalPlayer.Money += salary;
        }

        /// <summary>
        /// Called when a ped assigned to the current script has left the script due to a more important action, such as being arrested by the player.
        /// This is invoked right before control is granted to the new script, so perform all necessary freeing actions right here.
        /// </summary>
        /// <param name="ped">The ped</param>
        public override void PedLeftScript(LPed ped)
        {
            base.PedLeftScript(ped);

            // Free ped
            Functions.RemoveFromDeletionList(ped, this);
            Functions.SetPedIsOwnedByScript(ped, this, false);
        }
    }
}
