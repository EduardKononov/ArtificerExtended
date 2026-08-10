using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage;
using EntityStates.Toolbot;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using R2API.Networking.Interfaces;
using R2API.Networking;
using static R2API.Networking.NetworkingHelpers;

namespace ArtificerExtended.States
{
    class PolarVortexBase : GenericCharacterMain, ISkillState
    {
        public static GameObject muzzleflashEffect => Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Mage.MuzzleflashMageIceLarge_prefab).WaitForCompletion();//FlyUpState.muzzleflashEffect;

        internal SkillSlot _activatorSkillSlot;
        internal bool authorityProceedToNextState = false;
        internal bool addedFallImmunity = false;
        internal bool crit = false;
        internal bool _vortexEnding = false;
        internal bool vortexEnding
        {
            get => characterBody.HasBuff(_1FrostbiteSkill.vortexEndingBuff);
            set
            {
                if (_vortexEnding == true)
                    return;

                if (!_vortexEnding)
                    characterBody.AddTimedBuffAuthority(_1FrostbiteSkill.vortexEndingBuff.buffIndex, 20);
                _vortexEnding = value;
            }
        }
        public GenericSkill activatorSkillSlot { get; set; }

        protected virtual void SetNextState()
        {
            authorityProceedToNextState = false;
            outer.SetNextStateToMain();
        }
        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(addedFallImmunity);
            writer.Write(crit);
            writer.Write((int)_activatorSkillSlot);
        }
        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            addedFallImmunity = reader.ReadBoolean();
            crit = reader.ReadBoolean();
            _activatorSkillSlot = (SkillSlot)reader.ReadInt32();
            if (activatorSkillSlot == null)
            {
                switch (_activatorSkillSlot)
                {
                    case SkillSlot.Primary:
                        activatorSkillSlot = skillLocator.primary;
                        break;
                    case SkillSlot.Secondary:
                        activatorSkillSlot = skillLocator.secondary;
                        break;
                    case SkillSlot.Utility:
                        activatorSkillSlot = skillLocator.utility;
                        break;
                    case SkillSlot.Special:
                        activatorSkillSlot = skillLocator.special;
                        break;
                }
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (NetworkServer.active && !base.characterBody.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreFallDamage))
            {
                base.characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                addedFallImmunity = true;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.characterBody)
            {
                base.characterBody.SetAimTimer(1f);
                if (base.isAuthority && base.characterBody.HasBuff(DLC2Content.Buffs.DisableAllSkills.buffIndex))
                {
                    outer.SetNextStateToMain();
                }
            }
        }
        public override void OnExit()
        {
            base.OnExit();
            if (!authorityProceedToNextState && base.isAuthority)
            {
                new SyncVortexClear(this.characterBody.gameObject, this.addedFallImmunity)
                    .Send(R2API.Networking.NetworkDestination.Server);
            }
            if (NetworkServer.active)
            {
                characterBody.ClearTimedBuffs(_1FrostbiteSkill.vortexEndingBuff);
            }
        }

        protected void StopHover()
        {
            if (base.isAuthority)
            {
                EntityStateMachine entityStateMachine2 = EntityStateMachine.FindByCustomName(base.gameObject, "Jet");
                if (entityStateMachine2 == null || entityStateMachine2.state.GetType() != typeof(JetpackOn))
                {
                    return;
                }
                entityStateMachine2.SetNextStateToMain();
            }
        }

        /// <summary>
        /// transmits muzzle flash effect and sends vortex blast message to server
        /// </summary>
        public void InflictSnowAuthority()
        {
            if (!this.isAuthority)
            {
                return;
            }
            EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleLeft", true);
            EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleRight", true);
            new SyncVortexBlast(characterBody.corePosition, gameObject, characterBody.damage * _1FrostbiteSkill.blizzardDamageCoefficient, this.crit)
                .Send(R2API.Networking.NetworkDestination.Server);
            return;

            #region x3
            if (!NetworkServer.active)
                return;

            float damage = characterBody.damage * _1FrostbiteSkill.blizzardDamageCoefficient;
            RainrotSharedUtils.Frost.FrostUtilsModule.CreateIceBlast(characterBody,
                FlyUpState.blastAttackForce, damage, _1FrostbiteSkill.blizzardProcCoefficient,
                _1FrostbiteSkill.blizzardRadius, this.crit, characterBody.corePosition, true, DamageSource.Special);

            return;
            EffectManager.SpawnEffect(_1FrostbiteSkill.novaEffectPrefab, new EffectData
            {
                origin = base.transform.position,
                scale = _1FrostbiteSkill.blizzardRadius
            }, true);
            BlastAttack blastAttack = new BlastAttack();
            blastAttack.radius = _1FrostbiteSkill.blizzardRadius;
            blastAttack.procCoefficient = _1FrostbiteSkill.blizzardProcCoefficient;
            blastAttack.position = base.transform.position;
            blastAttack.attacker = base.gameObject;
            blastAttack.crit = crit;
            blastAttack.baseDamage = base.characterBody.damage * _1FrostbiteSkill.blizzardDamageCoefficient;
            blastAttack.falloffModel = BlastAttack.FalloffModel.None;
            blastAttack.damageType = new DamageTypeCombo(DamageTypeExtended.Generic, DamageTypeExtended.Frost, DamageSource.Special);
            blastAttack.baseForce = 1500;
            blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
            blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;

            blastAttack.Fire();
            #endregion
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Stun;
        }
    }
}
