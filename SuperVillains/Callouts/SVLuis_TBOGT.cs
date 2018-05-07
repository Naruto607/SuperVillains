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
    /// Callout that calls the TBoGT protagonist: Luis F. Lopez, fleeing the scene after the Drug Wars with his friends.
    /// </summary>
    [CalloutInfo("SVLuis", ECalloutProbability.Low)]
    internal class SVLuis_TBOGT : Callout
    {
        /// <summary>
        /// Vehicle models that can be used.
        /// </summary>
        private string[] vehicleModels = new string[] { "BURRITO", "BURRITO2", "SPEEDO", "AMBULANCE", "CAVALCADE2", "AVAN" };

        /// <summary>
        /// The pursuit.
        /// </summary>
        private LHandle pursuit;

        /// <summary>
        /// The criminals.
        /// </summary>
        private LPed[] criminals;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private LVehicle vehicle;

        /// <summary>
        /// The position at which the vehicles are spawned
        /// </summary>
        private Vector3 spawnPosition;

        /// <summary>
        /// Hash value of FIB Ped model
        /// </summary>
        private static UInt32 fib_model_int = 3295460374;

        /// <summary>
        /// Initializes a new instance of the <see cref="SVLuis_TBOGT"/> class.
        /// </summary>
        public SVLuis_TBOGT()
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
            this.CalloutMessage = string.Format(Resources.CALLOUT_SV_LUIS_TBOGT_MSG, Functions.GetAreaStringFromPosition(this.spawnPosition));
            //Functions.PlaySoundUsingPosition("INS_THIS_IS_CONTROL_WE_HAVE CRIM_AN_OFFICER_IN_NEED_OF_ASSISTANCE FOR CRIM_A_CRIMINAL_FLEEING_A_CRIME_SCENE IN_OR_ON_POSITION", this.spawnPosition);
            int rand = Common.GetRandomValue(0, 2);

            switch (rand)
            {
                case 0:
                    Functions.PlaySoundUsingPosition("INS_THIS_IS_CONTROL_WE_HAVE INS_TRAFFIC_ALERT_FOR CRIM_A_CRIMINAL_FLEEING_A_CRIME_SCENE IN_OR_ON_POSITION", this.spawnPosition);
                    break;
                case 1:
                    Functions.PlaySoundUsingPosition("ALL_UNITS INS_WE_HAVE_A_REPORT_OF_ERRR CRIM_CRIMINALS_PERFORMING_DRIVEBY_ACTIVITY IN_OR_ON_POSITION", this.spawnPosition);
                    break;
            }
        }

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
                if (this.vehicle.isObjectValid())
                {
                    // Ensure vehicle is freed on end
                    Functions.AddToScriptDeletionList(this.vehicle, this);
                    this.vehicle.PlaceOnNextStreetProperly();

                    // Create suspects
                    this.criminals = new LPed[3]
                    {
                        vehicle.CreatePedOnSeat(VehicleSeat.Driver,new CModel(new Model("IG_LUIS2")),RelationshipGroup.Criminal),
                        vehicle.CreatePedOnSeat(VehicleSeat.Driver,new CModel(new Model("IG_ARMANDO")),RelationshipGroup.Criminal),
                        vehicle.CreatePedOnSeat(VehicleSeat.Driver,new CModel(new Model("IG_HENRIQUE")),RelationshipGroup.Criminal)
                    };

                    // Allow suspects to use custom Persona Data specific to protagonist characters
                    criminals[0].PersonaData = new PersonaData(new DateTime(1983, 8, 17), 13, "Luis Fernando", "Lopez", true, 8, true);

                    if (Common.GetRandomBool(0, 3, 1))
                    {
                        criminals[0].Money = 5000;
                        criminals[0].ItemsCarried = LPed.EPedItem.Drugs;
                    }
                    else criminals[0].Money = 0;

                    criminals[1].PersonaData = new PersonaData(new DateTime(1981, 6, 20), 4, "Armando", "", true, 0, true);
                    criminals[2].PersonaData = new PersonaData(new DateTime(1982, 9, 29), 4, "Henrique", "", true, 3, true);

                    for (int i = 0; i < this.criminals.Length; i++)
                    {
                        // Make ignore all events
                        this.criminals[i].BlockPermanentEvents = true;
                        this.criminals[i].Task.AlwaysKeepTask = true;

                        // Give specified weapons
                        //criminals[i].ItemsCarried = LPed.EPedItem.Weapons;
                        criminals[i].Weapons.RemoveAll();
                        criminals[i].Weapons.FromType(Weapon.TBOGT_Pistol44).Ammo = 9999;
                        criminals[i].Weapons.FromType(Weapon.TBOGT_GoldenSMG).Ammo = 320;
                        criminals[i].Weapons.FromType(Weapon.Melee_Knife).Ammo = 999;
                        criminals[i].Weapons.FromType(Weapon.TBOGT_AdvancedMG).Ammo = 400;
                        criminals[i].Weapons.Select(Weapon.TBOGT_AdvancedMG);
                        criminals[i].WillDoDrivebys = true;
                        criminals[i].CantBeDamagedByRelationshipGroup(RelationshipGroup.Criminal, base.OnCalloutAccepted());
                        criminals[i].ComplianceChance = Common.GetRandomValue(0, 75);

                        // Make default of the ped's component variation
                        Function.Call("SET_CHAR_DEFAULT_COMPONENT_VARIATION", new Parameter[] { criminals[i].GPed });

                        // Add to deletion list and to pursuit
                        Functions.AddToScriptDeletionList(this.criminals[i], this);
                        Functions.AddPedToPursuit(this.pursuit, this.criminals[i]);
                    }
                }

                // Create FIB Squad (x2)
                LVehicle fibCar = new LVehicle(World.GetNextPositionOnStreet(this.vehicle.Position), "FBI");
                LVehicle fibCar2 = new LVehicle(World.GetNextPositionOnStreet(fibCar.Position.Around((float)10)), "FBI");
                LPed[] fibPersonnel = new LPed[4]
                {
                    fibCar.CreatePedOnSeat(VehicleSeat.Driver, new CModel(fib_model_int), RelationshipGroup.Cop),
                    fibCar.CreatePedOnSeat(VehicleSeat.RightFront, new CModel(fib_model_int), RelationshipGroup.Cop),
                    fibCar.CreatePedOnSeat(VehicleSeat.LeftRear, new CModel(fib_model_int), RelationshipGroup.Cop),
                    fibCar.CreatePedOnSeat(VehicleSeat.RightRear, new CModel(fib_model_int), RelationshipGroup.Cop),
                };
                LPed[] fibPersonnel2 = new LPed[4]
                {
                    fibCar2.CreatePedOnSeat(VehicleSeat.Driver, new CModel(fib_model_int), RelationshipGroup.Cop),
                    fibCar2.CreatePedOnSeat(VehicleSeat.RightFront, new CModel(fib_model_int), RelationshipGroup.Cop),
                    fibCar2.CreatePedOnSeat(VehicleSeat.LeftRear, new CModel(fib_model_int), RelationshipGroup.Cop),
                    fibCar2.CreatePedOnSeat(VehicleSeat.RightRear, new CModel(fib_model_int), RelationshipGroup.Cop),
                };

                if (fibCar.isObjectValid() && fibCar2.isObjectValid())
                {
                    Functions.AddToScriptDeletionList(fibCar, this);
                    Functions.AddToScriptDeletionList(fibCar2, this);
                    fibCar.PlaceOnNextStreetProperly();
                    fibCar2.PlaceOnNextStreetProperly();
                    fibCar.SirenActive = true;
                    fibCar2.SirenActive = true;

                    foreach (LPed fib in fibPersonnel)
                    {
                        if (ValidityCheck.isObjectValid(fib))
                        {
                            Functions.AddToScriptDeletionList(fib, this);
                            fib.Weapons.RemoveAll();
                            fib.Weapons.FromType(Weapon.Handgun_Glock).Ammo = 999;
                            fib.Weapons.FromType(Weapon.TBOGT_AssaultSMG).Ammo = 999;
                            fib.Weapons.FromType(Weapon.Rifle_M4).Ammo = 999;
                            fib.Weapons.Select(Weapon.Rifle_M4);
                        }
                    }

                    foreach (LPed fib in fibPersonnel2)
                    {
                        if (ValidityCheck.isObjectValid(fib))
                        {
                            Functions.AddToScriptDeletionList(fib, this);
                            fib.Weapons.RemoveAll();
                            fib.Weapons.FromType(Weapon.TBOGT_Pistol44).Ammo = 999;
                            fib.Weapons.FromType(Weapon.SMG_MP5).Ammo = 999;
                            fib.Weapons.FromType(Weapon.Shotgun_Basic).Ammo = 999;
                            fib.Weapons.Select(Weapon.SMG_MP5);
                        }
                    }
                }

                // Since we want other cops to join, set as called in already and also active it for player
                Functions.SetPursuitCalledIn(this.pursuit, true);
                Functions.SetPursuitIsActiveForPlayer(this.pursuit, true);

                // Show message to the player
                Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_ROBBERY_CATCH_UP"), 25000);
                Functions.AddTextToTextwall(string.Format(Resources.TEXT_INFO_RELAY_SV_LUIS, criminals[0].PersonaData.FullName, criminals[0].PersonaData.BirthDay));
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

            // Print text message when all suspect have been arrested
            int arrestCount = this.criminals.Count(criminal => criminal.Exists() && criminal.HasBeenArrested);
            if (arrestCount == this.criminals.Length)
            {
                //Functions.PrintText("All arrested!", 5000);
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
            int salary = 8000;

            // End pursuit if still running
            if (this.pursuit != null)
            {
                Functions.ForceEndPursuit(this.pursuit);
            }

            // Issue: You will gain salary even though you lost the suspect during the chase
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
