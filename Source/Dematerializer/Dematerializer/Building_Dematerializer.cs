using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;
using UnityEngine;
using RimWorld;
using Verse.AI;

namespace BetterRimworlds.Dematerializer
{
    [StaticConstructorOnStartup]
    class Building_Dematerializer : Building_Storage, IThingHolder
    {
        protected DematerializerBuffer DematerializerBuffer;

        protected static Texture2D UI_ADD_RESOURCES;
        protected static Texture2D UI_ADD_COLONIST;

        protected static Texture2D UI_GATE_IN;
        protected static Texture2D UI_GATE_OUT;

        protected static Texture2D UI_POWER_UP;
        protected static Texture2D UI_POWER_DOWN;

        static Graphic graphicActive;
        static Graphic graphicInactive;

        CompPowerTrader power;

        int currentCapacitorCharge = 1000;
        int requiredCapacitorCharge = 1000;
        int chargeSpeed = 1;

        protected Map currentMap;

        static Building_Dematerializer()
        {
            UI_ADD_RESOURCES = ContentFinder<Texture2D>.Get("UI/ADD_RESOURCES", true);
            UI_ADD_COLONIST = ContentFinder<Texture2D>.Get("UI/ADD_COLONIST", true);

            UI_GATE_IN = ContentFinder<Texture2D>.Get("UI/StargateGUI-In", true);
            UI_GATE_OUT = ContentFinder<Texture2D>.Get("UI/StargateGUI-Out", true);

            UI_POWER_UP = ContentFinder<Texture2D>.Get("UI/PowerUp", true);
            UI_POWER_DOWN = ContentFinder<Texture2D>.Get("UI/PowerDown", true);

            GraphicRequest requestActive = new GraphicRequest(Type.GetType("Graphic_Single"), "Things/Buildings/Stargate-Active",   ShaderDatabase.DefaultShader, new Vector2(3, 3), Color.white, Color.white, new GraphicData(), 0, null);

            graphicActive = new Graphic_Single();
            graphicActive.Init(requestActive);

            GraphicRequest requestInactive = new GraphicRequest(Type.GetType("Graphic_Single"), "Things/Buildings/Stargate", ShaderDatabase.DefaultShader, new Vector2(3, 3), Color.white, Color.white, new GraphicData(), 0, null);

            graphicInactive = new Graphic_Single();
            graphicInactive.Init(requestInactive);
        }

        public Building_Dematerializer()
        {
            this.DematerializerBuffer = new DematerializerBuffer(this);
        }

        #region Override

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            this.currentMap = map;
            base.SpawnSetup(map, respawningAfterLoad);

            this.power = base.GetComp<CompPowerTrader>();
            
            Log.Warning("Found some things in the Dematerializer's buffer: " + this.DematerializerBuffer.Count);
        }

