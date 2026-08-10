using ArtificerExtended.Skills;
using EntityStates.Mage;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.States
{
    class SyncVortexBlast : INetMessage
    {
        Vector3 position;
        bool isCrit;
        double damage;
        GameObject attackerObject;
        public SyncVortexBlast()
        {
        }
        public SyncVortexBlast(Vector3 position, GameObject attackerObject, double damage, bool isCrit)
        {
            this.position = position;
            this.attackerObject = attackerObject;
            this.damage = damage;
            this.isCrit = isCrit;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.position);
            writer.Write(this.attackerObject);
            writer.Write(this.damage);
            writer.Write(this.isCrit);
        }
        public void Deserialize(NetworkReader reader)
        {
            this.position = reader.ReadVector3();
            this.attackerObject = reader.ReadGameObject();
            this.damage = reader.ReadDouble();
            this.isCrit = reader.ReadBoolean();
        }

        public void OnReceived()
        {
            if (!NetworkServer.active)
                return;

            if(attackerObject.TryGetComponent(out CharacterBody attackerBody))
            {
                RainrotSharedUtils.Frost.FrostUtilsModule.CreateIceBlast(attackerBody,
                    FlyUpState.blastAttackForce, (float)this.damage, _1FrostbiteSkill.blizzardProcCoefficient,
                    _1FrostbiteSkill.blizzardRadius, this.isCrit, this.position, true, DamageSource.Special);
            }
        }
    }
}
