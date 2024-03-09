/*
 * This file is part of Dematerializer, a Better Rimworlds Project.
 *
 * Copyright © 2024 Theodore R. Smith
 * Author: Theodore R. Smith <hopeseekr@gmail.com>
 *   GPG Fingerprint: D8EA 6E4D 5952 159D 7759  2BB4 EEB6 CE72 F441 EC41
 *   https://github.com/BetterRimworlds/Dematerializer
 *
 * This file is licensed under the Creative Commons No-Derivations v4.0 License.
 * Most rights are reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using RimWorld;

namespace BetterRimworlds.Dematerializer
{
    [StaticConstructorOnStartup]
    class Building_Dematerializer : Building, IThingHolder
    {
        const int ADDITION_DISTANCE = 3;

        public bool PoweringUp = true;

        protected DematerializerBuffer dematerializedBuffer;
        protected Area_Allowed teleportArea;

        protected static Texture2D UI_ADD_RESOURCES;
        protected static Texture2D UI_ADD_COLONIST;

        protected static Texture2D UI_GATE_IN;
        protected static Texture2D UI_GATE_OUT;

        protected static Texture2D UI_POWER_UP;
        protected static Texture2D UI_POWER_DOWN;

        static Graphic graphicActive;
        static Graphic graphicInactive;

        private bool isPowerInited = false;
        CompPowerTrader power;
        // CompProperties_Power powerProps;

        int currentCapacitorCharge = 1000;
        int requiredCapacitorCharge = 1000;
        int chargeSpeed = 1;

        protected Map currentMap;

        static Building_Dematerializer()
        {
            UI_ADD_RESOURCES = ContentFinder<Texture2D>.Get("UI/ADD_RESOURCES", true);
            UI_ADD_COLONIST = ContentFinder<Texture2D>.Get("UI/ADD_COLONIST", true);

            UI_GATE_IN = ContentFinder<Texture2D>.Get("UI/StargateGUI-In", true);
            UI_GATE_OUT = ContentFinder<Texture2D>.Get("UI/StargateGUI-Out", true );

            UI_POWER_UP = ContentFinder<Texture2D>.Get("UI/PowerUp", true);
            UI_POWER_DOWN = ContentFinder<Texture2D>.Get("UI/PowerDown", true);

#if RIMWORLD12
            GraphicRequest requestActive = new GraphicRequest(Type.GetType("Graphic_Single"), "Things/Buildings/Dematerializer-Active",   ShaderDatabase.DefaultShader, new Vector2(3, 3), Color.white, Color.white, new GraphicData(), 0, null);
            GraphicRequest requestInactive = new GraphicRequest(Type.GetType("Graphic_Single"), "Things/Buildings/Dematerializer", ShaderDatabase.DefaultShader, new Vector2(3, 3), Color.white, Color.white, new GraphicData(), 0, null);
#endif
#if RIMWORLD13 || RIMWORLD14
            GraphicRequest requestActive = new GraphicRequest(Type.GetType("Graphic_Single"), "Things/Buildings/Dematerializer-Active",   ShaderDatabase.DefaultShader, new Vector2(3, 3), Color.white, Color.white, new GraphicData(), 0, null, null);
            GraphicRequest requestInactive = new GraphicRequest(Type.GetType("Graphic_Single"), "Things/Buildings/Dematerializer", ShaderDatabase.DefaultShader, new Vector2(3, 3), Color.white, Color.white, new GraphicData(), 0, null, null);
#endif

            graphicActive = new Graphic_Single();
            graphicActive.Init(requestActive);

            graphicInactive = new Graphic_Single();
            graphicInactive.Init(requestInactive);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            this.currentMap = map;

            this.power = base.GetComp<CompPowerTrader>();
            this.dematerializedBuffer ??= new DematerializerBuffer(this);

            // this.power = new CompPowerTrader();

            Log.Warning("Found some things in the Dematerializer's buffer: " + this.dematerializedBuffer.Count);
            var foundTeleportArea = Find.CurrentMap.areaManager.GetLabeled("Teleport Field");
            if (foundTeleportArea is null)
            {
                foundTeleportArea = new Area_Allowed(map.areaManager, "Teleport Field");
                Find.CurrentMap.areaManager.AllAreas.Add(foundTeleportArea);
            }

            this.teleportArea = (Area_Allowed) foundTeleportArea;
            
            base.SpawnSetup(map, respawningAfterLoad);
            // this.dematerializedBuffer.Init();
        }

        // For displaying contents to the user.
        public ThingOwner GetDirectlyHeldThings() => this.dematerializedBuffer;

        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, (IList<Thing>) this.GetDirectlyHeldThings());

        public override string GetInspectString()
        {
            // float excessPower = this.power.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick;
            return "Capacitor Charge: " + this.currentCapacitorCharge + " / " + this.requiredCapacitorCharge + "\n"
                 + "Power needed: " + Math.Round(this.power.powerOutputInt * -1.0f) + " W\n"
                 + "Buffer Items: " + this.dematerializedBuffer.Count + " / " + this.dematerializedBuffer.getMaxStacks() + "\n"
                 + "Stored Mass: " + this.dematerializedBuffer.GetStoredMass() + " kg"
                // + "Gain Rate: " + excessPower + "\n"
                // + "Stored Energy: " + this.power.PowerNet.CurrentStoredEnergy()
                ;
        }

        // Saving game
        public override void ExposeData()
        {
            Scribe_Values.Look<int>(ref currentCapacitorCharge, "currentCapacitorCharge");
            Scribe_Values.Look<int>(ref requiredCapacitorCharge, "requiredCapacitorCharge");
            Scribe_Values.Look<int>(ref chargeSpeed, "chargeSpeed");
            // Scribe_Values.Look<CompPowerTrader>(ref power, "power");

            Scribe_Deep.Look<DematerializerBuffer>(ref this.dematerializedBuffer, "dematerializedBuffer", new object[]
            {
                this
            });

            base.ExposeData();
        }

        protected void BaseTickRare()
        {
            base.TickRare();
        }

        public override void TickRare()
        {
            if (!this.dematerializedBuffer.Any())
            {
                if (this.fullyCharged == true)
                {
                    this.power.powerOutputInt = 0;
                    chargeSpeed = 0;
                    this.updatePowerDrain();
                }

                if (this.fullyCharged == false && this.power.PowerOn)
                {
                    currentCapacitorCharge += chargeSpeed;

                    float excessPower = this.power.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick;
                    if (excessPower + (this.power.PowerNet.CurrentStoredEnergy() * 1000) > 5000)
                    {
                        // chargeSpeed += 5 - (this.chargeSpeed % 5);
                        chargeSpeed = (int)Math.Round(this.power.PowerNet.CurrentStoredEnergy() * 0.25 / 10);
                        this.updatePowerDrain();
                    }
                    else if (excessPower + (this.power.PowerNet.CurrentStoredEnergy() * 1000) > 1000)
                    {
                        chargeSpeed += 1;
                        this.updatePowerDrain();
                    }
                }
            }

            if (this.fullyCharged == true)
            {
                bool hasNoPower = this.power.PowerNet == null || !this.power.PowerNet.HasActivePowerSource;
                bool hasInsufficientPower = this.power.PowerOn == false;
                if (hasNoPower || hasInsufficientPower)
                {
                    // if (hasNoPower)
                    // {
                    //     Log.Error("NO POWER");
                    // }
                    //
                    // if (hasInsufficientPower)
                    // {
                    //     Log.Error("INSUFFICIENT POWER");
                    // }

                    // Ignore power requirements during a solar flare.
                    bool isSolarFlare = this.currentMap.gameConditionManager.ConditionIsActive(GameConditionDefOf.SolarFlare);
                    if (isSolarFlare)
                    {
                        return;
                    }

                    // Log.Error("========= NOT ENOUGH POWER +========");
                    this.EjectLeastMassive();
                    return;
                }
                if (this.isPowerInited == false)
                {
                    this.dematerializedBuffer.Init();
                    this.isPowerInited = true;
                    this.power.PowerOutput = -10000;
                }

                // Auto-add stuff if it's inside the Stargate area.
                // There is no gate address yet. Abort.
                this.AddResources();
            }
            
            base.TickRare();
        }

        #region Commands

        private bool fullyCharged
        {
            get
            {
                return (this.currentCapacitorCharge >= this.requiredCapacitorCharge);
            }
        }

        protected IEnumerable<Gizmo> GetDefaultGizmos()
        {
            return base.GetGizmos();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            // Add the stock Gizmoes
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            if (true)
            {
                Command_Action act = new Command_Action();
                //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                act.action = () => this.Dematerialize();
                act.icon = UI_GATE_OUT;
                act.defaultLabel = "Dematerialize";
                act.defaultDesc = "Dematerialize";
                act.activateSound = SoundDef.Named("Click");
                //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                //act.groupKey = 689736;
                yield return act;
            }

            if (DefDatabase<ResearchProjectDef>.GetNamed("BRW_QuantumTeleportation").IsFinished)
            {
                Command_Action act = new Command_Action();
                //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                act.action = () => this.Rematerialize();
                act.icon = UI_GATE_IN;
                act.defaultLabel = "Materialize";
                act.defaultDesc = "Materialize";
                act.activateSound = SoundDef.Named("Click");
                //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                //act.groupKey = 689736;
                yield return act;
            }

            // +57 319-666-8030
        }

        public void AddResources()
        {
            if (this.fullyCharged == false) {
                return;
            }

            List<Thing> foundThings = BetterRimworlds.Utilities.FindItemThingsNearBuilding(this, Building_Dematerializer.ADDITION_DISTANCE, this.currentMap);
 
            foreach (Thing foundThing in foundThings)
            {
                this.dematerializedBuffer.TryAdd(foundThing);
            }
        }

        public bool HasThingsInBuffer()
        {
            return this.dematerializedBuffer.Count > 0;
        }

        public void Dematerialize()
        {
            if (this.fullyCharged)
            {
                //Log.Message("CLick AddColonist");
                IEnumerable<Pawn> closePawns = BetterRimworlds.Utilities.findPawnsInArea(this.teleportArea);

                if (closePawns != null)
                {
                    foreach (Pawn currentPawn in closePawns.ToList())
                    {
                        if (currentPawn.Spawned)
                        {
                            // Fixes a bug w/ support for B19+ and later where colonists go *crazy*
                            // if they enter a Stargate after they've ever been drafted.
                            if (currentPawn.verbTracker != null)
                            {
                                currentPawn.verbTracker = new VerbTracker(currentPawn);
                            }

                            // Remove memories or they will go insane...
                            if (currentPawn.def.defName == "Human")
                            {
                                currentPawn.needs.mood.thoughts.memories = new MemoryThoughtHandler(currentPawn);
                            }

                            this.dematerializedBuffer.TryAdd(currentPawn);
                        }
                    }
                }

                // Tell the MapDrawer that here is something thats changed
                Find.CurrentMap.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
                //this.teleportArea = new Area_Allowed(this.currentMap.areaManager, "Teleport Field");
                this.teleportArea.Delete();
                // Find.CurrentMap.areaManager.AllAreas.Add(this.teleportArea);
            }
            else
            {
                Messages.Message("Insufficient Power to add Colonist", MessageTypeDefOf.RejectInput);
            }
        }

        public virtual bool Rematerialize()
        {
            /* Tuple<int, List<Thing>> **/
            var recallData = this.dematerializedBuffer.ToList();
            this.dematerializedBuffer.Clear();
 
            if (recallData.Count == 0)
            {
                Messages.Message("WARNING: The Stargate buffer was empty!!", MessageTypeDefOf.ThreatBig);
                return false;
            }
            
            bool wasPlaced;
            foreach (Thing currentThing in recallData)
            {
                try
                {
                    // If it's just a teleport, destroy the thing first...
                    // Log.Warning("a1: is offworld? " + offworldEvent + " | Stargate Buffer count: " + this.stargateBuffer.Count);
                    wasPlaced = GenPlace.TryPlaceThing(currentThing, this.Position + new IntVec3(0, 0, -2),
                        this.currentMap, ThingPlaceMode.Near);
                    // Readd the unplaced Thing into the stargateBuffer.
                    if (!wasPlaced)
                    {
                        Log.Warning("Could not place " + currentThing.Label);
                        this.dematerializedBuffer.TryAdd(currentThing);
                    }
                    else
                    {
                        this.currentCapacitorCharge = 0;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("=== COULD NOT SPAWN !!!! === " + e.Message);
                    continue;
                }
            }

            recallData.Clear();

            // Tell the MapDrawer that here is something that's changed
            Find.CurrentMap.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);

            return !this.dematerializedBuffer.Any();
        }

        private void updatePowerDrain()
        {
            this.power.powerOutputInt = -1000 * this.chargeSpeed;
        }

        #endregion
        
        public override Graphic Graphic
        {
            get
            {
                if (this.HasThingsInBuffer())
                {
                    return Building_Dematerializer.graphicActive;
                }
                else
                {
                    return Building_Dematerializer.graphicInactive;
                }
            }
        }

        public bool UpdateRequiredPower(float extraPower)
        {
            this.power.PowerOutput = -1 * extraPower;
            return true;
        }

        public void EjectLeastMassive()
        {
            // Drop the lightest items first.
            this.dematerializedBuffer.DestroyLeastMassive();
        }
    }
}