        // For displaying contents to the user.
        public ThingOwner GetDirectlyHeldThings() => this.DematerializerBuffer;

        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, (IList<Thing>) this.GetDirectlyHeldThings());

        // Saving game
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref currentCapacitorCharge, "currentCapacitorCharge");
            Scribe_Values.Look<int>(ref requiredCapacitorCharge, "requiredCapacitorCharge");
            Scribe_Values.Look<int>(ref chargeSpeed, "chargeSpeed");

            Scribe_Deep.Look<DematerializerBuffer>(ref this.DematerializerBuffer, "stargateBuffer", new object[]
            {
                this
            });
        }

        protected void BaseTickRare()
        {
            base.TickRare();
        }

        public override void TickRare()
        {
            base.TickRare();
            if (this.power.PowerOn)
            {
                currentCapacitorCharge += chargeSpeed;
            }

            // Stop using power if it's full.
            if (currentCapacitorCharge >= requiredCapacitorCharge)
            {
                currentCapacitorCharge = requiredCapacitorCharge;
            }

            if (this.currentCapacitorCharge < 0)
            {
                this.currentCapacitorCharge = 0;
                this.chargeSpeed = 0;
                this.updatePowerDrain();
            }
        }

        #endregion

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
                act.action = () => this.AddResources();
                act.icon = UI_ADD_RESOURCES;
                act.defaultLabel = "Add Resources";
                act.defaultDesc = "Add Resources";
                act.activateSound = SoundDef.Named("Click");
                //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                //act.groupKey = 689736;
                yield return act;
            }

            if (true)
            {
                Command_Action act = new Command_Action();
                //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                act.action = () => this.AddColonist();
                act.icon = UI_ADD_COLONIST;
                act.defaultLabel = "Add Colonist";
                act.defaultDesc = "Add Colonist";
                act.activateSound = SoundDef.Named("Click");
                //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                //act.groupKey = 689736;
                yield return act;
            }

            if (true)
            {
                Command_Action act = new Command_Action();
                //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                act.action = () => this.StargateDialOut();
                act.icon = UI_GATE_OUT;
                act.defaultLabel = "Dial Out";
                act.defaultDesc = "Dial Out";
                act.activateSound = SoundDef.Named("Click");
                //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                //act.groupKey = 689736;
                yield return act;
            }

            if (true)
            {
                Command_Action act = new Command_Action();
                //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                act.action = () => this.StargateRecall();
                act.icon = UI_GATE_IN;
                act.defaultLabel = "Recall";
                act.defaultDesc = "Recall";
                act.activateSound = SoundDef.Named("Click");
                //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                //act.groupKey = 689736;
                yield return act;
            }

            if (true)
            {
                Command_Action act = new Command_Action
                {
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    action = () => this.PowerRateIncrease(),
                    icon = UI_POWER_UP,
                    defaultLabel = "Increase Power",
                    defaultDesc = "Increase Power",
                    activateSound = SoundDef.Named("Click")
                };
                //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                //act.groupKey = 689736;
                yield return act;
            }

            if (true)
            {
                Command_Action act = new Command_Action
                {
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    action = () => this.PowerRateDecrease(),
                    icon = UI_POWER_DOWN,
                    defaultLabel = "Decrease Power",
                    defaultDesc = "Decrease Power",
                    activateSound = SoundDef.Named("Click")
                };
                //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                //act.groupKey = 689736;
                yield return act;
            }

        }

        public void AddResources()
        {
            if (this.fullyCharged)
            {
                Thing foundThing = Enhanced_Development.Utilities.Utilities.FindItemThingsNearBuilding(this, 10000, this.currentMap);

                if (foundThing != null)
                {
                    if (foundThing.Spawned && this.DematerializerBuffer.Count < 500)
                    {
                        List<Thing> thingList = new List<Thing>();
                        //thingList.Add(foundThing);
                        this.DematerializerBuffer.TryAdd(foundThing);

                        //Building_OrbitalRelay.listOfThingLists.Add(thingList);

                        //Recursively Call to get Everything
                        this.AddResources();
                    }
                }

                // Tell the MapDrawer that here is something thats changed
                Find.CurrentMap.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
            }
            else
            {
                Messages.Message("Insufficient Power to add Resources", MessageTypeDefOf.RejectInput);
            }

        }

        public void AddColonist()
        {
            if (this.fullyCharged)
            {
                //Log.Message("CLick AddColonist");
                IEnumerable<Pawn> closePawns = Enhanced_Development.Utilities.Utilities.findPawnsInColony(this.Position, 1000);

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

                            this.DematerializerBuffer.TryAdd(currentPawn);
                        }
                    }
                }

                // Tell the MapDrawer that here is something thats changed
                Find.CurrentMap.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
            }
            else
            {
                Messages.Message("Insufficient Power to add Colonist", MessageTypeDefOf.RejectInput);
            }
        }

        public void StargateDialOut()
        {
            if (!this.fullyCharged)
            {
                Messages.Message("Insufficient power to establish connection.", MessageTypeDefOf.RejectInput);
                return;
            }

            this.DematerializerBuffer.Clear();

            // Tell the MapDrawer that here is something thats changed
            Find.CurrentMap.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);

            this.currentCapacitorCharge -= this.requiredCapacitorCharge;
        }

        public bool HasThingsInBuffer()
        {
            return this.DematerializerBuffer.Count > 0;
        }

        public List<Thing> Teleport()
        {
            var itemsToTeleport = new List<Thing>();
            itemsToTeleport.AddRange(this.DematerializerBuffer);
            this.DematerializerBuffer.Clear();

            // Tell the MapDrawer that here is something thats changed
            Find.CurrentMap.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);

            this.currentCapacitorCharge -= this.requiredCapacitorCharge;

            return itemsToTeleport;
        }

        public virtual bool StargateRecall()
        {
            // List<Thing> inboundBuffer = (List<Thing>)null;
            var inboundBuffer = new List<Thing>();

            Log.Warning("Inbound Buffer Count? " + inboundBuffer.Count);
            Messages.Message("Incoming wormhole!", MessageTypeDefOf.PositiveEvent);

            foreach (Thing currentThing in inboundBuffer)
            {
                currentThing.thingIDNumber = -1;
                Verse.ThingIDMaker.GiveIDTo(currentThing);

                // If it's an equippable object, like a gun, reset its verbs or ANY colonist that equips it *will* go insane...
                // This is actually probably the root cause of Colonist Insanity (holding an out-of-phase item with IDs belonging
                // to an alternate dimension). This is the equivalent of how Olivia goes insane in the TV series Fringe.
                if (currentThing is ThingWithComps item)
                {
                    item.InitializeComps();
                }

                if (currentThing.def.CanHaveFaction)
                {
                    currentThing.SetFactionDirect(Faction.OfPlayer);
                }
                
                // Fixes a bug w/ support for B19+ and later where colonists go *crazy*
                // if they enter a Stargate after they've ever been drafted.
                if (currentThing is Pawn pawn)
                {
                    // Carry over injuries, sicknesses, addictions, and artificial body parts.
                    var hediffSet = pawn.health.hediffSet;

                    pawn.health = new Pawn_HealthTracker(pawn);

                    foreach (var hediff in hediffSet.hediffs.ToList())
                    {
                        pawn.health.AddHediff(hediff.def, hediff.Part);
                    }

                    if (pawn.IsColonist)
                    {
                        pawn.verbTracker = new VerbTracker(pawn);
                        pawn.carryTracker = new Pawn_CarryTracker(pawn);
                        pawn.rotationTracker = new Pawn_RotationTracker(pawn);
                        pawn.thinker = new Pawn_Thinker(pawn);
                        pawn.mindState = new Pawn_MindState(pawn);
                        pawn.jobs = new Pawn_JobTracker(pawn);
                        pawn.ownership = new Pawn_Ownership(pawn);
                        pawn.drafter = new Pawn_DraftController(pawn);
                        pawn.natives = null;
                        // pawn.outfits = new Pawn_OutfitTracker(pawn);
                        pawn.pather = new Pawn_PathFollower(pawn);
                        // pawn.records = new Pawn_RecordsTracker(pawn);
                        // pawn.relations = new Pawn_RelationsTracker(pawn);
                        pawn.caller = new Pawn_CallTracker(pawn);
                        // pawn.needs = new Pawn_NeedsTracker(pawn);
                        pawn.drugs = new Pawn_DrugPolicyTracker(pawn);
                        pawn.interactions = new Pawn_InteractionsTracker(pawn);
                        pawn.stances = new Pawn_StanceTracker(pawn);
                        // pawn.story = new Pawn_StoryTracker(pawn);
                        // pawn.playerSettings = new Pawn_PlayerSettings(pawn);
                        // pawn.psychicEntropy = new Pawn_PsychicEntropyTracker(pawn);
                        // pawn.workSettings = new Pawn_WorkSettings(pawn);

                        pawn.meleeVerbs = new Pawn_MeleeVerbs(pawn);

                        pawn.skills.SkillsTick();
                        // Reset Skills Since Midnight.
                        foreach (SkillRecord skill in pawn.skills.skills)
                        {
                            skill.xpSinceMidnight = 0;
                            //lastXpSinceMidnightResetTimestamp
                        }
                    }

                    if (pawn.RaceProps.ToolUser)
                    {
                        if (pawn.equipment == null)
                            pawn.equipment = new Pawn_EquipmentTracker(pawn);
                        if (pawn.apparel == null)
                            pawn.apparel = new Pawn_ApparelTracker(pawn);

                        // Reset their equipped weapon's verbTrackers as well, or they'll go insane if they're carrying an out-of-phase weapon...
                        if (pawn.equipment.Primary != null)
                        {
                            pawn.equipment.Primary.InitializeComps();
                            pawn.equipment.PrimaryEq.verbTracker = new VerbTracker(pawn);
                            // pawn.equipment.PrimaryEq.verbTracker.AllVerbs.Add(new Verb_Shoot());
                        }

                        // Quickly draft and undraft the Colonist. This will cause them to become aware of the newly-in-phase weapon they are holding,
                        // if any. This is effectively the cure of Stargate Insanity.
                        pawn.drafter.Drafted = true;
                        pawn.drafter.Drafted = false;

                    }               

                    // Remove memories or they will go insane...
                    if (pawn.RaceProps.Humanlike)
                    {
                        pawn.guest = new Pawn_GuestTracker(pawn);
                        // pawn.guilt = new Pawn_GuiltTracker(pawn);
                        pawn.abilities = new Pawn_AbilityTracker(pawn);
                        pawn.needs.mood.thoughts.memories = new MemoryThoughtHandler(pawn);
                    }
                    
                    // Give them a brief psychic shock so that they will be given proper Melee Verbs and not act like a Visitor.
                    // Hediff shock = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, pawn, null);
                    // pawn.health.AddHediff(shock, null, null);
                    PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, true);
                }

                GenPlace.TryPlaceThing(currentThing, this.Position + new IntVec3(0, 0, -2), this.currentMap, ThingPlaceMode.Near);
            }

            inboundBuffer.Clear();

            // Tell the MapDrawer that here is something that's changed
            Find.CurrentMap.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);

            return true;
        }

        private void PowerStopUsing()
        {
            this.chargeSpeed = 0;
            this.updatePowerDrain();
        }

        private void PowerRateIncrease()
        {
            this.chargeSpeed += 1;
            this.updatePowerDrain();
        }

        private void PowerRateDecrease()
        {
            this.chargeSpeed -= 1;
            this.updatePowerDrain();
        }

        private void updatePowerDrain()
        {
            this.power.powerOutputInt = -1000 * this.chargeSpeed;
        }

        #endregion

        #region Graphics-text

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

        public override string GetInspectString()
        {
            return base.GetInspectString() + "\n"
                + "Buffer Items: " + this.DematerializerBuffer.Count + " / 500\n"
                + "Capacitor Charge: " + this.currentCapacitorCharge + " / " + this.requiredCapacitorCharge;
        }

        #endregion
    }
}
