using GTA;
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
    /// Callout that calls the main GTA protagonist: Niko Bellic, with two friends and a rocket launcher.
    /// </summary>
    [CalloutInfo("SVNiko", ECalloutProbability.VeryLow)]
    internal class SVNiko : Callout
    {
        #region Initialization
        /// <summary>
        /// The blip of the position.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The criminals.
        /// </summary>
        private List<LPed> criminals;

        /// <summary>
        /// The pursuit instance used in case suspect wants to flee.
        /// </summary>
        private LHandle pursuit;

        /// <summary>
        /// The spawn point.
        /// </summary>
        private SpawnPoint spawnPoint;

        /// <summary>
        /// Checks whether the officer is called in the callout, that is, an officer is requesting assistance.
        /// </summary>
        private bool isOfficerCalledIn = new Boolean();

        /// <summary>
        /// A number of GTA peds for process method.
        /// </summary>
        private Ped[] peds;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Shootout"/> class.
        /// </summary>
        public SVNiko()
        {
            isOfficerCalledIn = Common.GetRandomBool(0, 5, 1);

            if (isOfficerCalledIn)
                this.CalloutMessage = Resources.CALLOUT_SV_NIKO_MESSAGE;
            else this.CalloutMessage = Resources.CALLOUT_SV_NIKO_CIVIL_MSG;
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

            if (isOfficerCalledIn)
            {
                this.CalloutMessage = string.Format(Resources.CALLOUT_SV_NIKO_MESSAGE, area);

                // Play audio
                string audioMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Officers);
                string crimeMessage = "CRIM_SHOTS_FIRED_AT_AN_OFFICER";
                if (Common.GetRandomBool(0, 2, 1))
                {
                    crimeMessage = "CRIM_AN_OFFICER_IN_DANGER_OF_FIREARM_DISCHARGE_FROM_SUSPECT";
                }

                Functions.PlaySoundUsingPosition(audioMessage + crimeMessage + " IN_OR_ON_POSITION ALL_UNITS_PLEASE_RESPOND", this.spawnPoint.Position);
            }
            else
            {
                this.CalloutMessage = string.Format(Resources.CALLOUT_SV_NIKO_CIVIL_MSG, area);
                int rand = Common.GetRandomValue(0, 3);

                switch (rand)
                {
                    // Play audio
                    case 0:
                        string audioMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Civilians);
                        string crimeMessage = "CRIM_A_SUSPECT_ARMED_AND_DANGEROUS";
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            crimeMessage = "CRIM_CRIMINALS_DISTURBING_THE_PEACE";
                        }

                        Functions.PlaySoundUsingPosition(audioMessage + crimeMessage + " IN_OR_ON_POSITION", this.spawnPoint.Position);
                        break;
                    case 1:
                        Functions.PlaySoundUsingPosition("ATTENTION_ALL_UNITS INS_I_NEED_A_UNIT_FOR CRIM_POSSIBLE_TERRORIST_ACTIVITY IN_OR_ON_POSITION PLEASE_INVESTIGATE", this.spawnPoint.Position); break;
                    case 2:
                        Functions.PlaySoundUsingPosition("THIS_IS_CONTROL UNITS_PLEASE_BE_ADVISED INS_WE_HAVE_A_REPORT_OF_ERRR CRIM_TERRORIST_ACTIVITY IN_OR_ON_POSITION", this.spawnPoint.Position); break;
                }
            }

            return base.OnBeforeCalloutDisplayed();
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
            if (Common.GetRandomBool(0, 5, 1) && !isOfficerCalledIn)
            {
                Log.Debug("OnCalloutAccepted: Is prank", this);
                this.SetAsPrankCall();
            }
            else try
                {
                    this.criminals = new List<LPed>();

                    // Create Niko with an RPG
                    LPed criminal1 = new LPed(this.spawnPoint.Position, "IG_NIKO", LPed.EPedGroup.Criminal);
                    criminal1.Weapons.RemoveAll();
                    criminal1.Weapons.RocketLauncher.Ammo = 100;
                    criminal1.Weapons.DesertEagle.Ammo = 999;
                    criminal1.Weapons.BaseballBat.Ammo = 1;
                    criminal1.DefaultWeapon = Weapon.Heavy_RocketLauncher;
                    criminal1.Weapons.RocketLauncher.Select();
                    criminal1.MaxHealth = 400;
                    criminal1.Health = 400;
                    criminal1.PersonaData = new PersonaData(new DateTime(1978, 6, 15), 10, "Niko", "Bellic", false, 8, true);
                    criminal1.ComplianceChance = Common.GetRandomValue(0, 60);

                    // Make default of the ped's component variation
                    Function.Call("SET_CHAR_DEFAULT_COMPONENT_VARIATION", new Parameter[] { criminal1.GPed });

                    // Create Little Jacob with an AK
                    LPed criminal2 = new LPed(this.spawnPoint.Position, "IG_LILJACOB", LPed.EPedGroup.Criminal);
                    criminal2.Weapons.RemoveAll();
                    criminal2.Weapons.AssaultRifle_AK47.Ammo = 999;
                    criminal2.Weapons.Glock.Ammo = 999;
                    criminal2.Weapons.Knife.Ammo = 1;
                    criminal2.DefaultWeapon = Weapon.Rifle_AK47;
                    criminal2.Weapons.AssaultRifle_AK47.Select();
                    criminal2.MaxHealth = 300;
                    criminal2.Health = 300;
                    criminal2.PersonaData = new PersonaData(new DateTime(1981, 4, 22), 4, "\"Little\" Jacob", "Hughes", true, 3, true);
                    criminal2.ComplianceChance = Common.GetRandomValue(50, 100);

                    // Make default of the ped's component variation
                    Function.Call("SET_CHAR_DEFAULT_COMPONENT_VARIATION", new Parameter[] { criminal2.GPed });

                    // Create Packie with a bolt-action sniper
                    LPed criminal3 = new LPed(this.spawnPoint.Position, "IG_PACKIE_MC", LPed.EPedGroup.Criminal);
                    criminal3.Weapons.RemoveAll();
                    criminal3.Weapons.SniperRifle_M40A1.Ammo = 999;
                    criminal3.Weapons.Glock.Ammo = 999;
                    criminal3.Weapons.Knife.Ammo = 1;
                    //criminal3.DefaultWeapon = Weapon.Handgun_Glock;
                    criminal3.DefaultWeapon = Weapon.SniperRifle_M40A1;
                    criminal3.Weapons.SniperRifle_M40A1.Select();
                    criminal3.MaxHealth = 300;
                    criminal3.Health = 300;
                    criminal3.Armor = 50;
                    criminal3.PersonaData = new PersonaData(new DateTime(1979, 8, 13), 10, "Patrick \"Packie\"", "McReary", true, 5, true);
                    criminal3.ComplianceChance = Common.GetRandomValue(75, 100);

                    // Make default of the ped's component variation
                    Function.Call("SET_CHAR_DEFAULT_COMPONENT_VARIATION", new Parameter[] { criminal3.GPed });

                    criminals.Add(criminal1);
                    criminals.Add(criminal2);
                    criminals.Add(criminal3);
                    foreach (LPed criminal in criminals)
                    {
                        if (criminal.isObjectValid())
                        {
                            Functions.AddToScriptDeletionList(criminal, this);
                            Functions.SetPedIsOwnedByScript(criminal, this, true);
                            criminal.RelationshipGroup = RelationshipGroup.Criminal;
                            criminal.ChangeRelationship(RelationshipGroup.Criminal, Relationship.Companion);
                            criminal.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);

                            // We don't want the criminal to flee yet
                            criminal.DisablePursuitAI = true;
                            criminal.CantBeDamagedByRelationshipGroup(RelationshipGroup.Criminal, base.OnCalloutAccepted());

                            Functions.AddPedToPursuit(this.pursuit, criminal);

                            // Place near any street if a criminal is in a building
                            if (!criminal.EnsurePedIsNotInBuilding(criminal.Position))
                                criminal.Position = World.GetNextPositionOnStreet(this.spawnPoint.Position);
                        }
                    }

                // Start fighting immediately if the officers on the field are calling the callout in
                    if (isOfficerCalledIn)
                    {
                        Vector3 temp1 = World.GetNextPositionOnPavement(this.spawnPoint.Position);
                        Vector3 temp2 = (temp1 + new Vector3(0.3f, 0.3f, 0)).ToGround();
                        Vector3 temp3 = World.GetNextPositionOnStreet(this.spawnPoint.Position).Around((float)Common.GetRandomValue(5, 15));
                        Vector3 temp4 = (temp1 + new Vector3(0.9f, 0.9f, 0)).ToGround();
                        LPed[] cops = new LPed[]
                        {
                            new LPed(temp1, "M_M_FATCOP_01", LPed.EPedGroup.Cop),
                            new LPed(temp2, "M_M_FATCOP_01", LPed.EPedGroup.Cop),
                            new LPed(temp3, "M_M_FATCOP_01", LPed.EPedGroup.Cop),
                            new LPed(temp4, "M_M_FATCOP_01", LPed.EPedGroup.Cop)
                        };

                        foreach (LPed cop in cops)
                        {
                            Functions.AddToScriptDeletionList(cop, this);
                            cop.Weapons.RemoveAll();
                            cop.Weapons.DesertEagle.Ammo = 999;
                            cop.Weapons.BarettaShotgun.Ammo = 999;
                            cop.DefaultWeapon = Weapon.Shotgun_Baretta;
                            cop.Weapons.BarettaShotgun.Select();

                            Function.Call("SET_CHAR_RANDOM_COMPONENT_VARIATION", new Parameter[] { cop.GPed });
                        }

                        // Create cop cars
                        Vehicle copcar1 = World.CreateVehicle(new Model("POLICE"), World.GetNextPositionOnStreet(temp4).ToGround());
                        Vehicle copcar2 = World.CreateVehicle(new Model("POLICE"), copcar1.Position.Around(3.3f).ToGround());

                        // Don't place it REALLY close to the terrorists
                        if ((copcar1.Position - criminal1.Position).Length() < 10f) copcar1.Position = World.GetNextPositionOnStreet(copcar1.Position + new Vector3(13, 15, 0)).ToGround();
                        if ((copcar2.Position - criminal1.Position).Length() < 10f) copcar2.Position = World.GetNextPositionOnStreet(copcar2.Position.Around(4f) + new Vector3(13, 15, 0)).ToGround();

                        // Release them as an ambient vehicle
                        copcar1.NoLongerNeeded();
                        copcar2.NoLongerNeeded();

                        this.State = EShootoutState.Fighting;
                        this.Engage();

                        // Request one backup unit automatically
                        Functions.RequestPoliceBackupAtPosition(LPlayer.LocalPlayer.Ped.Position);
                        Functions.PlaySoundUsingPosition("DFROM_DISPATCH_2_UNITS_FROM POSITION", LPlayer.LocalPlayer.Ped.Position);
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
            if (LPlayer.LocalPlayer.Ped.HasBeenDamagedBy(Weapon.Heavy_RocketLauncher))
                LPlayer.LocalPlayer.Ped.ForceRagdoll(30000, false);
            else if (LPlayer.LocalPlayer.Ped.HasBeenDamagedBy(Weapon.SniperRifle_M40A1))
                LPlayer.LocalPlayer.Ped.ForceRagdoll(10000, true);

            peds = World.GetAllPeds();
            foreach (Ped ped in peds)
            {
                if (ped.isObjectValid())
                {
                    if (ped.HasBeenDamagedBy(Weapon.Heavy_RocketLauncher))
                        ped.ForceRagdoll(30000, false);
                    else if (ped.HasBeenDamagedBy(Weapon.SniperRifle_M40A1))
                        ped.ForceRagdoll(10000, true);
                }
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the calloutmanager to shutdown the callout or can be called by the 
        /// callout itself to execute the cleanup code. Call base to set state to None.
        /// </summary>
        public override void End()
        {
            base.End();
            this.State = EShootoutState.None;

            if (this.blip != null && this.blip.Exists())
            {
                this.blip.Delete();
            }

            if (this.pursuit != null)
            {
                Functions.ForceEndPursuit(this.pursuit);
            }
            
            foreach (Ped ped in peds) // Release the peds from script control
            {
                if (ped.isObjectValid()) ped.NoLongerNeeded();
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
                        criminal.StartKillingSpree(true);
                        criminal.Task.FightAgainstHatedTargets(criminal.RangeToDetectEnemies);
                    }
                }

                // Create a unit to respond to the call
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
                DelayedCaller.Call(ReportControl_IsPrank, this, 10000);
                this.State = EShootoutState.Prank;
            }
            else
            {
                bool playerHasBeenSpotted = false;
                bool randomFight = Common.GetRandomBool(0, 300, 1);

                // Check whether player has been spotted
                foreach (LPed criminal in this.criminals)
                {
                    //if (criminal.Exists())
                    if (criminal.isObjectValid())
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
                    this.Player_Yell();

                    Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_FIGHT_SUSPECTS"), 5000);
                }
            }
        }

        /// <summary>
        /// Alternate delegate method that tells the player to control/dispatch that it was a prank call.
        /// </summary>
        /// <param name="parameter">Parameters</param>
        private void ReportControl_IsPrank(params object[] parameter)
        {
            // Tell control it was a prank
            Functions.PlaySound("EMERG_PRANK_CALL", true, false);
            Functions.PrintText(Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_PRANK_END"), 5000);
            Functions.AddTextToTextwall(Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_PRANK_END_TW"), Functions.GetStringFromLanguageFile("POLICE_SCANNER_CONTROL"));
            this.End();
            // This is a small demo if you are building the callout script from the scratch and don't know how to use a delegate method, although simple by design (you just need to put functions in there)
            // Note that delegate method only works on some functions like DelayedCaller.Call and other compatible methods
        }

        /// <summary>
        /// Starts fighting.
        /// </summary>
        private void Engage()
        {
            foreach (LPed criminal in this.criminals)
            {
                if (criminal.isObjectValid())
                {
                    // Enable chase AI and extend sense range so suspects won't flee immediately but fight longer
                    criminal.DisablePursuitAI = false;
                    criminal.RangeToDetectEnemies = 80f*2;
                }
            }

            // Maybe add chance suspects can flee too?
            Functions.SetPursuitDontEnableCopBlips(this.pursuit, false);
            Functions.SetPursuitAllowWeaponsForSuspects(this.pursuit, true);
            Functions.SetPursuitForceSuspectsToFight(this.pursuit, true);

            if (isOfficerCalledIn)
            {
                Functions.SetPursuitIsActiveDelayed(this.pursuit, 0, Common.GetRandomValue(1000, 5000));
                Functions.SetPursuitTactics(this.pursuit, true);
                Functions.SetPursuitHelicopterTactics(this.pursuit, true);
                Functions.SetPursuitMaximumUnits(this.pursuit, 40, 40);
            }
            else Functions.SetPursuitIsActiveDelayed(this.pursuit, 2500, 5000);
        }

        /// <summary>
        /// Reports player to Control to confirm in contact with suspect.
        /// </summary>
        private void Player_Yell()
        {
            // Request two police backups if the player is not a SWAT/FIB member
            if (LPlayer.LocalPlayer.Model != new Model("M_Y_SWAT") && LPlayer.LocalPlayer.Model != new Model("M_M_FBI"))
            {

                Functions.AddTextToTextwall(string.Format(Resources.TEXT_INFO_RELAY_SV_NIKO, LPlayer.LocalPlayer.Username), Functions.GetStringFromLanguageFile("POLICE_SCANNER_OFFICER") + " " + LPlayer.LocalPlayer.Username);
                //LPlayer.LocalPlayer.Ped.SayAmbientSpeech("REQUEST_BACKUP");
                Functions.PlaySound("REQUEST_BACKUP", true, false);

                DelayedCaller.Call(delegate
                {
                    Functions.RequestPoliceBackupAtPosition(LPlayer.LocalPlayer.Ped.Position);
                    Functions.RequestPoliceBackupAtPosition(LPlayer.LocalPlayer.Ped.Position);
                    Functions.PlaySoundUsingPosition("THIS_IS_CONTROL INS_ALL_HANDS_TO CRIM_AN_OFFICER_IN_DANGER_OF_FIREARM_DISCHARGE_FROM_SUSPECT IN_OR_ON_POSITION SUSPECT ARMED_AND_DANGEROUS ALL_UNITS_PLEASE_RESPOND", this.spawnPoint.Position);
                }, this, 3000);
            }
            else if (LPlayer.LocalPlayer.Model == new Model("M_Y_SWAT") || LPlayer.LocalPlayer.Model == new Model("M_M_FBI"))
            {
                Functions.AddTextToTextwall(string.Format(Resources.TEXT_INFO_RELAY_SV_NIKO_SWAT, LPlayer.LocalPlayer.Username, criminals[0].PersonaData.FullName), Functions.GetStringFromLanguageFile("POLICE_SCANNER_OFFICER") + " " + LPlayer.LocalPlayer.Username);
                LPlayer.LocalPlayer.Ped.SayAmbientSpeech("FIGHT");

                DelayedCaller.Call(delegate
                {
                    Functions.PlaySoundUsingPosition(Functions.CreateRandomAudioIntroString(EIntroReportedBy.Officers) + "CRIM_TERRORIST_ACTIVITY IN_OR_ON_POSITION", this.spawnPoint.Position);
                }, this, 2000);
            }
            else
            {
                Functions.AddTextToTextwall(string.Format(Resources.TEXT_INFO_RELAY_SV_NIKO_SWAT, LPlayer.LocalPlayer.Username, criminals[0].PersonaData.FullName), Functions.GetStringFromLanguageFile("POLICE_SCANNER_OFFICER") + " " + LPlayer.LocalPlayer.Username);
                LPlayer.LocalPlayer.Ped.SayAmbientSpeech("TARGET");

                DelayedCaller.Call(delegate
                {
                    Functions.PlaySoundUsingPosition(Functions.CreateRandomAudioIntroString(EIntroReportedBy.Officers) + "CRIM_TERRORIST_ACTIVITY IN_OR_ON_POSITION", this.spawnPoint.Position);
                }, this, 2000);
            }
        }

        /// <summary>
        /// In combat.
        /// </summary>
        private void InCombat()
        {
            if (!Functions.IsPursuitStillRunning(this.pursuit))
            {
                int salary = 10000;
                this.SetCalloutFinished(true, true, true);
                Functions.AddTextToTextwall(Functions.GetStringFromLanguageFile("CALLOUT_SHOOTOUT_END_TW"), Functions.GetStringFromLanguageFile("POLICE_SCANNER_CONTROL"));
                Functions.PrintText(string.Format(Resources.CALLOUT_SV_BONUS_SALARY, salary), 8000);
                LPlayer.LocalPlayer.Money += salary;
                this.End();
            }
        }

        /// <summary>
        /// Prank call.
        /// </summary>
        private void Prank()
        {
            // TODO: Set a nearest ped on the street as a prank caller - easily identified by someone continuously talking on the phone
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
