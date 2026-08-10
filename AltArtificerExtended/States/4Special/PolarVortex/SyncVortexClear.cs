using ArtificerExtended.Skills;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.States
{
    class SyncVortexClear : INetMessage
    {
        GameObject bodyObject;
        bool removeFallImmunity;
        public SyncVortexClear()
        {
        }
        public SyncVortexClear(GameObject bodyObject, bool removeFallImmunity)
        {
            this.bodyObject = bodyObject;
            this.removeFallImmunity = removeFallImmunity;
        }
        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.bodyObject);
            writer.Write(this.removeFallImmunity);
        }
        public void Deserialize(NetworkReader reader)
        {
            this.bodyObject = reader.ReadGameObject();
            this.removeFallImmunity = reader.ReadBoolean();
        }

        public void OnReceived()
        {
            if (!NetworkServer.active)
                return;

            if(bodyObject.TryGetComponent(out CharacterBody body))
            {
                while (body.HasBuff(_1FrostbiteSkill.artiIceShield))
                    body.RemoveBuff(_1FrostbiteSkill.artiIceShield);

                if (this.removeFallImmunity)
                    body.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            }
        }
    }
}
