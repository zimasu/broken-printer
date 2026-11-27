using Klei.AI;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zolibrary.Logging;
using static DistributionPlatform;
using static StateMachine;

namespace DuplicantLifecycles
{
    public class AgingTrait : StateMachineComponent<AgingTrait.StatesInstance>
    {
        private float personalMaxAge;
        private float youthCutoff;
        private float middleCutoff;
        private float elderlyCutoff;

        private bool usePercentage;
        private float customBase;
        private int timeToDieCycleCheck;

        private float immortalAttributeMultiplier;
        private List<AttributeModifier> immortalAttributeModifiers;
        private float youthAttributeMultiplier;
        private List<AttributeModifier> youthAttributeModifiers;
        private float middleAttributeMultiplier;
        private List<AttributeModifier> middleAttributeModifiers;
        private float elderlyAttributeMultiplier;
        private List<AttributeModifier> elderlyAttributeModifiers;
        private float dyingAttributeMultiplier;
        private List<AttributeModifier> dyingAttributeModifiers;

        protected override void OnSpawn()
        {
            this.timeToDieCycleCheck = -1;

            // Deterministic random lifespan between 14 and 28 cycles per clone (persists across save/load)
            MinionIdentity identity = this.GetComponent(typeof(MinionIdentity)) as MinionIdentity;
            int seed = identity != null ? (identity.name.GetHashCode() ^ Mathf.RoundToInt(identity.arrivalTime * 100f)) : GetInstanceID();
            System.Random rng = new System.Random(seed);
            this.personalMaxAge = 14f + (float)(rng.NextDouble() * 14.0); // 14.0 to 28.0 cycles

            // Scale life stages proportionally to their personal 14-28 cycle lifespan
            this.youthCutoff = this.personalMaxAge * 0.45f;   // First ~45% of life (Fresh Copy)
            this.middleCutoff = this.personalMaxAge * 0.75f;  // 45% - 75% (Unstable DNA)
            this.elderlyCutoff = this.personalMaxAge * 0.90f; // 75% - 90% (Degrading Copy; last 10% = Terminal)

            this.immortalAttributeMultiplier = DuplicantLifecycleConfigChecker.ImmortalAttributeMultiplier;
            this.youthAttributeMultiplier = DuplicantLifecycleConfigChecker.YouthAttributeMultiplier;
            this.middleAttributeMultiplier = DuplicantLifecycleConfigChecker.MiddleAttributeMultiplier;
            this.elderlyAttributeMultiplier = DuplicantLifecycleConfigChecker.ElderlyAttributeMultiplier;
            this.dyingAttributeMultiplier = DuplicantLifecycleConfigChecker.DyingAttributeMultiplier;
            this.usePercentage = DuplicantLifecycleConfigChecker.UsePercentage;
            this.customBase = DuplicantLifecycleConfigChecker.CustomBase;

            this.SetupModifierLists();
            this.smi.StartSM();
        }

        private void SetupModifierLists()
        {
            this.immortalAttributeModifiers = this.CalculateAgeModifiers(immortalAttributeMultiplier);
            this.youthAttributeModifiers = this.CalculateAgeModifiers(youthAttributeMultiplier);
            this.middleAttributeModifiers = this.CalculateAgeModifiers(middleAttributeMultiplier);
            this.elderlyAttributeModifiers = this.CalculateAgeModifiers(elderlyAttributeMultiplier);
            this.dyingAttributeModifiers = this.CalculateAgeModifiers(dyingAttributeMultiplier);
        }

        private List<AttributeModifier> CalculateAgeModifiers(float multiplier)
        {
            return new List<AttributeModifier>
            {
                new AttributeModifier("Construction", this.GetModifierDelta("Construction", multiplier), (string)DuplicantLifecycleStrings.AgingNAME, false, false, false),
                new AttributeModifier("Digging",      this.GetModifierDelta("Digging", multiplier),      (string)DuplicantLifecycleStrings.AgingNAME, false, false, false),
                new AttributeModifier("Machinery",    this.GetModifierDelta("Machinery", multiplier),    (string)DuplicantLifecycleStrings.AgingNAME, false, false, false),
                new AttributeModifier("Athletics",    this.GetModifierDelta("Athletics", multiplier),    (string)DuplicantLifecycleStrings.AgingNAME, false, false, false),
                new AttributeModifier("Learning",     this.GetModifierDelta("Learning", multiplier),     (string)DuplicantLifecycleStrings.AgingNAME, false, false, false),
                new AttributeModifier("Cooking",      this.GetModifierDelta("Cooking", multiplier),      (string)DuplicantLifecycleStrings.AgingNAME, false, false, false),
                new AttributeModifier("Art",          this.GetModifierDelta("Art", multiplier),          (string)DuplicantLifecycleStrings.AgingNAME, false, false, false),
                new AttributeModifier("Strength",     this.GetModifierDelta("Strength", multiplier),     (string)DuplicantLifecycleStrings.AgingNAME, false, false, false),
                new AttributeModifier("Caring",       this.GetModifierDelta("Caring", multiplier),       (string)DuplicantLifecycleStrings.AgingNAME, false, false, false),
                new AttributeModifier("Botanist",     this.GetModifierDelta("Botanist", multiplier),     (string)DuplicantLifecycleStrings.AgingNAME, false, false, false),
                new AttributeModifier("Ranching",     this.GetModifierDelta("Ranching", multiplier),     (string)DuplicantLifecycleStrings.AgingNAME, false, false, false)
            };
        }

