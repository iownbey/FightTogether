using HKMirror.Reflection;
using Hkmp.Api.Server;
using HkmpPouch;
using Modding;
using Satchel;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SharedHealthManager
{
    public class ShareHealth : MonoBehaviour {

        public HealthManager hm;
        public int lastHp,originalHp;
        public string Name;

        bool poolJoined = false;
        void UpdatePool(int hpreduction = 0) {
            Log("UpdatePool");

            //relay damage
            SharedHealthManager.pipeClient.SendToServer("ModifyPool", $"{Name}|{hpreduction.ToString(CultureInfo.InvariantCulture)}");
            Log("UpdatePool" + $"{Name}|{hpreduction.ToString(CultureInfo.InvariantCulture)}");
        }
        void LeavePool() {

            Log("Leave Pool");
            //leave pool
            if (hm.hp > originalHp) { 
                hm.hp = originalHp; // reset to starting hp as a fail safe
                lastHp = originalHp;
            }
            poolJoined = false;
            SharedHealthManager.pipeClient.SendToServer("LeavePool", $"{Name}");

            // also remove any listeners
            SharedHealthManager.pipeClient.OnRecieve -= PipeClient_OnRecieve;
            Log($"Leave Pool{Name}");
        }
        void JoinPool() {

            Log("Join Pool");
            if (poolJoined) {
                return;
            }
            if(hm.hp <= 0)
            {
                return;
            }
            //join pool
            var hp = hm.hp.ToString(CultureInfo.InvariantCulture);
            SharedHealthManager.pipeClient.SendToServer("JoinPool", $"{Name}|{hp}");

            Log($"Join Pool { Name}|{ hp}");
            poolJoined = true;
            SharedHealthManager.pipeClient.OnRecieve += PipeClient_OnRecieve;
        }
        void Awake() {
            hm = GetComponent<HealthManager>();
            Name = gameObject.scene.name + gameObject.name;
            lastHp = hm.hp;
            originalHp = hm.hp;
            SharedHealthManager.pipeClient.ClientApi.ClientManager.ConnectEvent += ClientManager_ConnectEvent; ;
            SharedHealthManager.pipeClient.ClientApi.ClientManager.DisconnectEvent += ClientManager_DisconnectEvent;
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            JoinPool();
        }
        private void Log(string str)
        {
            SharedHealthManager.Instance.Log(str);
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

        private void PipeClient_OnRecieve(object sender, ReceivedEventArgs e)
        {
            var Data = e.Data;
            if (Data.EventName != "UpdatedPool") { return; }

            var split = e.Data.EventData.Split(new char[] { '|' });
            if (Name != split[0]) { return; }

            SharedHealthManager.Instance.Log(e.Data.EventData);

            var hp = int.Parse(split[1]);
            lastHp = hp; // since we do not want to track our own changes
            hm.hp = hp;
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
                    SharedHealthManager.Instance.Log("Source is null, using Hero");
                    hI.Source = HeroController.instance.gameObject;
                }

                SharedHealthManager.pipeClient.OnRecieve -= PipeClient_OnRecieve;
                if (hm != null) { 
                    ReflectionHelper.CallMethod<HealthManager>(hm,"TakeDamage", hI); 
                }
            }
            
        }

        void OnDestroy() { 
            LeavePool();
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            SharedHealthManager.pipeClient.ClientApi.ClientManager.ConnectEvent -= ClientManager_ConnectEvent; ;
            SharedHealthManager.pipeClient.ClientApi.ClientManager.DisconnectEvent -= ClientManager_DisconnectEvent;
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self == null || gameObject == null) {
                Log("Somehow the HM itself is NULL");
                return;
            }
            if (self.gameObject == null)
            {
                Log("Somehow the HM GO itself is NULL");
                return;
            }
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy || self.gameObject != gameObject) {
                orig(self, hitInstance);
                return;
            }
            JoinPool();
            var oldHp = hm.hp;
            orig(self, hitInstance);
            UpdatePool((oldHp - hm.hp));
        }

        void FixedUpdate() { 
            if(lastHp != hm.hp)
            {
                var difference = lastHp - hm.hp;
                Log("Correcting for unknown case");
                UpdatePool(difference);
                lastHp = hm.hp;
            }
        }
    }
    public class SharedHealthManager : Mod
    {
        internal static SharedHealthManager Instance;
        internal static Server server;
        internal static PipeClient pipeClient;

        internal static Dictionary<string, HealthManager> healthManagers = new Dictionary<string, HealthManager>();
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;

            if (server == null)
            {
                server = new Server();
                ServerAddon.RegisterAddon(server);
            }
            if (pipeClient == null) {
                pipeClient = new PipeClient("SharedHealthManager");
            }
            On.HealthManager.Start += HealthManager_Start;
            pipeClient.OnRecieve += PipeClient_OnRecieve;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        private void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            var gos = GameObjectUtils.GetAllGameObjectsInScene();
            foreach(var go in gos)
            {
                JoinPool(go);
            }
        }

        private void PipeClient_OnRecieve(object sender, ReceivedEventArgs e)
        {
            
        }

        private void HealthManager_Start(On.HealthManager.orig_Start orig, HealthManager self)
        {
            orig(self);
            JoinPool(self.gameObject);
        }

        private void JoinPool(GameObject go)
        {
            if (go.GetComponent<HealthManager>() == null) return;
            go.GetAddComponent<ShareHealth>();
        }

    }

}