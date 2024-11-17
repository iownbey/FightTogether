using HkmpPouch;
using Modding;
using FightTogether.Events;
using UnityEngine;

namespace FightTogether
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
            FightTogether.pipeClient.SendToServer(pipeEvent);
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
            FightTogether.pipeClient.SendToServer(new LeavePoolEvent { BossName = Name });

            // also remove any listeners
            //ESoulLink.pipeClient.OnRecieve -= PipeClient_OnRecieve;
            if (OnPoolUpdateEvent != null && !OnPoolUpdateEvent.Destroyed)
            {
                OnPoolUpdateEvent.Destroy();
            }
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
            FightTogether.pipeClient.SendToServer(new JoinPoolEvent { BossName = Name, WithHealth = hm.hp });

            poolJoined = true;
            //SharedHealthManager.pipeClient.OnRecieve += PipeClient_OnRecieve;
            OnPoolUpdateEvent = FightTogether.pipeClient.On(PoolUpdateEventFactory.Instance).Do<PoolUpdateEvent>((pipeEvent) =>
            {
                if (pipeEvent.BossName != Name) { return; }
                UpdateHealth(pipeEvent.CurrentHealth);
            });
        }
        void FindName()
        {
            Name = gameObject.scene.name + gameObject.name;
            if (!FightTogether.healthManagerNames.Contains(Name))
            {

                FightTogether.healthManagerNames.Add(Name);
                return;
            }
            for (var i = 0; i < 100; i++)
            {
                Name = $"{gameObject.scene.name}{gameObject.name} ({i})";
                if (!FightTogether.healthManagerNames.Contains(Name))
                {
                    FightTogether.healthManagerNames.Add(Name);
                    return;
                }

            }
            FightTogether.Instance.Log($"Cannot find unique name for {Name} after 100 tries, bug game has bugs!");
        }
        void Awake()
        {
            hm = GetComponent<HealthManager>();
            FindName();
            lastHp = hm.hp;
            originalHp = hm.hp;
            if (hm.isDead)
            {
                return;
            }
            FightTogether.pipeClient.ClientApi.ClientManager.ConnectEvent += ClientManager_ConnectEvent; ;
            FightTogether.pipeClient.ClientApi.ClientManager.DisconnectEvent += ClientManager_DisconnectEvent;
            JoinPool();
            if (hm.hp > 90)
            {
                FightTogether.pipeClient.ClientApi.UiManager.ChatBox.AddMessage($"An ill fated one Appears {gameObject.name}!");
            }
        }
        private void Log(string str)
        {
            FightTogether.Instance.Log(str);
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
                    FightTogether.Instance.Log("Source is null, using Hero");
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
                    if (originalHp > 90)
                    {
                        FightTogether.pipeClient.ClientApi.UiManager.ChatBox.AddMessage($"The waves were felt across the realms!");
                    }
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
            FightTogether.pipeClient.ClientApi.ClientManager.ConnectEvent -= ClientManager_ConnectEvent; ;
            FightTogether.pipeClient.ClientApi.ClientManager.DisconnectEvent -= ClientManager_DisconnectEvent;
        }

        void FixedUpdate()
        {
            if (lastHp != hm.hp && lastHp > hm.hp)
            {
                var difference = lastHp - hm.hp;
                // now this is just the normal way of things
                //Log($"Correcting hp for an unknown case, old: {lastHp} new: {hm.hp} difference: {difference}");
                UpdatePool(difference);
                lastHp = hm.hp;
            }
        }
    }

}
