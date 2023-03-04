using HkmpPouch;
using Modding;
using ESoulLink.Events;
using UnityEngine;

namespace ESoulLink
{

    public class HpLinkBehaviour : MonoBehaviour
    {

        public HealthManager hm;
        public int lastHp, originalHp;
        public string Name;

        private OnEvent OnPoolUpdateEvent;

        bool poolJoined = false;
        void UpdatePool(int hpreduction = 0)
        {
            //relay damage
            var pipeEvent = new ModifyPoolHealthEvent { BossName = Name, ReduceHealthBy = hpreduction };
            ESoulLink.pipeClient.SendToServer(pipeEvent);
        }
        void LeavePool()
        {
            //leave pool
            if (hm.hp > originalHp)
            {
                hm.hp = originalHp; // reset to starting hp as a fail safe
                lastHp = originalHp;
            }
            poolJoined = false;
            ESoulLink.pipeClient.SendToServer(new LeavePoolEvent { BossName = Name });

            // also remove any listeners
            ESoulLink.pipeClient.OnRecieve -= PipeClient_OnRecieve;
        }
        void JoinPool()
        {

            if (poolJoined)
            {
                return;
            }
            if (hm.hp <= 0)
            {
                return;
            }
            //join pool
            ESoulLink.pipeClient.SendToServer(new JoinPoolEvent { BossName = Name, WithHealth = hm.hp });

            poolJoined = true;
            //SharedHealthManager.pipeClient.OnRecieve += PipeClient_OnRecieve;
            OnPoolUpdateEvent = ESoulLink.pipeClient.On(PoolUpdateEventFactory.Instance).Do<PoolUpdateEvent>((pipeEvent) => {
                if (pipeEvent.BossName != Name) { return; }
                UpdateHealth(pipeEvent.CurrentHealth);
            });
        }
        void Awake()
        {
            hm = GetComponent<HealthManager>();
            Name = gameObject.scene.name + gameObject.name;
            lastHp = hm.hp;
            originalHp = hm.hp;
            ESoulLink.pipeClient.ClientApi.ClientManager.ConnectEvent += ClientManager_ConnectEvent; ;
            ESoulLink.pipeClient.ClientApi.ClientManager.DisconnectEvent += ClientManager_DisconnectEvent;
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            JoinPool();
        }
        private void Log(string str)
        {
            ESoulLink.Instance.Log(str);
        }
        private void ClientManager_ConnectEvent()
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) { return; }
            Log("Client Connect");
            JoinPool();
        }

        private void ClientManager_DisconnectEvent()
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) { return; }
            Log("Client Disconnect");
            LeavePool();
        }

        private void UpdateHealth(int Hp)
        {

            lastHp = Hp; // since we do not want to track our own changes
            hm.hp = Hp;

            if (hm.hp <= 0)
            {
                var hI = new HitInstance()
                {
                    AttackType = AttackTypes.Nail,
                    Source = HeroController.instance.gameObject,
                    DamageDealt = 9999,
                    Multiplier = 1,
                    MagnitudeMultiplier = 1,
                    CircleDirection = true,
                    IgnoreInvulnerable = true
                };
                if (hI.Source == null)
                {
                    ESoulLink.Instance.Log("Source is null, using Hero");
                    hI.Source = HeroController.instance.gameObject;
                }

                //SharedHealthManager.pipeClient.OnRecieve -= PipeClient_OnRecieve;
                if (OnPoolUpdateEvent != null && !OnPoolUpdateEvent.Destroyed)
                {
                    OnPoolUpdateEvent.Destroy();
                }
                if (hm != null)
                {
                    ReflectionHelper.CallMethod<HealthManager>(hm, "TakeDamage", hI);
                }
            }
        }
        private void PipeClient_OnRecieve(object sender, ReceivedEventArgs e)
        {
            if (e.Data.EventName != "UpdatedPool") { return; }
            var UpdateEvent = (PoolUpdateEvent)PoolUpdateEventFactory.Instance.FromSerializedString(e.Data.EventData);
            if (Name != UpdateEvent.BossName) { return; }
            UpdateHealth(UpdateEvent.CurrentHealth);

        }

        void OnDestroy()
        {
            LeavePool();
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            ESoulLink.pipeClient.ClientApi.ClientManager.ConnectEvent -= ClientManager_ConnectEvent; ;
            ESoulLink.pipeClient.ClientApi.ClientManager.DisconnectEvent -= ClientManager_DisconnectEvent;
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self == null || gameObject == null)
            {
                return;
            }
            if (self.gameObject == null)
            {
                return;
            }
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy || self.gameObject != gameObject)
            {
                orig(self, hitInstance);
                return;
            }
            JoinPool();
            var oldHp = hm.hp;
            orig(self, hitInstance);
            UpdatePool((oldHp - hm.hp));
        }

        void FixedUpdate()
        {
            if (lastHp != hm.hp)
            {
                var difference = lastHp - hm.hp;
                Log($"Correcting hp for an unknown case, old: {lastHp} new: {hm.hp} difference: {difference}");
                UpdatePool(difference);
                lastHp = hm.hp;
            }
        }
    }

}
