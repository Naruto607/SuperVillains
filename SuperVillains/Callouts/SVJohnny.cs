﻿using GTA;
using GTA.Native;
using LCPD_First_Response.Engine;
using LCPD_First_Response.Engine.Timers;
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
    /// Callout that calls the TLAD protagonist: Johnny Klebitz, doing a gang riot with his crew.
    /// </summary>
    [CalloutInfo("SVJohnny", ECalloutProbability.VeryLow)]
    internal class SVJohnny:Callout
    {
        /// <summary>
        /// The blip of the position.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The criminals.
        /// </summary>
        private List<LPed> criminals;

        /// <summary>
        /// The criminal models.
        /// </summary>
        private string[] criminalModels = { "M_Y_GBIK_LO_01", "M_Y_GBIK_LO_02" };

        /// <summary>
        /// The Mafia models.
        /// </summary>
        private string[] mafiaModels = { "M_Y_GMAF_HI_01", "M_Y_GMAF_HI_02", "M_Y_GMAF_LO_01", "M_Y_GMAF_LO_02" };

        /// <summary>
        /// The Johnny ped.
        /// </summary>
        private LPed johnny;

        /// <summary>
        /// The pursuit instance used in case suspect wants to flee.
        /// </summary>
        private LHandle pursuit;

        /// <summary>
        /// The spawn point.
        /// </summary>
        private SpawnPoint spawnPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="Shootout"/> class.
        /// </summary>
        public SVJohnny()
        {
            //this.CalloutMessage = Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_MESSAGE");
            this.CalloutMessage = Resources.CALLOUT_SV_JOHNNY_MESSAGE;
        }

        /// <summary>
        /// The shootout state.
        /// </summary>
        [Flags]
        internal enum EShootoutState
        {
            /// <summary>
            /// No state.
            /// </summary>
            None = 0x0,

            /// <summary>
            /// Waiting for player.
            /// </summary>
            WaitingForPlayer = 0x1,

            /// <summary>
            /// Player is close.
            /// </summary>
            PlayerIsClose = 0x2,

            /// <summary>
            /// In combat.
            /// </summary>
            InCombat = 0x4,

            /// <summary>
            /// In combat.
            /// </summary>
            Fighting = 0x8,

            /// <summary>
            /// Arrived, but prank.
            /// </summary>
            Prank = 0x10,
        }

        /// <summary>
        /// Called just before the callout message is being displayed. Return false if callout should be aborted.
        /// </summary>
        /// <returns>
        /// True if callout can be displayed, false if it should be aborted.
        /// </returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            bool ready = base.OnBeforeCalloutDisplayed();
            this.spawnPoint = Callout.GetSpawnPointInRange(LPlayer.LocalPlayer.Ped.Position, 100, 400);

            if (this.spawnPoint == SpawnPoint.Zero)
            {
                return false;
            }

            // Show user where callout is
            this.ShowCalloutAreaBlipBeforeAccepting(this.spawnPoint.Position, 50f);
            this.AddMinimumDistanceCheck(80f, this.spawnPoint.Position);

            // Get area name
            string area = Functions.GetAreaStringFromPosition(this.spawnPoint.Position);
            //this.CalloutMessage = string.Format(Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_MESSAGE"), area);
            this.CalloutMessage = string.Format(Resources.CALLOUT_SV_JOHNNY_MESSAGE, area);

            // Play audio
            string audioMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Civilians);
            string crimeMessage = "CRIM_GANG_RELATED_VIOLENCE";
            if (Common.GetRandomBool(0, 2, 1))
            {
                crimeMessage = "CRIM_A_DOMESTIC_DISTURBANCE";
            }

            Functions.PlaySoundUsingPosition(audioMessage + crimeMessage + " IN_OR_ON_POSITION", this.spawnPoint.Position);

            return ready;
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

            this.pursuit = Functions.CreatePursuit();
            Functions.SetPursuitCopsCanJoin(this.pursuit, false);
            Functions.SetPursuitDontEnableCopBlips(this.pursuit, true);

            // Add blip
            this.blip = Functions.CreateBlipForArea(this.spawnPoint.Position, 30f);
            this.blip.Display = BlipDisplay.ArrowAndMap;
            this.blip.RouteActive = true;

            // Decide whether prank call or not
            if (Common.GetRandomBool(0, 5, 1))
            {
                Log.Debug("OnCalloutAccepted: Is prank", this);
                this.SetAsPrankCall();
            }
            else try
            {
                this.criminals = new List<LPed>();

                int random = Common.GetRandomValue(6, 13);
                for (int i = 0; i < random; i++)
                {
                    LPed criminal = new LPed(this.spawnPoint.Position, Common.GetRandomCollectionValue<string>(this.criminalModels), LPed.EPedGroup.Criminal);
                    if (ValidityCheck.isObjectValid(criminal))
                    {
                        // Ensure ped is not in a building
                        if (criminal.EnsurePedIsNotInBuilding(criminal.Position))
                        {
                            Functions.AddToScriptDeletionList(criminal, this);
                            Functions.SetPedIsOwnedByScript(criminal, this, true);
                            criminal.RelationshipGroup = RelationshipGroup.Gang_Biker2;
                            criminal.ChangeRelationship(RelationshipGroup.Gang_Biker2, Relationship.Companion);
                            criminal.ChangeRelationship(RelationshipGroup.Gang_Italian, Relationship.Hate);
                            criminal.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);
                            criminal.CantBeDamagedByRelationshipGroup(RelationshipGroup.Gang_Biker2, true);

                            // We don't want the criminal to flee yet
                            criminal.DisablePursuitAI = true;
                            criminal.EquipWeapon();

                            // Set up weapons
                            criminal.Weapons.RemoveAll();
                            criminal.Weapons.FromType(Weapon.Handgun_Glock).Ammo = 999;
                            criminal.Weapons.FromType(Weapon.Shotgun_Basic).Ammo = 120;
                            criminal.Weapons.FromType(Weapon.Melee_Knife);
                            criminal.Weapons.FromType(Weapon.Thrown_Molotov).Ammo = 2;
                            criminal.Weapons.Select(Weapon.Thrown_Molotov);

                            criminal.ComplianceChance = Common.GetRandomValue(25, 75);

                            Functions.AddPedToPursuit(this.pursuit, criminal);
                            this.criminals.Add(criminal);
                        }
                        else
                        {
                            Log.Debug("OnCalloutAccepted: Failed to place ped properly outside of building", this);
                            criminal.Delete();
                        }
                    }
                }

                if (this.criminals.Count == 0)
                {
                    isReady = false;
                }
                else
                {
                    // Create Johnny
                    johnny = new LPed(this.spawnPoint.Position.Around((float)10), "IG_JOHNNYBIKER", LPed.EPedGroup.MissionPed);
                    johnny.PersonaData = new PersonaData(new DateTime(1974, 3, 17), 13, "Johnny", "Klebitz", true, 9, true);
                    johnny.RelationshipGroup = RelationshipGroup.Gang_Biker2;
                    johnny.ChangeRelationship(RelationshipGroup.Gang_Biker2, Relationship.Companion);
                    johnny.ChangeRelationship(RelationshipGroup.Gang_Italian, Relationship.Hate);
                    johnny.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);
                    johnny.CantBeDamagedByRelationshipGroup(RelationshipGroup.Gang_Biker2, true);

                    // We don't want the criminal to flee yet
                    johnny.DisablePursuitAI = true;
                    johnny.EquipWeapon();

                    // Set up weapons
                    johnny.Weapons.RemoveAll();
                    johnny.Weapons.DesertEagle.Ammo = 999;
                    johnny.Weapons.AssaultRifle_AK47.Ammo = 300;
                    johnny.Weapons.Knife.Ammo = 1;
                    johnny.Weapons.AssaultRifle_AK47.Select();
                    johnny.ComplianceChance = Common.GetRandomValue(0, 30);

                    // Make default of the ped's component variation
                    Function.Call("SET_CHAR_DEFAULT_COMPONENT_VARIATION", new Parameter[] { johnny.GPed });

                    Functions.AddPedToPursuit(this.pursuit, johnny);
                    this.criminals.Add(johnny);
                }

                // Chance to spawn another bunch of suspects fighting each other
                if (Common.GetRandomBool(0, 2, 1))
                {
                    random = Common.GetRandomValue(7, 15);
                    for (int i = 0; i < random; i++)
                    {
                        LPed criminal = new LPed(this.spawnPoint.Position, Common.GetRandomCollectionValue<string>(this.mafiaModels), LPed.EPedGroup.Criminal);
                        if (ValidityCheck.isObjectValid(criminal))
                        {
                            Functions.AddToScriptDeletionList(criminal, this);
                            Functions.SetPedIsOwnedByScript(criminal, this, true);
                            criminal.RelationshipGroup = RelationshipGroup.Gang_Italian;
                            criminal.ChangeRelationship(RelationshipGroup.Gang_Italian, Relationship.Companion);
                            criminal.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);
                            criminal.ChangeRelationship(RelationshipGroup.Gang_Biker2, Relationship.Hate);

                            // We don't want the criminal to flee yet
                            criminal.DisablePursuitAI = true;
                            criminal.EquipWeapon();

                            criminal.Weapons.RemoveAll();
                            criminal.Weapons.Glock.Ammo = 999;
                            criminal.Weapons.Uzi.Ammo = 999;
                            criminal.Weapons.Knife.Ammo = 1;
                            if (Common.GetRandomBool(0, 50, 1))
                                criminal.Weapons.Uzi.Select();
                            else
                            {
                                criminal.Weapons.AssaultRifle_M4.Ammo = 999;
                                criminal.Weapons.AssaultRifle_M4.Select();
                            }

                            Functions.AddPedToPursuit(this.pursuit, criminal);
                            this.criminals.Add(criminal);
                        }
                    }

                    // Chance to start fighting immediately
                    if (Common.GetRandomBool(0, 2, 1))
                    {
                        this.State = EShootoutState.Fighting;
                        this.Engage();

                        // Request one backup unit automatically
                        Functions.RequestPoliceBackupAtPosition(LPlayer.LocalPlayer.Ped.Position);
                        Functions.RequestPoliceBackupAtPosition(LPlayer.LocalPlayer.Ped.Position);
                        Functions.PlaySoundUsingPosition("DFROM_DISPATCH_3_UNITS_FROM POSITION FOR CRIM_A_DOMESTIC_DISTURBANCE", LPlayer.LocalPlayer.Ped.Position);
                    }

                    Functions.AddTextToTextwall(string.Format(Resources.TEXT_INFO_RELAY_SV_JOHNNY, johnny.PersonaData.FullName, johnny.PersonaData.BirthDay),
                        Functions.GetStringFromLanguageFile("POLICE_SCANNER_CONTROL"));
                }
                isReady = true;
            }
                catch (Exception ex) { Log.Error("OnCalloutAccepted: Cannot create Pursuit instance: " + ex, this); isReady = false; }

            // Add states
            if (isReady)
            {
                this.RegisterStateCallback(EShootoutState.WaitingForPlayer, this.WaitingForPlayer);
                this.RegisterStateCallback(EShootoutState.PlayerIsClose, this.PlayerIsClose);
                this.RegisterStateCallback(EShootoutState.InCombat, this.InCombat);
                this.RegisterStateCallback(EShootoutState.Fighting, this.InCombat);
                this.RegisterStateCallback(EShootoutState.Prank, this.Prank);
                this.State = EShootoutState.WaitingForPlayer;
                Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_GET_TO_CRIME_SCENE"), 8000);
            }

            return isReady;
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the calloutmanager to shutdown the callout or can be called by the 
        /// callout itself to execute the cleanup code. Call base to set state to None.
        /// </summary>
        public override void End()
        {
            base.End();
            int salary = 1000;

            this.State = EShootoutState.None;

            if (ValidityCheck.isObjectValid(this.blip))
            {
                this.blip.Delete();
            }

            if (this.pursuit != null)
            {
                Functions.ForceEndPursuit(this.pursuit);
            }

            if (!this.IsPrankCall)
            {
                Functions.AddTextToTextwall(Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_END_TW"), Functions.GetStringFromLanguageFile("POLICE_SCANNER_CONTROL"));
                Functions.PrintText(string.Format(Resources.CALLOUT_SV_BONUS_SALARY, salary), 8000);
                LPlayer.LocalPlayer.Money += salary;
            }
        }

        /// <summary>
        /// Waiting for player.
        /// </summary>
        private void WaitingForPlayer()
        {
            if (LPlayer.LocalPlayer.Ped.Position.DistanceTo(this.spawnPoint.Position) < 80)
            {
                if (!this.IsPrankCall)
                {
                    foreach (LPed criminal in this.criminals)
                    {
                        criminal.Task.WanderAround();
                    }
                }

                // Create another unit to respond to the call
                Functions.RequestPoliceBackupAtPosition(LPlayer.LocalPlayer.Ped.Position);
                Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_PRANK_INVESTIGATE"), 5000);

                this.State = EShootoutState.PlayerIsClose;
            }
        }

        /// <summary>
        /// Player is close.
        /// </summary>
        private void PlayerIsClose()
        {
            if (LPlayer.LocalPlayer.Ped.Position.DistanceTo(this.spawnPoint.Position) > 55)
            {
                return;
            }

            this.blip.Delete();

            if (this.IsPrankCall)
            {
                DelayedCaller.Call(
                    delegate
                    {
                        // Tell control it was a prank
                        Functions.PlaySound("EMERG_PRANK_CALL", true, false);
                        Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_PRANK_END"), 5000);
                        Functions.AddTextToTextwall(Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_PRANK_END_TW"), Functions.GetStringFromLanguageFile("POLICE_SCANNER_CONTROL"));
                        this.End();
                    },
                    this,
                    10000);

                this.State = EShootoutState.Prank;
            }
            else
            {
                bool playerHasBeenSpotted = false;
                bool randomFight = Common.GetRandomBool(0, 300, 1);

                // Check whether player has been spotted
                foreach (LPed criminal in this.criminals)
                {
                    if (ValidityCheck.isObjectValid(criminal))
                    {
                        if (criminal.HasSpottedPed(LPlayer.LocalPlayer.Ped, false))
                        {
                            playerHasBeenSpotted = true;
                            break;
                        }
                    }
                }

                // If player has been spotted
                if (playerHasBeenSpotted || randomFight)
                {
                    this.State = EShootoutState.Fighting;
                    this.Engage();

                    Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_FIGHT_SUSPECTS"), 5000);

                    if (LPlayer.LocalPlayer.Model != new Model("M_Y_SWAT") && LPlayer.LocalPlayer.Model != new Model("M_M_FBI"))
                        LPlayer.LocalPlayer.Ped.SayAmbientSpeech("SPOT_SUSPECT");
                    else if (LPlayer.LocalPlayer.Model == new Model("M_Y_SWAT") || LPlayer.LocalPlayer.Model == new Model("M_M_FBI"))
                        LPlayer.LocalPlayer.Ped.SayAmbientSpeech("DRAW_GUN");
                    else LPlayer.LocalPlayer.Ped.SayAmbientSpeech("TARGET");
                }
            }
        }

        /// <summary>
        /// Starts fighting.
        /// </summary>
        private void Engage()
        {
            foreach (LPed criminal in this.criminals)
            {
                if (ValidityCheck.isObjectValid(criminal))
                {
                    // Enable chase AI and extend sense range so suspects won't flee immediately but fight longer
                    criminal.DisablePursuitAI = false;
                    criminal.RangeToDetectEnemies = 80f;
                }
            }

            // Maybe add chance suspects can flee too?
            Functions.SetPursuitDontEnableCopBlips(this.pursuit, false);
            Functions.SetPursuitAllowWeaponsForSuspects(this.pursuit, true);
            Functions.SetPursuitForceSuspectsToFight(this.pursuit, true);
            Functions.SetPursuitIsActiveDelayed(this.pursuit, 2500, 5000);
        }

        /// <summary>
        /// In combat.
        /// </summary>
        private void InCombat()
        {
            if (!Functions.IsPursuitStillRunning(this.pursuit))
            {
                this.SetCalloutFinished(true, true, true);
                this.End();
            }
        }

        /// <summary>
        /// Prank call.
        /// </summary>
        private void Prank()
        {
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