        private float GetModifierDelta(string id, float multiplier)
        {
            float base_value = this.customBase;

            if (this.usePercentage)
            {
                AttributeInstance i = this.gameObject.GetAttributes().Get(id);
                if (i != null && i.GetTotalValue() > 0)
                    base_value = i.GetTotalValue();
                else
                    base_value = 0f;
            }
            return Math.Abs(base_value) * multiplier;
        }

        private float CurrentMinionAge()
        {
            MinionIdentity identity = this.smi.master.GetComponent(typeof(MinionIdentity)) as MinionIdentity;
            if (identity == null) return 0f;
            float currentCycle = GameClock.Instance.GetCycle();
            return currentCycle - identity.arrivalTime;
        }

        protected bool IsImmortal()
        {
            // Bionic Duplicants (DLC3) are synthetic and immune to biological DNA collapse
            if (this.gameObject.HasTag(GameTags.Minions.Models.Bionic))
                return true;

            WorkerBase worker = this.GetComponent<WorkerBase>();
            if (worker == null)
                return false;

            Traits traits = worker.GetComponent<Traits>();
            return traits != null && traits.HasTrait(DuplicantLifecycleStrings.ImmortalID);
        }

        protected void ApplyImmortalModifiers() => this.ApplyModifiers(this.immortalAttributeModifiers);
        protected bool IsYouthful() => CurrentMinionAge() < this.youthCutoff;
        protected void ApplyYouthfulModifiers() => this.ApplyModifiers(this.youthAttributeModifiers);
        protected bool IsMiddleAged() => CurrentMinionAge() >= this.youthCutoff && CurrentMinionAge() < this.middleCutoff;
        protected void ApplyMiddleAgedModifiers() => this.ApplyModifiers(this.middleAttributeModifiers);
        protected bool IsElderly() => CurrentMinionAge() >= this.middleCutoff && CurrentMinionAge() < this.elderlyCutoff;
        protected void ApplyElderlyModifiers() => this.ApplyModifiers(this.elderlyAttributeModifiers);
        protected bool IsDying() => CurrentMinionAge() >= this.elderlyCutoff;
        protected bool IsDead() => this.gameObject.HasTag(GameTags.Dead);
        protected void ApplyDyingModifiers() => this.ApplyModifiers(this.dyingAttributeModifiers);

        protected bool TimeToDie()
        {
            if (!DuplicantLifecycleConfigChecker.EnableDeath || IsImmortal())
                return false;

            float minionAge = this.CurrentMinionAge();

            // Guaranteed expiration once their 14-28 cycle personal lifespan is reached
            if (minionAge >= this.personalMaxAge)
            {
                this.KillDuplicant();
                return true;
            }

            if (this.timeToDieCycleCheck == GameClock.Instance.GetCycle())
                return false;

            this.timeToDieCycleCheck = GameClock.Instance.GetCycle();

            // Escalating chance during the final 10% Terminal window
            float cyclesLeft = Mathf.Max(1f, this.personalMaxAge - minionAge);
            if (UnityEngine.Random.Range(0f, cyclesLeft + 1f) <= 0.35f)
            {
                this.KillDuplicant();
                return true;
            }

            return false;
        }

        private void ApplyModifiers(List<AttributeModifier> toAdd)
        {
            Attributes attributes = this.gameObject.GetAttributes();
            foreach (AttributeModifier am in toAdd)
                attributes.Add(am);
        }

        protected void RemoveModifiers(List<AttributeModifier> toRemove)
        {
            Attributes attributes = this.gameObject.GetAttributes();
            foreach (AttributeModifier am in toRemove)
                attributes.Remove(am);
        }

