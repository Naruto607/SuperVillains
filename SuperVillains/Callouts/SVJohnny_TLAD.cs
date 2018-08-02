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
    /// Callout that calls the TLAD protagonist: Johnny Klebitz, doing a gang riot with his crew.
    /// </summary>
    [CalloutInfo("SVJohnny",ECalloutProbability.VeryLow)]
    internal class SVJohnny_TLAD:Callout
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
        /// Northern Guardians Division personnel.
        /// </summary>
        private List<LPed> NGD_Personnels;

        /// <summary>
        /// The criminal models.
        /// </summary>
        private string[] criminalModels = new string[]
            {
                "M_Y_GBIK_LO_01", "M_Y_GBIK_LO_02", "M_Y_GLOST_01", "M_Y_GLOST_02", "M_Y_GLOST_03",
                "M_Y_GLOST_04", "M_Y_GLOST_05", "M_Y_GLOST_06", "LOSTBUDDY_01", "LOSTBUDDY_02",
                "LOSTBUDDY_03", "LOSTBUDDY_04", "LOSTBUDDY_05", "LOSTBUDDY_06", "LOSTBUDDY_07",
                "LOSTBUDDY_08", "LOSTBUDDY_09", "LOSTBUDDY_10", "LOSTBUDDY_11", "LOSTBUDDY_12",
                "LOSTBUDDY_13", "F_Y_GLOST_01", "F_Y_GLOST_02", "F_Y_GLOST_03", "F_Y_GLOST_04"
            };

        /// <summary>
        /// Northern Guardians Division models.
        /// </summary>
        private string[] NGD_MemberModels = new string[]
        {
            "M_Y_CIADLC_01", "M_Y_CIADLC_02", "M_M_FBI"
        };

        /// <summary>
        /// Criminal leaders.
        /// </summary>
        private LPed johnny, clay, terry;

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
        /// Initializes a new instance of the <see cref="SVJohnny_TLAD"/> class.
        /// </summary>
        public SVJohnny_TLAD()
        {
            isOfficerCalledIn = Common.GetRandomBool(0, 5, 1);

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
            bool isReady = base.OnBeforeCalloutDisplayed();
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
            if (isOfficerCalledIn)
            {
                int rand = Common.GetRandomValue(0, 3);
                switch (rand)
                {
                    case 0: Functions.PlaySoundUsingPosition("THIS_IS_CONTROL INS_ALL_HANDS_TO CRIM_A_FIREARM_ATTACK_ON_AN_OFFICER IN_OR_ON_POSITION", this.spawnPoint.Position);
                        break;
                    case 1: Functions.PlaySoundUsingPosition("THIS_IS_CONTROL INS_AVAILABLE_UNITS_RESPOND_TO CRIM_AN_OFFICER_ASSAULT IN_OR_ON_POSITION", this.spawnPoint.Position);
                        break;
                    case 2:
                        string audioMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Officers);
                        string crimeMessage = "CRIM_GANG_RELATED_VIOLENCE";
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            crimeMessage = "CRIM_CRIMINALS_DISTURBING_THE_PEACE";
                        }

                        Functions.PlaySoundUsingPosition(audioMessage + crimeMessage + " IN_OR_ON_POSITION", this.spawnPoint.Position);
                        break;
                }
            }
            else
            {
                string audioMessage = Functions.CreateRandomAudioIntroString(EIntroReportedBy.Civilians);
                string crimeMessage = "CRIM_GANG_RELATED_VIOLENCE";
                if (Common.GetRandomBool(0, 2, 1))
                {
                    crimeMessage = "CRIM_A_DOMESTIC_DISTURBANCE";
                }

                Functions.PlaySoundUsingPosition(audioMessage + crimeMessage + " IN_OR_ON_POSITION", this.spawnPoint.Position);
            }

            return isReady;
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

                int random = Common.GetRandomValue(6, 16);
                for (int i = 0; i < random; i++)
                {
                    LPed criminal = new LPed(this.spawnPoint.Position, Common.GetRandomCollectionValue<string>(this.criminalModels), LPed.EPedGroup.Criminal);
                    if (criminal.isObjectValid()) // derived from ValidityCheck - greetings to LtFlash
                    {
                        // Ensure ped is not in a building
                        if (criminal.EnsurePedIsNotInBuilding(criminal.Position))
                        {
                            Functions.AddToScriptDeletionList(criminal, this);
                            Functions.SetPedIsOwnedByScript(criminal, this, true);
                            criminal.RelationshipGroup = RelationshipGroup.Gang_Biker2;
                            criminal.ChangeRelationship(RelationshipGroup.Gang_Biker2, Relationship.Companion);
                            criminal.ChangeRelationship(RelationshipGroup.Criminal, Relationship.Like);
                            criminal.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);
                            criminal.CantBeDamagedByRelationshipGroup(RelationshipGroup.Gang_Biker2, base.OnCalloutAccepted());

                            // We don't want the criminal to flee yet
                            criminal.DisablePursuitAI = true;

                            criminal.Weapons.RemoveAll();
                            criminal.Weapons.FromType(Weapon.TLAD_Automatic9mm).Ammo = 999;
                            criminal.DefaultWeapon = Weapon.TLAD_Automatic9mm;
                            if (Common.GetRandomBool(0,3,1))
                                criminal.Weapons.FromType(Weapon.Rifle_AK47).Ammo = 150;
                            else criminal.Weapons.FromType(Weapon.TLAD_AssaultShotgun).Ammo = 128;
                            criminal.Weapons.FromType(Weapon.Melee_Knife);
                            criminal.Weapons.FromType(Weapon.Thrown_Molotov).Ammo = 1;
                            criminal.Weapons.Select(Weapon.Thrown_Molotov);

                            criminal.CanSwitchWeapons = true;
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
                    isReady = false;
                else
                {
                    // Create Johnny
                    johnny = new LPed(this.spawnPoint.Position.Around((float)10), "IG_JOHNNYBIKER", LPed.EPedGroup.MissionPed);
                    johnny.PersonaData = new PersonaData(new DateTime(1974, 3, 17, 8, 30, 0, DateTimeKind.Utc), 13, "Johnny", "Klebitz", true, 9, true);
                    johnny.RelationshipGroup = RelationshipGroup.Special;
                    johnny.ChangeRelationship(RelationshipGroup.Special, Relationship.Companion);
                    johnny.ChangeRelationship(RelationshipGroup.Gang_Biker2, Relationship.Companion);
                    johnny.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);
                    johnny.CantBeDamagedByRelationshipGroup(RelationshipGroup.Gang_Biker2, base.OnCalloutAccepted());
                    johnny.CantBeDamagedByRelationshipGroup(RelationshipGroup.Special, base.OnCalloutAccepted());
                    johnny.BecomeMissionCharacter();

                    // Place near any criminal ped if he's in a building
                    if (johnny.EnsurePedIsNotInBuilding(johnny.Position) == false) johnny.Position = criminals[Common.GetRandomValue(0, criminals.Count-1)].Position + new Vector3(1.0f, 1.5f, 0);

                    // We don't want the criminal to flee yet
                    johnny.DisablePursuitAI = true;
                    johnny.EquipWeapon();

                    johnny.Weapons.RemoveAll();
                    johnny.Weapons.FromType(Weapon.Handgun_DesertEagle).Ammo = 999;
                    johnny.Weapons.FromType(Weapon.TLAD_GrenadeLauncher).Ammo = 100;
                    johnny.Weapons.FromType(Weapon.Melee_Knife).Ammo = 1;
                    johnny.Weapons.Select(Weapon.TLAD_GrenadeLauncher);
                    johnny.DefaultWeapon = Weapon.TLAD_GrenadeLauncher;

                    int tempVar = Common.GetRandomValue(100, 500);
                    johnny.MaxHealth = tempVar;
                    johnny.Health = tempVar;
                    johnny.Armor = 100;
                    johnny.AlwaysDiesOnLowHealth = base.OnCalloutAccepted();
                    johnny.ComplianceChance = Common.GetRandomValue(0, 25);

                    // Make default of the ped's component variation
                    Function.Call("SET_CHAR_DEFAULT_COMPONENT_VARIATION", new Parameter[] { johnny.GPed });

                    Functions.AddPedToPursuit(this.pursuit, johnny);
                    this.criminals.Add(johnny);

                    // Create Clay
                    clay = new LPed(johnny.Position, "IG_CLAY", LPed.EPedGroup.MissionPed);
                    clay.PersonaData = new PersonaData(new DateTime(1977, 5, 16), 20, "Clay", "", true, 6, true);
                    clay.RelationshipGroup = RelationshipGroup.Special;
                    clay.ChangeRelationship(RelationshipGroup.Special, Relationship.Companion);
                    clay.ChangeRelationship(RelationshipGroup.Gang_Biker2, Relationship.Companion);
                    clay.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);
                    clay.CantBeDamagedByRelationshipGroup(RelationshipGroup.Gang_Biker2, base.OnCalloutAccepted());
                    clay.CantBeDamagedByRelationshipGroup(RelationshipGroup.Special, base.OnCalloutAccepted());
                    clay.BecomeMissionCharacter();

                    // Place near any criminal ped if he's in a building
                    if (clay.EnsurePedIsNotInBuilding(clay.Position) == false) clay.Position = criminals[Common.GetRandomValue(0, criminals.Count-1)].Position + new Vector3(1.0f, -1.5f, 0);

                    // We don't want the criminal to flee yet
                    clay.DisablePursuitAI = true;
                    clay.EquipWeapon();

                    clay.Weapons.RemoveAll();
                    clay.Weapons.FromType(Weapon.Handgun_Glock).Ammo = 999;
                    clay.DefaultWeapon = Weapon.Handgun_Glock;
                    clay.Weapons.FromType(Weapon.Rifle_M4).Ammo = 150;
                    clay.Weapons.FromType(Weapon.TLAD_PipeBomb).Ammo = 3;
                    clay.Weapons.FromType(Weapon.Melee_Knife);
                    clay.Weapons.Select(Weapon.TLAD_PipeBomb);

                    tempVar = Common.GetRandomValue(200, 400);
                    clay.MaxHealth = tempVar;
                    clay.Health = tempVar;
                    clay.Armor = 20;
                    clay.AlwaysDiesOnLowHealth = base.OnCalloutAccepted();
                    clay.ComplianceChance = Common.GetRandomValue(0, 25);

                    // Make default of the ped's component variation
                    Function.Call("SET_CHAR_DEFAULT_COMPONENT_VARIATION", new Parameter[] { clay.GPed });

                    Functions.AddPedToPursuit(this.pursuit, clay);
                    this.criminals.Add(clay);

                    // Create Terry
                    terry = new LPed(johnny.Position, "IG_TERRY", LPed.EPedGroup.MissionPed);
                    terry.PersonaData = new PersonaData(new DateTime(1977, 8, 29), 14, "Terry", "", true, 8, true);
                    terry.RelationshipGroup = RelationshipGroup.Special;
                    terry.ChangeRelationship(RelationshipGroup.Gang_Biker2, Relationship.Companion);
                    terry.ChangeRelationship(RelationshipGroup.Special, Relationship.Companion);
                    terry.ChangeRelationship(RelationshipGroup.Cop, Relationship.Hate);
                    terry.CantBeDamagedByRelationshipGroup(RelationshipGroup.Gang_Biker2, base.OnCalloutAccepted());
                    terry.CantBeDamagedByRelationshipGroup(RelationshipGroup.Special, base.OnCalloutAccepted());
                    terry.BecomeMissionCharacter();

                    // Place near any criminal ped if he's in a building
                    if (terry.EnsurePedIsNotInBuilding(terry.Position) == false) terry.Position = criminals[Common.GetRandomValue(0, criminals.Count-1)].Position + new Vector3(-1.0f, 1.5f, 0);

                    // We don't want the criminal to flee yet
                    terry.DisablePursuitAI = true;
                    terry.EquipWeapon();

                    terry.Weapons.RemoveAll();
                    terry.Weapons.FromType(Weapon.TLAD_Automatic9mm).Ammo = 999;
                    terry.Weapons.FromType(Weapon.TLAD_AssaultShotgun).Ammo = 128;
                    terry.Weapons.FromType(Weapon.SMG_MP5).Ammo = 300;
                    terry.Weapons.FromType(Weapon.Melee_Knife);
                    terry.Weapons.Select(Weapon.SMG_MP5);
                    terry.DefaultWeapon = Weapon.SMG_MP5;

                    terry.MaxHealth = 250;
                    terry.Health = 250;
                    terry.Armor = 30;
                    terry.ComplianceChance = Common.GetRandomValue(0, 30);
                    terry.AlwaysDiesOnLowHealth = base.OnCalloutAccepted();

                    // Make default of the ped's component variation
                    Function.Call("SET_CHAR_DEFAULT_COMPONENT_VARIATION", new Parameter[] { terry.GPed });

                    Functions.AddPedToPursuit(this.pursuit, terry);
                    this.criminals.Add(terry);
                }

                // Chance to spawn NGD Personnel fighting each other
                // If spawned, it'll start fighting immediately
                if (Common.GetRandomBool(0, 2, 1))
                {
                    isOfficerCalledIn = true;
                    NGD_Personnels = new List<LPed>();
                    random = Common.GetRandomValue(6, 13);
                    for (int i = 0; i < random; i++)
                    {
                        LPed NGD = new LPed(World.GetPositionAround(this.spawnPoint.Position, 15f).ToGround(), Common.GetRandomCollectionValue<string>(this.NGD_MemberModels), LPed.EPedGroup.Cop);
                        if (NGD.isObjectValid()) // derived from ValidityCheck - greetings to LtFlash
                        {
                            Functions.AddToScriptDeletionList(NGD, this);
                            //Functions.SetPedIsOwnedByScript(NGD, this, true);
                            NGD.RelationshipGroup = RelationshipGroup.Cop;
                            NGD.ChangeRelationship(RelationshipGroup.Cop, Relationship.Companion);
                            NGD.ChangeRelationship(RelationshipGroup.Gang_Biker2, Relationship.Hate);
                            NGD.ChangeRelationship(RelationshipGroup.Special, Relationship.Hate);

                            // We don't want the personnel to flee yet
                            //NGD.DisablePursuitAI = true;

                            NGD.Weapons.RemoveAll();
                            NGD.Weapons.Glock.Ammo = 999;
                            NGD.Weapons.BasicShotgun.Ammo = 999;
                            if (Common.GetRandomBool(0, 100, 0))
                            {
                                NGD.Weapons.BasicSniperRifle.Ammo = 100;
                                NGD.Weapons.BasicSniperRifle.Select();
                                NGD.DefaultWeapon = Weapon.SniperRifle_Basic;
                            }
                            else
                            {
                                NGD.Weapons.AssaultRifle_M4.Ammo = 999;
                                NGD.Weapons.AssaultRifle_M4.Select();
                                NGD.DefaultWeapon = Weapon.Rifle_M4;
                            }

                            NGD.PriorityTargetForEnemies = true;

                            // Randomize NGD ped component variation
                            Function.Call("SET_CHAR_RANDOM_COMPONENT_VARIATION", new Parameter[] { NGD.GPed });
                            this.NGD_Personnels.Add(NGD);
                        }
                    }
                    this.State = EShootoutState.Fighting;
                    this.Engage();

                    // Request one backup unit automatically
                    Functions.RequestPoliceBackupAtPosition(LPlayer.LocalPlayer.Ped.Position);
                    Functions.PlaySoundUsingPosition("DFROM_DISPATCH_2_UNITS_FROM POSITION", LPlayer.LocalPlayer.Ped.Position);

                    // Create random vehicle
                    Vehicle ngdveh1 = World.CreateVehicle(World.GetNextPositionOnStreet(this.spawnPoint.Position.Around(18.1f)).ToGround());
                    Vehicle ngdveh2 = World.CreateVehicle(ngdveh1.Position.Around(3f).ToGround());

                    // Release them as an ambient vehicle
                    ngdveh1.NoLongerNeeded();
                    ngdveh2.NoLongerNeeded();
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
                Functions.AddTextToTextwall(string.Format(Resources.TEXT_INFO_RELAY_SV_JOHNNY, johnny.PersonaData.FullName, johnny.PersonaData.BirthDay),
                    Functions.GetStringFromLanguageFile("POLICE_SCANNER_CONTROL"));
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
            this.State = EShootoutState.None;

            if (this.blip.isObjectValid()) // derived from ValidityCheck - greetings to LtFlash
            {
                this.blip.Delete();
            }

            if (this.pursuit != null)
            {
                Functions.ForceEndPursuit(this.pursuit);
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
                        criminal.Task.FightAgainstHatedTargets(criminal.RangeToDetectEnemies);
                    }
                    /*foreach (LPed NGD_Member in this.NGD_Personnels)
                    {
                        NGD_Member.Task.FightAgainstHatedTargets((float)Common.GetRandomValue(100, 200));
                    }*/
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
                    if (criminal.isObjectValid()) // derived from ValidityCheck - greetings to LtFlash
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
        /// Starts fighting.
        /// </summary>
        private void Engage()
        {
            foreach (LPed criminal in this.criminals)
            {
                if (criminal.isObjectValid()) // derived from ValidityCheck - greetings to LtFlash
                {
                    // Enable chase AI and extend sense range so suspects won't flee immediately but fight longer
                    criminal.DisablePursuitAI = false;
                    criminal.RangeToDetectEnemies = 80f*4;
                    criminal.StartKillingSpree(true);
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

                DelayedCaller.Call(delegate
                {
                    Functions.AddTextToTextwall(Resources.TEXT_INFO_RELAY_SV_JOHNNY_MISSION_UPDATE, Functions.GetStringFromLanguageFile("POLICE_SCANNER_CONTROL"));
                }, this, 5000);
            }
            else Functions.SetPursuitIsActiveDelayed(this.pursuit, 2500, 5000);
        }

        /// <summary>
        /// Reports player to Control to confirm in contact with suspect.
        /// </summary>
        private void Player_Yell()
        {
            if (LPlayer.LocalPlayer.Model == new Model("M_Y_COP") || LPlayer.LocalPlayer.Model == new Model("M_M_FATCOP_01") || LPlayer.LocalPlayer.Model == new Model("M_Y_STROOPER") || LPlayer.LocalPlayer.Model == new Model("M_Y_COP_TRAFFIC"))
                LPlayer.LocalPlayer.Ped.SayAmbientSpeech("SPOT_SUSPECT");
            else if (LPlayer.LocalPlayer.Model == new Model("M_Y_SWAT") || LPlayer.LocalPlayer.Model == new Model("M_M_FBI"))
                LPlayer.LocalPlayer.Ped.SayAmbientSpeech("DRAW_GUN");
            else LPlayer.LocalPlayer.Ped.SayAmbientSpeech("TARGET");
        }

        /// <summary>
        /// In combat.
        /// </summary>
        private void InCombat()
        {
            if (!Functions.IsPursuitStillRunning(this.pursuit))
            {
                int salary = 6800;
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
