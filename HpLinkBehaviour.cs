using HkmpPouch;
using FightTogether.Events;
using Modding;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System;

namespace FightTogether
{

    public class HpLinkBehaviour : MonoBehaviour
    {
        public HealthManager hm;
        public string entityName;

        int trackedHp;


        void CreateName()
        {
            entityName = gameObject.scene.name + gameObject.name + gameObject.transform.position.x + gameObject.transform.position.y;
        }
        void Start()
        {
            hm = GetComponent<HealthManager>();
            trackedHp = hm.hp;
            CreateName();
            if (hm.isDead)
            {
                return;
            }
            FightTogether.pipeClient.ClientApi.ClientManager.ConnectEvent += SendHealthUpdate;
            FightTogether.pipeClient.On(UpdateHealthEventFactory.Instance).Do<HealthEvent>((e) =>
            {
                if (e.entityName == entityName)
                {
                    SyncHealth(e.health);
                }
            });
            if (FightTogether.enemyHealths.TryGetValue(entityName, out int hp))
            {
                SyncHealth(hp);
            }
            else
            {
                FightTogether.pipeClient.SendToServer(new HealthEvent(HealthOperation.Init, entityName, hm.hp));
            }
        }

        void SendHealthUpdate()
        {
            FightTogether.pipeClient.SendToServer(new HealthEvent(HealthOperation.Update, entityName, hm.hp));
        }

        void OnDestroy()
        {
            SendHealthUpdate();
        }

        void SyncHealth(int hp)
        {
            if (hp <= 0)
            {
                FightTogether.pipeClient.ClientApi.UiManager.ChatBox.AddMessage($"Synced {gameObject.name} by murder");
                hm.Hit(new HitInstance()
                {
                    AttackType = AttackTypes.Nail,
                    Source = HeroController.instance.gameObject,
                    DamageDealt = 9999,
                    Multiplier = 1,
                    MagnitudeMultiplier = 1,
                    CircleDirection = true,
                    IgnoreInvulnerable = true
                });
            }
            else if (hp < hm.hp)
            {
                FightTogether.pipeClient.ClientApi.UiManager.ChatBox.AddMessage($"Synced {gameObject.name} to {hp}");
                hm.ApplyExtraDamage(hm.hp - hp);
            }
            trackedHp = hm.hp;
        }

        void Update()
        {
            if (hm.hp != trackedHp)
            {
                trackedHp = hm.hp;
                SendHealthUpdate();
            }
        }
    }

}