        public void KillDuplicant()
        {
            this.RemoveModifiers(this.dyingAttributeModifiers);
            DeathMonitor.Instance deathSMI = this.gameObject.GetSMI<DeathMonitor.Instance>();
            if (deathSMI != null)
            {
                deathSMI.Kill(DuplicantLifeCyclePatches.oldAgeDeath);
            }
        }

        public class StatesInstance : GameStateMachine<AgingTrait.States, AgingTrait.StatesInstance, AgingTrait, object>.GameInstance
        {
            public StatesInstance(AgingTrait master) : base(master) { }
        }

        public class States : GameStateMachine<AgingTrait.States, AgingTrait.StatesInstance, AgingTrait>
        {
            public State noage;
            public State immortal;
public State youthful;
            public State middleaged;
            public State elderly;
            public State dying;
            public State dead;
            public override void InitializeStates(out StateMachine.BaseState default_state)
            {
                default_state = this.noage;
                this.noage
                .Transition(this.dead, smi => smi.master.IsDead(), UpdateRate.SIM_200ms)
                .Transition(this.immortal, smi => smi.master.IsImmortal(), UpdateRate.SIM_200ms)
                .Transition(this.youthful, smi => smi.master.IsYouthful(), UpdateRate.SIM_200ms)
                .Transition(this.middleaged, smi => smi.master.IsMiddleAged(), UpdateRate.SIM_200ms)
                .Transition(this.elderly, smi => smi.master.IsElderly(), UpdateRate.SIM_200ms)
                .Transition(this.dying, smi => smi.master.IsDying(), UpdateRate.SIM_200ms);
                this.immortal
                .Enter("Immortal", smi => smi.master.ApplyImmortalModifiers())
                .Exit("NotImmortal", smi => smi.master.RemoveModifiers(smi.master.immortalAttributeModifiers))
                .ToggleStatusItem(DuplicantLifecycleStrings.Immortal, null)
                .ToggleExpression(Db.Get().Expressions.Relief, null)
                .TagTransition(GameTags.Dead, this.dead);
                this.youthful
                .Enter("Youthful", smi => smi.master.ApplyYouthfulModifiers())
                .Exit("NotYouthful", smi => smi.master.RemoveModifiers(smi.master.youthAttributeModifiers))
                .ToggleStatusItem(DuplicantLifecycleStrings.AgingYouth, null)
                                .Transition(this.middleaged, smi => smi.master.IsMiddleAged(), UpdateRate.SIM_4000ms)
                .Transition(this.immortal, smi => smi.master.IsImmortal(), UpdateRate.SIM_1000ms)
                .TagTransition(GameTags.Dead, this.dead);
                this.middleaged
                .Enter("MiddleAged", smi => smi.master.ApplyMiddleAgedModifiers())
                .Exit("NotMiddleAged", smi => smi.master.RemoveModifiers(smi.master.middleAttributeModifiers))
                .ToggleStatusItem(DuplicantLifecycleStrings.AgingMiddle, null)
                                .Transition(this.elderly, smi => smi.master.IsElderly(), UpdateRate.SIM_4000ms)
                .Transition(this.immortal, smi => smi.master.IsImmortal(), UpdateRate.SIM_1000ms)
                .TagTransition(GameTags.Dead, this.dead);
                this.elderly
                .Enter("Elderly", smi => smi.master.ApplyElderlyModifiers())
                .Exit("NotElderly", smi => smi.master.RemoveModifiers(smi.master.elderlyAttributeModifiers))
                .ToggleStatusItem(DuplicantLifecycleStrings.AgingElderly, null)
                .ToggleExpression(Db.Get().Expressions.Tired, null)
                .Transition(this.dying, smi => smi.master.IsDying(), UpdateRate.SIM_4000ms)
                .Transition(this.immortal, smi => smi.master.IsImmortal(), UpdateRate.SIM_1000ms)
                .TagTransition(GameTags.Dead, this.dead);
                this.dying
                .Enter("Dying", smi => smi.master.ApplyDyingModifiers())
                .ToggleStatusItem(DuplicantLifecycleStrings.AgingDying, null)
                .ToggleExpression(Db.Get().Expressions.Sick, null)
                .Transition(this.dead, smi => smi.master.TimeToDie(), UpdateRate.SIM_4000ms)
                .Transition(this.immortal, smi => smi.master.IsImmortal(), UpdateRate.SIM_1000ms)
                .TagTransition(GameTags.Dead, this.dead);
                this.dead.Enter("Dead", smi => smi.StopSM("Death"));
            }
        }
    }
}
